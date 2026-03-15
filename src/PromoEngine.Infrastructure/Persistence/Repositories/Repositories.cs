using Microsoft.EntityFrameworkCore;
using PromoEngine.Application.Abstractions;
using PromoEngine.Application.Dtos;
using PromoEngine.Domain.Enums;
using PromoEngine.Domain.Models;
using PromoEngine.Infrastructure.Persistence;

namespace PromoEngine.Infrastructure.Persistence.Repositories;

public sealed class PromotionRepository(PromoEngineDbContext dbContext) : IPromotionRepository
{
    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken)
        => (await dbContext.Promotions.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTimeOffset now, CancellationToken cancellationToken)
        => (await dbContext.Promotions
            .AsNoTracking()
            .Where(x => x.IsActive && x.StartsAtUtc <= now && x.EndsAtUtc >= now)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Promotions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        dbContext.Promotions.Add(Map(promotion));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Promotions.FirstAsync(x => x.Id == promotion.Id, cancellationToken);
        entity.Code = promotion.Code;
        entity.Name = promotion.Name;
        entity.Description = promotion.Description;
        entity.CampaignKey = promotion.CampaignKey;
        entity.Type = (int)promotion.Type;
        entity.IsActive = promotion.IsActive;
        entity.StartsAtUtc = promotion.StartsAtUtc;
        entity.EndsAtUtc = promotion.EndsAtUtc;
        entity.Priority = promotion.Priority;
        entity.IsFunded = promotion.IsFunded;
        entity.Channel = (int?)promotion.Channel;
        entity.Segment = (int?)promotion.Segment;
        entity.IsCombinable = promotion.IsCombinable;
        entity.BudgetCap = promotion.BudgetCap;
        entity.BudgetConsumed = promotion.BudgetConsumed;
        entity.BudgetDailyCap = promotion.BudgetDailyCap;
        entity.BudgetPerCustomerCap = promotion.BudgetPerCustomerCap;
        entity.Value = promotion.Value;
        entity.DiscountValueType = (int)promotion.DiscountValueType;
        entity.ThresholdAmount = promotion.ThresholdAmount;
        entity.RequiredQuantity = promotion.RequiredQuantity;
        entity.ChargedQuantity = promotion.ChargedQuantity;
        entity.BundlePrice = promotion.BundlePrice;
        entity.MinimumMarginRate = promotion.MinimumMarginRate;
        entity.MinimumCartValue = promotion.MinimumCartValue;
        entity.MaximumDiscount = promotion.MaximumDiscount;
        entity.FundingManufacturerRate = promotion.FundingManufacturerRate;
        entity.FundingRetailerRate = promotion.FundingRetailerRate;
        entity.CouponCode = promotion.CouponCode;
        entity.TargetSkus = SerializeList(promotion.TargetSkus);
        entity.BundleSkus = SerializeList(promotion.BundleSkus);
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Promotions.FirstAsync(x => x.Id == id, cancellationToken);
        dbContext.Promotions.Remove(entity);
    }

    private static Promotion Map(PromotionEntity entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        CampaignKey = entity.CampaignKey,
        Type = (PromotionType)entity.Type,
        IsActive = entity.IsActive,
        StartsAtUtc = entity.StartsAtUtc,
        EndsAtUtc = entity.EndsAtUtc,
        Priority = entity.Priority,
        IsFunded = entity.IsFunded,
        Channel = entity.Channel.HasValue ? (Channel)entity.Channel.Value : null,
        Segment = entity.Segment.HasValue ? (CustomerSegment)entity.Segment.Value : null,
        IsCombinable = entity.IsCombinable,
        BudgetCap = entity.BudgetCap,
        BudgetConsumed = entity.BudgetConsumed,
        BudgetDailyCap = entity.BudgetDailyCap,
        BudgetPerCustomerCap = entity.BudgetPerCustomerCap,
        Value = entity.Value,
        DiscountValueType = (DiscountValueType)entity.DiscountValueType,
        ThresholdAmount = entity.ThresholdAmount,
        RequiredQuantity = entity.RequiredQuantity,
        ChargedQuantity = entity.ChargedQuantity,
        BundlePrice = entity.BundlePrice,
        MinimumMarginRate = entity.MinimumMarginRate,
        MinimumCartValue = entity.MinimumCartValue,
        MaximumDiscount = entity.MaximumDiscount,
        FundingManufacturerRate = entity.FundingManufacturerRate,
        FundingRetailerRate = entity.FundingRetailerRate,
        CouponCode = entity.CouponCode,
        TargetSkus = DeserializeList(entity.TargetSkus),
        BundleSkus = DeserializeList(entity.BundleSkus)
    };

    private static PromotionEntity Map(Promotion promotion) => new()
    {
        Id = promotion.Id,
        Code = promotion.Code,
        Name = promotion.Name,
        Description = promotion.Description,
        CampaignKey = promotion.CampaignKey,
        Type = (int)promotion.Type,
        IsActive = promotion.IsActive,
        StartsAtUtc = promotion.StartsAtUtc,
        EndsAtUtc = promotion.EndsAtUtc,
        Priority = promotion.Priority,
        IsFunded = promotion.IsFunded,
        Channel = (int?)promotion.Channel,
        Segment = (int?)promotion.Segment,
        IsCombinable = promotion.IsCombinable,
        BudgetCap = promotion.BudgetCap,
        BudgetConsumed = promotion.BudgetConsumed,
        BudgetDailyCap = promotion.BudgetDailyCap,
        BudgetPerCustomerCap = promotion.BudgetPerCustomerCap,
        Value = promotion.Value,
        DiscountValueType = (int)promotion.DiscountValueType,
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
        TargetSkus = SerializeList(promotion.TargetSkus),
        BundleSkus = SerializeList(promotion.BundleSkus),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        UpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private static string SerializeList(IReadOnlyList<string> values) => string.Join(';', values);

    private static IReadOnlyList<string> DeserializeList(string value)
        => string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed class QuoteAuditRepository(PromoEngineDbContext dbContext) : IQuoteAuditRepository
{
    public Task AddAsync(QuoteAuditEntry entry, CancellationToken cancellationToken)
    {
        dbContext.QuoteAudits.Add(new QuoteAuditEntity
        {
            QuoteId = entry.QuoteId,
            IsSimulation = entry.IsSimulation,
            Strategy = entry.Strategy,
            Subtotal = entry.Subtotal,
            DiscountTotal = entry.DiscountTotal,
            NetTotal = entry.NetTotal,
            RequestJson = entry.RequestJson,
            ResponseJson = entry.ResponseJson,
            CreatedAtUtc = entry.CreatedAtUtc
        });

        return Task.CompletedTask;
    }
}

public sealed class BudgetConsumptionRepository(PromoEngineDbContext dbContext) : IBudgetConsumptionRepository
{
    public async Task<IReadOnlyDictionary<Guid, BudgetConsumptionSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> promotionIds,
        DateOnly consumptionDateUtc,
        string customerId,
        CancellationToken cancellationToken)
    {
        var ids = promotionIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, BudgetConsumptionSnapshot>();
        }

        var entries = await dbContext.BudgetConsumptions
            .AsNoTracking()
            .Where(x => ids.Contains(x.PromotionId) && x.ConsumptionDateUtc == consumptionDateUtc && (x.CustomerId == null || x.CustomerId == customerId))
            .ToListAsync(cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => new BudgetConsumptionSnapshot(
                id,
                entries.Where(x => x.PromotionId == id && x.CustomerId == null).Sum(x => x.ConsumedAmount),
                entries.Where(x => x.PromotionId == id && x.CustomerId == customerId).Sum(x => x.ConsumedAmount)));
    }

    public async Task AddRangeAsync(IEnumerable<BudgetConsumptionEntry> entries, CancellationToken cancellationToken)
    {
        var materializedEntries = entries.Where(entry => entry.Amount > 0m).ToArray();
        foreach (var entry in materializedEntries)
        {
            await UpsertBucketAsync(entry.PromotionId, entry.ConsumptionDateUtc, null, entry.Amount, entry.RecordedAtUtc, cancellationToken);
            await UpsertBucketAsync(entry.PromotionId, entry.ConsumptionDateUtc, entry.CustomerId, entry.Amount, entry.RecordedAtUtc, cancellationToken);
        }
    }

    private async Task UpsertBucketAsync(
        Guid promotionId,
        DateOnly consumptionDateUtc,
        string? customerId,
        decimal amount,
        DateTimeOffset recordedAtUtc,
        CancellationToken cancellationToken)
    {
        var bucket = await dbContext.BudgetConsumptions.FirstOrDefaultAsync(
            x => x.PromotionId == promotionId && x.ConsumptionDateUtc == consumptionDateUtc && x.CustomerId == customerId,
            cancellationToken);

        if (bucket is null)
        {
            dbContext.BudgetConsumptions.Add(new BudgetConsumptionEntity
            {
                Id = Guid.NewGuid(),
                PromotionId = promotionId,
                ConsumptionDateUtc = consumptionDateUtc,
                CustomerId = customerId,
                ConsumedAmount = amount,
                CreatedAtUtc = recordedAtUtc,
                UpdatedAtUtc = recordedAtUtc
            });

            return;
        }

        bucket.ConsumedAmount += amount;
        bucket.UpdatedAtUtc = recordedAtUtc;
    }
}

public sealed class PromotionRedemptionRepository(PromoEngineDbContext dbContext) : IPromotionRedemptionRepository
{
    public async Task AddRangeAsync(IEnumerable<PromotionRedemptionEntry> entries, CancellationToken cancellationToken)
    {
        var materializedEntries = entries.Where(x => x.Amount > 0m).ToArray();
        var promotionIds = materializedEntries.Select(entry => entry.PromotionId).Distinct().ToArray();
        var promotions = await dbContext.Promotions.Where(x => promotionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var entry in materializedEntries)
        {
            dbContext.PromotionRedemptions.Add(new PromotionRedemptionEntity
            {
                Id = Guid.NewGuid(),
                QuoteId = entry.QuoteId,
                PromotionId = entry.PromotionId,
                PromotionCode = entry.PromotionCode,
                Amount = entry.Amount,
                RedeemedAtUtc = entry.RedeemedAtUtc
            });

            if (promotions.TryGetValue(entry.PromotionId, out var promotion))
            {
                promotion.BudgetConsumed += entry.Amount;
                promotion.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }
    }
}
