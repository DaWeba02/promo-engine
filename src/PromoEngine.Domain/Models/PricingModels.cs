using PromoEngine.Domain.Enums;

namespace PromoEngine.Domain.Models;

public sealed class Promotion
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CampaignKey { get; init; } = string.Empty;
    public PromotionType Type { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public int Priority { get; init; }
    public bool IsFunded { get; init; }
    public decimal BudgetCap { get; init; }
    public decimal BudgetConsumed { get; init; }
    public decimal Value { get; init; }
    public DiscountValueType DiscountValueType { get; init; } = DiscountValueType.FixedAmount;
    public decimal ThresholdAmount { get; init; }
    public int RequiredQuantity { get; init; }
    public int ChargedQuantity { get; init; }
    public decimal BundlePrice { get; init; }
    public decimal MinimumMarginRate { get; init; }
    public string? CouponCode { get; init; }
    public IReadOnlyList<string> TargetSkus { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BundleSkus { get; init; } = Array.Empty<string>();

    public decimal BudgetRemaining => BudgetCap <= 0m ? decimal.MaxValue : Math.Max(0m, BudgetCap - BudgetConsumed);
}

public sealed record QuoteRequest(
    string CustomerId,
    string Currency,
    string? CouponCode,
    ConflictResolutionStrategy Strategy,
    decimal MinimumMarginRate,
    IReadOnlyList<QuoteLine> Items);

public sealed record QuoteLine(
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    int? StockLevel);

public sealed record QuotedLine(
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    decimal LineSubtotal,
    decimal DiscountAmount,
    decimal NetLineTotal);

public sealed record KpiImpact(
    decimal RevenueDelta,
    decimal MarginDelta,
    decimal InventoryScore,
    decimal BudgetUsage);

public sealed record PromotionLineImpact(
    string Sku,
    int Quantity,
    decimal DiscountAmount);

public sealed record PromotionDecision(
    Guid PromotionId,
    string PromotionCode,
    string PromotionName,
    string Status,
    string ReasonCode,
    decimal DiscountAmount,
    decimal BudgetImpact,
    IReadOnlyList<PromotionLineImpact> AffectedItems,
    KpiImpact KpiEffect);

public sealed record PriceQuote(
    Guid QuoteId,
    bool IsSimulation,
    string Currency,
    ConflictResolutionStrategy Strategy,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal NetTotal,
    decimal MarginAmount,
    decimal MarginRate,
    IReadOnlyList<QuotedLine> Lines,
    IReadOnlyList<PromotionDecision> Promotions,
    KpiImpact KpiSummary);
