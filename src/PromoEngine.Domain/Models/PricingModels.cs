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
    public Channel? Channel { get; init; }
    public CustomerSegment? Segment { get; init; }
    public bool IsCombinable { get; init; } = true;
    public decimal BudgetCap { get; init; }
    public decimal BudgetConsumed { get; init; }
    public decimal BudgetDailyCap { get; init; }
    public decimal BudgetConsumedDaily { get; init; }
    public decimal? BudgetPerCustomerCap { get; init; }
    public decimal BudgetConsumedByCustomer { get; init; }
    public decimal Value { get; init; }
    public DiscountValueType DiscountValueType { get; init; } = DiscountValueType.FixedAmount;
    public decimal ThresholdAmount { get; init; }
    public int RequiredQuantity { get; init; }
    public int ChargedQuantity { get; init; }
    public decimal BundlePrice { get; init; }
    public decimal MinimumMarginRate { get; init; }
    public decimal MinimumCartValue { get; init; }
    public decimal? MaximumDiscount { get; init; }
    public decimal FundingManufacturerRate { get; init; }
    public decimal FundingRetailerRate { get; init; } = 1m;
    public string? CouponCode { get; init; }
    public IReadOnlyList<string> TargetSkus { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BundleSkus { get; init; } = Array.Empty<string>();

    public decimal BudgetRemaining => BudgetCap <= 0m ? decimal.MaxValue : Math.Max(0m, BudgetCap - BudgetConsumed);
    public decimal BudgetDailyRemaining => BudgetDailyCap <= 0m ? decimal.MaxValue : Math.Max(0m, BudgetDailyCap - BudgetConsumedDaily);
    public decimal BudgetPerCustomerRemaining => BudgetPerCustomerCap is null || BudgetPerCustomerCap <= 0m
        ? decimal.MaxValue
        : Math.Max(0m, BudgetPerCustomerCap.Value - BudgetConsumedByCustomer);
}

public sealed record QuoteRequest(
    string CustomerId,
    string Currency,
    string? CouponCode,
    ConflictResolutionStrategy Strategy,
    decimal MinimumMarginRate,
    IReadOnlyList<QuoteLine> Items,
    Channel Channel = Channel.Online,
    CustomerSegment Segment = CustomerSegment.ExistingCustomer);

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
    decimal BudgetUsage,
    decimal ManufacturerFundingAmount,
    decimal RetailerFundingAmount);

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
