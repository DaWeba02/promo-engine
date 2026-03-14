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
        entity.BudgetCap = promotion.BudgetCap;
        entity.BudgetConsumed = promotion.BudgetConsumed;
        entity.Value = promotion.Value;
        entity.DiscountValueType = (int)promotion.DiscountValueType;
        entity.ThresholdAmount = promotion.ThresholdAmount;
        entity.RequiredQuantity = promotion.RequiredQuantity;
        entity.ChargedQuantity = promotion.ChargedQuantity;
        entity.BundlePrice = promotion.BundlePrice;
        entity.MinimumMarginRate = promotion.MinimumMarginRate;
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
        BudgetCap = entity.BudgetCap,
        BudgetConsumed = entity.BudgetConsumed,
        Value = entity.Value,
        DiscountValueType = (DiscountValueType)entity.DiscountValueType,
        ThresholdAmount = entity.ThresholdAmount,
        RequiredQuantity = entity.RequiredQuantity,
        ChargedQuantity = entity.ChargedQuantity,
        BundlePrice = entity.BundlePrice,
        MinimumMarginRate = entity.MinimumMarginRate,
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
        BudgetCap = promotion.BudgetCap,
        BudgetConsumed = promotion.BudgetConsumed,
        Value = promotion.Value,
        DiscountValueType = (int)promotion.DiscountValueType,
        ThresholdAmount = promotion.ThresholdAmount,
        RequiredQuantity = promotion.RequiredQuantity,
        ChargedQuantity = promotion.ChargedQuantity,
        BundlePrice = promotion.BundlePrice,
        MinimumMarginRate = promotion.MinimumMarginRate,
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

public sealed class PromotionRedemptionRepository(PromoEngineDbContext dbContext) : IPromotionRedemptionRepository
{
    public async Task AddRangeAsync(IEnumerable<PromotionRedemptionEntry> entries, CancellationToken cancellationToken)
    {
        foreach (var entry in entries.Where(x => x.Amount > 0m))
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

            var promotion = await dbContext.Promotions.FirstAsync(x => x.Id == entry.PromotionId, cancellationToken);
            promotion.BudgetConsumed += entry.Amount;
            promotion.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
