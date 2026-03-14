using PromoEngine.Domain.Enums;
using PromoEngine.Domain.Models;

namespace PromoEngine.Domain.UnitTests.Fixtures;

internal static class TestData
{
    public static Promotion PercentPromotion(decimal value = 10m, params string[] skus) => new()
    {
        Code = "PERCENT10",
        Name = "Percent",
        Description = "Percent off",
        CampaignKey = "SPRING",
        Type = PromotionType.PercentDiscount,
        IsActive = true,
        StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        EndsAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        Value = value,
        TargetSkus = skus.Length == 0 ? new[] { "SKU-1" } : skus
    };

    public static Promotion FixedPromotion(decimal value = 5m, params string[] skus) => new()
    {
        Code = "FIXED5",
        Name = "Fixed",
        Description = "Fixed off",
        CampaignKey = "SPRING",
        Type = PromotionType.FixedAmountDiscount,
        IsActive = true,
        StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        EndsAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        Value = value,
        TargetSkus = skus.Length == 0 ? new[] { "SKU-1" } : skus
    };

    public static QuoteRequest Quote(ConflictResolutionStrategy strategy = ConflictResolutionStrategy.CustomerBestPrice, string? coupon = null, decimal minimumMargin = 0m)
        => new(
            "customer-1",
            "EUR",
            coupon,
            strategy,
            minimumMargin,
            new[]
            {
                new QuoteLine("SKU-1", 3, 20m, 10m, 50),
                new QuoteLine("SKU-2", 2, 15m, 7m, 200),
                new QuoteLine("SKU-3", 1, 30m, 12m, 5)
            });
}
