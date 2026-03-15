using System.Text.Json;
using PromoEngine.Application.Abstractions;
using PromoEngine.Application.Dtos;
using PromoEngine.Domain.Abstractions;
using PromoEngine.Domain.Enums;
using PromoEngine.Domain.Models;

namespace PromoEngine.Application.Services;

public sealed class QuoteService(
    IPromotionRepository promotionRepository,
    IQuoteAuditRepository quoteAuditRepository,
    IBudgetConsumptionRepository budgetConsumptionRepository,
    IPromotionRedemptionRepository promotionRedemptionRepository,
    IUnitOfWork unitOfWork,
    IPricingEngine pricingEngine,
    IClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<QuoteResponseDto> CreateQuoteAsync(QuoteRequestDto request, bool isSimulation, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var promotions = await GetPromotionsForEvaluationAsync(request.CustomerId, now, cancellationToken);
        var response = pricingEngine.Evaluate(request.ToDomainRequest(), promotions, now, isSimulation).ToDto();

        if (!isSimulation)
        {
            await quoteAuditRepository.AddAsync(
                new QuoteAuditEntry(
                    response.QuoteId,
                    false,
                    response.Strategy.ToString(),
                    response.Subtotal,
                    response.DiscountTotal,
                    response.NetTotal,
                    JsonSerializer.Serialize(request, JsonOptions),
                    JsonSerializer.Serialize(response, JsonOptions),
                    now),
                cancellationToken);

            var redemptions = response.Promotions
                .Where(promotion => string.Equals(promotion.Status, "Applied", StringComparison.Ordinal))
                .Select(promotion => new PromotionRedemptionEntry(response.QuoteId, promotion.PromotionId, promotion.PromotionCode, promotion.BudgetImpact, now));

            var budgetEntries = response.Promotions
                .Where(promotion => string.Equals(promotion.Status, "Applied", StringComparison.Ordinal) && promotion.BudgetImpact > 0m)
                .Select(promotion => new BudgetConsumptionEntry(
                    promotion.PromotionId,
                    request.CustomerId,
                    promotion.BudgetImpact,
                    DateOnly.FromDateTime(now.UtcDateTime),
                    now));

            await promotionRedemptionRepository.AddRangeAsync(redemptions, cancellationToken);
            await budgetConsumptionRepository.AddRangeAsync(budgetEntries, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    public async Task<SimulationCompareResponseDto> CompareStrategiesAsync(SimulationCompareRequestDto request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var promotions = await GetPromotionsForEvaluationAsync(request.CustomerId, now, cancellationToken);
        var results = request.Strategies
            .Distinct()
            .Select(strategy => pricingEngine.Evaluate(request.ToDomainRequest(strategy), promotions, now, isSimulation: true).ToDto())
            .Select(quote => new SimulationStrategyResultDto(
                quote.Strategy,
                new QuoteTotalsDto(quote.Subtotal, quote.DiscountTotal, quote.NetTotal, quote.MarginAmount, quote.MarginRate),
                quote.Promotions,
                quote.KpiSummary))
            .ToArray();

        return new SimulationCompareResponseDto(request.Currency, results);
    }

    private async Task<IReadOnlyList<Promotion>> GetPromotionsForEvaluationAsync(string customerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.GetActiveAsync(now, cancellationToken);
        var snapshots = await budgetConsumptionRepository.GetSnapshotsAsync(
            promotions.Select(promotion => promotion.Id).ToArray(),
            DateOnly.FromDateTime(now.UtcDateTime),
            customerId,
            cancellationToken);

        return promotions
            .Select(promotion => promotion.WithBudgetSnapshot(snapshots.GetValueOrDefault(promotion.Id)))
            .ToArray();
    }
}

public sealed class PromotionService(IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<PromotionDto>> GetAllAsync(CancellationToken cancellationToken)
        => (await promotionRepository.GetAllAsync(cancellationToken)).Select(promotion => promotion.ToDto()).ToArray();

    public async Task<PromotionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => (await promotionRepository.GetByIdAsync(id, cancellationToken))?.ToDto();

    public async Task<PromotionDto> CreateAsync(UpsertPromotionRequest request, CancellationToken cancellationToken)
    {
        var promotion = request.ToDomain();
        await promotionRepository.AddAsync(promotion, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return promotion.ToDto();
    }

    public async Task<PromotionDto?> UpdateAsync(Guid id, UpsertPromotionRequest request, CancellationToken cancellationToken)
    {
        if (await promotionRepository.GetByIdAsync(id, cancellationToken) is null)
        {
            return null;
        }

        var updated = request.ToDomain(id);
        await promotionRepository.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await promotionRepository.GetByIdAsync(id, cancellationToken) is null)
        {
            return false;
        }

        await promotionRepository.DeleteAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class MappingExtensions
{
    public static Promotion ToDomain(this UpsertPromotionRequest request, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Code = request.Code.Trim(),
        Name = request.Name.Trim(),
        Description = request.Description.Trim(),
        CampaignKey = request.CampaignKey.Trim(),
        Type = request.Type,
        IsActive = request.IsActive,
        StartsAtUtc = request.StartsAtUtc,
        EndsAtUtc = request.EndsAtUtc,
        Priority = request.Priority,
        IsFunded = request.IsFunded,
        Channel = request.Channel,
        Segment = request.Segment,
        IsCombinable = request.IsCombinable,
        BudgetCap = request.BudgetCap,
        BudgetConsumed = request.BudgetConsumed,
        BudgetDailyCap = request.BudgetDailyCap,
        BudgetPerCustomerCap = request.BudgetPerCustomerCap,
        Value = request.Value,
        DiscountValueType = request.DiscountValueType,
        ThresholdAmount = request.ThresholdAmount,
        RequiredQuantity = request.RequiredQuantity,
        ChargedQuantity = request.ChargedQuantity,
        BundlePrice = request.BundlePrice,
        MinimumMarginRate = request.MinimumMarginRate,
        MinimumCartValue = request.MinimumCartValue,
        MaximumDiscount = request.MaximumDiscount,
        FundingManufacturerRate = request.GetFundingManufacturerRate(),
        FundingRetailerRate = request.GetFundingRetailerRate(),
        CouponCode = request.CouponCode,
        TargetSkus = request.TargetSkus.Where(sku => !string.IsNullOrWhiteSpace(sku)).Select(sku => sku.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
        BundleSkus = request.BundleSkus.Where(sku => !string.IsNullOrWhiteSpace(sku)).Select(sku => sku.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
    };

    public static PromotionDto ToDto(this Promotion promotion) => new(
        promotion.Id,
        promotion.Code,
        promotion.Name,
        promotion.Description,
        promotion.CampaignKey,
        promotion.Type,
        promotion.IsActive,
        promotion.StartsAtUtc,
        promotion.EndsAtUtc,
        promotion.Priority,
        promotion.IsFunded,
        promotion.Channel,
        promotion.Segment,
        promotion.IsCombinable,
        promotion.BudgetCap,
        promotion.BudgetConsumed,
        promotion.BudgetDailyCap,
        promotion.BudgetPerCustomerCap,
        promotion.Value,
        promotion.DiscountValueType,
        promotion.ThresholdAmount,
        promotion.RequiredQuantity,
        promotion.ChargedQuantity,
        promotion.BundlePrice,
        promotion.MinimumMarginRate,
        promotion.MinimumCartValue,
        promotion.MaximumDiscount,
        promotion.FundingManufacturerRate,
        promotion.FundingRetailerRate,
        promotion.CouponCode,
        promotion.TargetSkus,
        promotion.BundleSkus);

    public static QuoteResponseDto ToDto(this PriceQuote quote) => new(
        quote.QuoteId,
        quote.IsSimulation,
        quote.Currency,
        quote.Strategy,
        quote.Subtotal,
        quote.DiscountTotal,
        quote.NetTotal,
        quote.MarginAmount,
        quote.MarginRate,
        quote.Lines.Select(line => new QuotedLineDto(line.Sku, line.Quantity, line.UnitPrice, line.UnitCost, line.LineSubtotal, line.DiscountAmount, line.NetLineTotal)).ToArray(),
        quote.Promotions.Select(promotion => new PromotionDecisionDto(
            promotion.PromotionId,
            promotion.PromotionCode,
            promotion.PromotionName,
            promotion.Status,
            promotion.ReasonCode,
            promotion.DiscountAmount,
            promotion.BudgetImpact,
            promotion.AffectedItems.Select(item => new PromotionLineImpactDto(item.Sku, item.Quantity, item.DiscountAmount)).ToArray(),
            new KpiImpactDto(
                promotion.KpiEffect.RevenueDelta,
                promotion.KpiEffect.MarginDelta,
                promotion.KpiEffect.InventoryScore,
                promotion.KpiEffect.BudgetUsage,
                promotion.KpiEffect.ManufacturerFundingAmount,
                promotion.KpiEffect.RetailerFundingAmount))).ToArray(),
        new KpiImpactDto(
            quote.KpiSummary.RevenueDelta,
            quote.KpiSummary.MarginDelta,
            quote.KpiSummary.InventoryScore,
            quote.KpiSummary.BudgetUsage,
            quote.KpiSummary.ManufacturerFundingAmount,
            quote.KpiSummary.RetailerFundingAmount));

    public static QuoteRequest ToDomainRequest(this QuoteRequestDto request) => new(
        request.CustomerId,
        request.Currency,
        request.CouponCode,
        request.Strategy,
        request.MinimumMarginRate,
        request.Items.Select(item => new QuoteLine(item.Sku, item.Quantity, item.UnitPrice, item.UnitCost, item.StockLevel)).ToArray(),
        request.Channel ?? Channel.Online,
        request.Segment ?? CustomerSegment.ExistingCustomer);

    public static QuoteRequest ToDomainRequest(this SimulationCompareRequestDto request, ConflictResolutionStrategy strategy) => new(
        request.CustomerId,
        request.Currency,
        request.CouponCode,
        strategy,
        request.MinimumMarginRate,
        request.Items.Select(item => new QuoteLine(item.Sku, item.Quantity, item.UnitPrice, item.UnitCost, item.StockLevel)).ToArray(),
        request.Channel ?? Channel.Online,
        request.Segment ?? CustomerSegment.ExistingCustomer);

    public static Promotion WithBudgetSnapshot(this Promotion promotion, BudgetConsumptionSnapshot? snapshot) => new()
    {
        Id = promotion.Id,
        Code = promotion.Code,
        Name = promotion.Name,
        Description = promotion.Description,
        CampaignKey = promotion.CampaignKey,
        Type = promotion.Type,
        IsActive = promotion.IsActive,
        StartsAtUtc = promotion.StartsAtUtc,
        EndsAtUtc = promotion.EndsAtUtc,
        Priority = promotion.Priority,
        IsFunded = promotion.IsFunded,
        Channel = promotion.Channel,
        Segment = promotion.Segment,
        IsCombinable = promotion.IsCombinable,
        BudgetCap = promotion.BudgetCap,
        BudgetConsumed = promotion.BudgetConsumed,
        BudgetDailyCap = promotion.BudgetDailyCap,
        BudgetConsumedDaily = snapshot?.DailyConsumed ?? 0m,
        BudgetPerCustomerCap = promotion.BudgetPerCustomerCap,
        BudgetConsumedByCustomer = snapshot?.CustomerConsumed ?? 0m,
        Value = promotion.Value,
        DiscountValueType = promotion.DiscountValueType,
        ThresholdAmount = promotion.ThresholdAmount,
        RequiredQuantity = promotion.RequiredQuantity,
        ChargedQuantity = promotion.ChargedQuantity,
        BundlePrice = promotion.BundlePrice,
        MinimumMarginRate = promotion.MinimumMarginRate,
        MinimumCartValue = promotion.MinimumCartValue,
        MaximumDiscount = promotion.MaximumDiscount,
        FundingManufacturerRate = promotion.FundingManufacturerRate,
        FundingRetailerRate = promotion.FundingRetailerRate,
        CouponCode = promotion.CouponCode,
        TargetSkus = promotion.TargetSkus,
        BundleSkus = promotion.BundleSkus
    };

    private static decimal GetFundingManufacturerRate(this UpsertPromotionRequest request)
    {
        if (request.FundingManufacturerRate.HasValue)
        {
            return request.FundingManufacturerRate.Value;
        }

        if (request.FundingRetailerRate.HasValue)
        {
            return 1m - request.FundingRetailerRate.Value;
        }

        return request.IsFunded ? 0.5m : 0m;
    }

    private static decimal GetFundingRetailerRate(this UpsertPromotionRequest request)
    {
        if (request.FundingRetailerRate.HasValue)
        {
            return request.FundingRetailerRate.Value;
        }

        if (request.FundingManufacturerRate.HasValue)
        {
            return 1m - request.FundingManufacturerRate.Value;
        }

        return request.IsFunded ? 0.5m : 1m;
    }
}
