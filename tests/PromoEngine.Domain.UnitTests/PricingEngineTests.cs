using PromoEngine.Domain.Enums;
using PromoEngine.Domain.Models;
using PromoEngine.Domain.Services;
using PromoEngine.Domain.UnitTests.Fixtures;

namespace PromoEngine.Domain.UnitTests;

public sealed class PricingEngineTests
{
    private readonly PricingEngine _engine = new();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Percent_discount_is_applied_with_explainability()
    {
        var quote = _engine.Evaluate(TestData.Quote(), new[] { TestData.PercentPromotion(10m, "SKU-1") }, _now, false);

        Assert.Equal(6m, quote.DiscountTotal);
        Assert.Contains(quote.Promotions, promotion => promotion.Status == "Applied" && promotion.ReasonCode == "Applied");
        Assert.Contains(quote.Promotions.Single().AffectedItems, item => item.Sku == "SKU-1");
    }

    [Fact]
    public void Quantity_deal_three_for_two_gives_one_free_unit()
    {
        var promotion = new Promotion
        {
            Code = "3FOR2",
            Name = "Three for two",
            Description = "Quantity deal",
            CampaignKey = "CLEARANCE",
            Type = PromotionType.QuantityDeal,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            RequiredQuantity = 3,
            ChargedQuantity = 2,
            TargetSkus = new[] { "SKU-1" }
        };

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);

        Assert.Equal(20m, quote.DiscountTotal);
    }

    [Fact]
    public void Bundle_discount_allocates_across_bundle_items()
    {
        var promotion = new Promotion
        {
            Code = "BUNDLE",
            Name = "Bundle",
            Description = "Bundle deal",
            CampaignKey = "BUNDLE",
            Type = PromotionType.Bundle,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            BundlePrice = 25m,
            BundleSkus = new[] { "SKU-2", "SKU-3" }
        };

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);

        Assert.Equal(20m, quote.DiscountTotal);
        Assert.Equal(2, quote.Promotions.Single().AffectedItems.Count);
    }

    [Fact]
    public void Margin_guard_rejects_candidate_below_threshold()
    {
        var promotion = TestData.FixedPromotion(15m, "SKU-3");
        var quote = _engine.Evaluate(TestData.Quote(minimumMargin: 0.55m), new[] { promotion }, _now, false);

        Assert.Contains(quote.Promotions, promotionDecision => promotionDecision.ReasonCode == "MarginGuardRejected");
        Assert.Equal(0m, quote.DiscountTotal);
    }

    [Fact]
    public void Budget_check_rejects_when_remaining_budget_is_too_low()
    {
        var promotion = new Promotion
        {
            Code = "FUNDED",
            Name = "Funded",
            Description = "Funded promotion",
            CampaignKey = "BUDGET",
            Type = PromotionType.PercentDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            Value = 50m,
            IsFunded = true,
            BudgetCap = 5m,
            TargetSkus = new[] { "SKU-1" }
        };

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.ReasonCode == "BudgetExceeded");
    }

    [Fact]
    public void Inventory_strategy_prefers_inventory_heavy_candidate()
    {
        var request = TestData.Quote(ConflictResolutionStrategy.InventoryReduction);
        var highInventoryCartPromotion = new Promotion
        {
            Code = "CART-STOCK",
            Name = "Cart Stock",
            Description = "Inventory reduction",
            CampaignKey = "CLEARANCE",
            Type = PromotionType.CartDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            ThresholdAmount = 50m,
            Value = 5m
        };

        var lowInventoryPromotion = new Promotion
        {
            Code = "SKU3",
            Name = "Sku3",
            Description = "Low stock promotion",
            CampaignKey = "CLEARANCE",
            Type = PromotionType.FixedAmountDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            Value = 10m,
            TargetSkus = new[] { "SKU-3" }
        };

        var quote = _engine.Evaluate(request, new[] { highInventoryCartPromotion, lowInventoryPromotion }, _now, false);

        Assert.Contains(quote.Promotions, promotion => promotion.PromotionCode == "CART-STOCK" && promotion.Status == "Applied");
        Assert.Contains(quote.Promotions, promotion => promotion.PromotionCode == "SKU3" && promotion.ReasonCode == "ConflictStrategyRejected");
    }

    [Fact]
    public void Cart_discount_rounding_matches_expected_total()
    {
        var promotion = new Promotion
        {
            Code = "CART",
            Name = "Cart",
            Description = "Cart off",
            CampaignKey = "SPRING",
            Type = PromotionType.CartDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            ThresholdAmount = 50m,
            Value = 12.34m
        };

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);

        Assert.Equal(12.34m, quote.DiscountTotal);
    }
}
