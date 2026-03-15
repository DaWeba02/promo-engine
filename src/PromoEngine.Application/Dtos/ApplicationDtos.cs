using PromoEngine.Domain.Enums;

namespace PromoEngine.Application.Dtos;

public sealed record QuoteLineRequestDto(
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    int? StockLevel);

public sealed record QuoteRequestDto(
    string CustomerId,
    string Currency,
    string? CouponCode,
    ConflictResolutionStrategy Strategy,
    decimal MinimumMarginRate,
    IReadOnlyList<QuoteLineRequestDto> Items,
    Channel? Channel = null,
    CustomerSegment? Segment = null);

public sealed record QuotedLineDto(
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    decimal LineSubtotal,
    decimal DiscountAmount,
    decimal NetLineTotal);

public sealed record PromotionLineImpactDto(
    string Sku,
    int Quantity,
    decimal DiscountAmount);

public sealed record KpiImpactDto(
    decimal RevenueDelta,
    decimal MarginDelta,
    decimal InventoryScore,
    decimal BudgetUsage,
    decimal ManufacturerFundingAmount,
    decimal RetailerFundingAmount);

public sealed record PromotionDecisionDto(
    Guid PromotionId,
    string PromotionCode,
    string PromotionName,
    string Status,
    string ReasonCode,
    decimal DiscountAmount,
    decimal BudgetImpact,
    IReadOnlyList<PromotionLineImpactDto> AffectedItems,
    KpiImpactDto KpiEffect);

public sealed record QuoteResponseDto(
    Guid QuoteId,
    bool IsSimulation,
    string Currency,
    ConflictResolutionStrategy Strategy,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal NetTotal,
    decimal MarginAmount,
    decimal MarginRate,
    IReadOnlyList<QuotedLineDto> Lines,
    IReadOnlyList<PromotionDecisionDto> Promotions,
    KpiImpactDto KpiSummary);

public sealed record SimulationCompareRequestDto(
    string CustomerId,
    string Currency,
    string? CouponCode,
    IReadOnlyList<ConflictResolutionStrategy> Strategies,
    decimal MinimumMarginRate,
    IReadOnlyList<QuoteLineRequestDto> Items,
    Channel? Channel = null,
    CustomerSegment? Segment = null);

public sealed record QuoteTotalsDto(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal NetTotal,
    decimal MarginAmount,
    decimal MarginRate);

public sealed record SimulationStrategyResultDto(
    ConflictResolutionStrategy Strategy,
    QuoteTotalsDto Totals,
    IReadOnlyList<PromotionDecisionDto> Promotions,
    KpiImpactDto KpiSummary);

public sealed record SimulationCompareResponseDto(
    string Currency,
    IReadOnlyList<SimulationStrategyResultDto> Results);

public sealed record UpsertPromotionRequest(
    string Code,
    string Name,
    string Description,
    string CampaignKey,
    PromotionType Type,
    bool IsActive,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int Priority,
    bool IsFunded,
    decimal BudgetCap,
    decimal BudgetConsumed,
    decimal Value,
    DiscountValueType DiscountValueType,
    decimal ThresholdAmount,
    int RequiredQuantity,
    int ChargedQuantity,
    decimal BundlePrice,
    decimal MinimumMarginRate,
    string? CouponCode,
    IReadOnlyList<string> TargetSkus,
    IReadOnlyList<string> BundleSkus,
    Channel? Channel = null,
    CustomerSegment? Segment = null,
    bool IsCombinable = true,
    decimal BudgetDailyCap = 0m,
    decimal? BudgetPerCustomerCap = null,
    decimal MinimumCartValue = 0m,
    decimal? MaximumDiscount = null,
    decimal? FundingManufacturerRate = null,
    decimal? FundingRetailerRate = null);

public sealed record PromotionDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string CampaignKey,
    PromotionType Type,
    bool IsActive,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int Priority,
    bool IsFunded,
    Channel? Channel,
    CustomerSegment? Segment,
    bool IsCombinable,
    decimal BudgetCap,
    decimal BudgetConsumed,
    decimal BudgetDailyCap,
    decimal? BudgetPerCustomerCap,
    decimal Value,
    DiscountValueType DiscountValueType,
    decimal ThresholdAmount,
    int RequiredQuantity,
    int ChargedQuantity,
    decimal BundlePrice,
    decimal MinimumMarginRate,
    decimal MinimumCartValue,
    decimal? MaximumDiscount,
    decimal FundingManufacturerRate,
    decimal FundingRetailerRate,
    string? CouponCode,
    IReadOnlyList<string> TargetSkus,
    IReadOnlyList<string> BundleSkus);

public sealed record QuoteAuditEntry(
    Guid QuoteId,
    bool IsSimulation,
    string Strategy,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal NetTotal,
    string RequestJson,
    string ResponseJson,
    DateTimeOffset CreatedAtUtc);

public sealed record PromotionRedemptionEntry(
    Guid QuoteId,
    Guid PromotionId,
    string PromotionCode,
    decimal Amount,
    DateTimeOffset RedeemedAtUtc);

public sealed record BudgetConsumptionSnapshot(
    Guid PromotionId,
    decimal DailyConsumed,
    decimal CustomerConsumed);

public sealed record BudgetConsumptionEntry(
    Guid PromotionId,
    string CustomerId,
    decimal Amount,
    DateOnly ConsumptionDateUtc,
    DateTimeOffset RecordedAtUtc);
