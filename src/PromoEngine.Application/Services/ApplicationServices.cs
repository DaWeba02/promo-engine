using System.Text.Json;
using PromoEngine.Application.Abstractions;
using PromoEngine.Application.Dtos;
using PromoEngine.Domain.Abstractions;
using PromoEngine.Domain.Models;

namespace PromoEngine.Application.Services;

public sealed class QuoteService(
    IPromotionRepository promotionRepository,
    IQuoteAuditRepository quoteAuditRepository,
    IPromotionRedemptionRepository promotionRedemptionRepository,
    IUnitOfWork unitOfWork,
    IPricingEngine pricingEngine,
    IClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<QuoteResponseDto> CreateQuoteAsync(QuoteRequestDto request, bool isSimulation, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var promotions = await promotionRepository.GetActiveAsync(now, cancellationToken);
        var domainRequest = new QuoteRequest(
            request.CustomerId,
            request.Currency,
            request.CouponCode,
            request.Strategy,
            request.MinimumMarginRate,
            request.Items.Select(item => new QuoteLine(item.Sku, item.Quantity, item.UnitPrice, item.UnitCost, item.StockLevel)).ToArray());

        var response = pricingEngine.Evaluate(domainRequest, promotions, now, isSimulation).ToDto();

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

            await promotionRedemptionRepository.AddRangeAsync(redemptions, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
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
        BudgetCap = request.BudgetCap,
        BudgetConsumed = request.BudgetConsumed,
        Value = request.Value,
        DiscountValueType = request.DiscountValueType,
        ThresholdAmount = request.ThresholdAmount,
        RequiredQuantity = request.RequiredQuantity,
        ChargedQuantity = request.ChargedQuantity,
        BundlePrice = request.BundlePrice,
        MinimumMarginRate = request.MinimumMarginRate,
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
        promotion.BudgetCap,
        promotion.BudgetConsumed,
        promotion.Value,
        promotion.DiscountValueType,
        promotion.ThresholdAmount,
        promotion.RequiredQuantity,
        promotion.ChargedQuantity,
        promotion.BundlePrice,
        promotion.MinimumMarginRate,
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
            new KpiImpactDto(promotion.KpiEffect.RevenueDelta, promotion.KpiEffect.MarginDelta, promotion.KpiEffect.InventoryScore, promotion.KpiEffect.BudgetUsage))).ToArray(),
        new KpiImpactDto(quote.KpiSummary.RevenueDelta, quote.KpiSummary.MarginDelta, quote.KpiSummary.InventoryScore, quote.KpiSummary.BudgetUsage));
}
