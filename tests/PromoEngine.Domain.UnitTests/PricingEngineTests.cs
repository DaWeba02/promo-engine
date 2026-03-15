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
        var quote = _engine.Evaluate(TestData.Quote(), new[] { CreatePercentPromotion("PERCENT10", 10m, "SKU-1") }, _now, false);

        Assert.Equal(6m, quote.DiscountTotal);
        Assert.Contains(quote.Promotions, promotion => promotion.Status == "Applied" && promotion.ReasonCode == "Applied");
        Assert.Contains(quote.Promotions.Single().AffectedItems, item => item.Sku == "SKU-1");
    }

    [Fact]
    public void Fixed_amount_discount_is_capped_at_line_subtotal()
    {
        var request = new QuoteRequest(
            "customer-1",
            "EUR",
            null,
            ConflictResolutionStrategy.CustomerBestPrice,
            0m,
            new[] { new QuoteLine("SKU-1", 1, 4m, 1m, 10) });
        var promotion = CreateFixedPromotion("FIXED-CAP", 10m, "SKU-1");

        var quote = _engine.Evaluate(request, new[] { promotion }, _now, false);

        Assert.Equal(4m, quote.DiscountTotal);
        Assert.Equal(0m, quote.Lines.Single().NetLineTotal);
    }

    [Fact]
    public void Bundle_discount_is_applied_multiple_times_when_each_sku_is_available()
    {
        var request = new QuoteRequest(
            "customer-1",
            "EUR",
            null,
            ConflictResolutionStrategy.CustomerBestPrice,
            0m,
            new[]
            {
                new QuoteLine("SKU-2", 2, 15m, 7m, 50),
                new QuoteLine("SKU-3", 2, 30m, 12m, 50)
            });
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
            FundingRetailerRate = 1m,
            BundleSkus = new[] { "SKU-2", "SKU-3" }
        };

        var quote = _engine.Evaluate(request, new[] { promotion }, _now, false);

        Assert.Equal(40m, quote.DiscountTotal);
        Assert.Equal(2, quote.Promotions.Single().AffectedItems.Count);
    }

    [Fact]
    public void Channel_mismatch_is_reported_as_a_rejected_reason()
    {
        var promotion = new Promotion
        {
            Code = "CHANNEL",
            Name = "Channel limited",
            Description = "Channel limited",
            CampaignKey = "CHANNEL",
            Type = PromotionType.PercentDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            Value = 10m,
            DiscountValueType = DiscountValueType.Percentage,
            Channel = Channel.Store,
            FundingRetailerRate = 1m,
            TargetSkus = new[] { "SKU-1" }
        };

        var quote = _engine.Evaluate(TestData.Quote(channel: Channel.Online), new[] { promotion }, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.ReasonCode == "ChannelMismatch");
    }

    [Fact]
    public void Segment_mismatch_is_reported_as_a_rejected_reason()
    {
        var promotion = new Promotion
        {
            Code = "SEGMENT",
            Name = "Segment limited",
            Description = "Segment limited",
            CampaignKey = "SEGMENT",
            Type = PromotionType.PercentDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            Value = 10m,
            DiscountValueType = DiscountValueType.Percentage,
            Segment = CustomerSegment.Loyalty,
            FundingRetailerRate = 1m,
            TargetSkus = new[] { "SKU-1" }
        };

        var quote = _engine.Evaluate(TestData.Quote(segment: CustomerSegment.ExistingCustomer), new[] { promotion }, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.ReasonCode == "SegmentMismatch");
    }

    [Fact]
    public void Non_combinable_promotions_cannot_stack()
    {
        var promotions = new[]
        {
            CreateFixedPromotion("NON-COMB", 5m, "SKU-1", isCombinable: false),
            CreateFixedPromotion("OTHER", 4m, "SKU-2")
        };

        var quote = _engine.Evaluate(TestData.Quote(), promotions, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.PromotionCode == "NON-COMB" && decision.Status == "Applied");
        Assert.Contains(quote.Promotions, decision => decision.PromotionCode == "OTHER" && decision.ReasonCode == "NonCombinableWithAppliedPromotion");
    }

    [Fact]
    public void Overlapping_combinable_promotions_are_rejected()
    {
        var promotions = new[]
        {
            CreateFixedPromotion("FIRST", 6m, "SKU-1"),
            CreatePercentPromotion("SECOND", 10m, "SKU-1")
        };

        var quote = _engine.Evaluate(TestData.Quote(), promotions, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.PromotionCode == "FIRST" && decision.Status == "Applied");
        Assert.Contains(quote.Promotions, decision => decision.PromotionCode == "SECOND" && decision.ReasonCode == "OverlappingItemsNotAllowed");
    }

    [Fact]
    public void Budget_total_cap_returns_specific_rejection_reason()
    {
        var promotion = CreateFixedPromotion("TOTAL", 10m, "SKU-1", budgetCap: 5m);

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.ReasonCode == "BudgetTotalExceeded");
    }

    [Fact]
    public void Budget_daily_cap_returns_specific_rejection_reason()
    {
        var promotion = CreateFixedPromotion("DAILY", 10m, "SKU-1", budgetDailyCap: 5m);

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.ReasonCode == "BudgetDailyExceeded");
    }

    [Fact]
    public void Budget_per_customer_cap_returns_specific_rejection_reason()
    {
        var promotion = CreateFixedPromotion("CUSTOMER", 10m, "SKU-1", budgetPerCustomerCap: 5m);

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.ReasonCode == "BudgetPerCustomerExceeded");
    }

    [Fact]
    public void Funding_split_is_included_in_kpi_effects()
    {
        var promotion = CreatePercentPromotion("FUNDED", 10m, "SKU-1", isFunded: true, fundingManufacturerRate: 0.6m, fundingRetailerRate: 0.4m);

        var quote = _engine.Evaluate(TestData.Quote(), new[] { promotion }, _now, false);
        var decision = quote.Promotions.Single(x => x.Status == "Applied");

        Assert.Equal(6m, decision.BudgetImpact);
        Assert.Equal(3.6m, decision.KpiEffect.ManufacturerFundingAmount);
        Assert.Equal(2.4m, decision.KpiEffect.RetailerFundingAmount);
        Assert.Equal(-2.4m, decision.KpiEffect.MarginDelta);
    }

    [Fact]
    public void Minimum_cart_value_and_maximum_discount_return_specific_reasons()
    {
        var minCartPromotion = CreateFixedPromotion("MIN-CART", 5m, "SKU-1", minimumCartValue: 500m);
        var maxDiscountPromotion = CreateFixedPromotion("MAX-DISCOUNT", 5m, "SKU-1", maximumDiscount: 2m);

        var quote = _engine.Evaluate(TestData.Quote(), new[] { minCartPromotion, maxDiscountPromotion }, _now, false);

        Assert.Contains(quote.Promotions, decision => decision.PromotionCode == "MIN-CART" && decision.ReasonCode == "MinCartValueNotMet");
        Assert.Contains(quote.Promotions, decision => decision.PromotionCode == "MAX-DISCOUNT" && decision.ReasonCode == "MaxDiscountExceeded");
    }

    private Promotion CreatePercentPromotion(
        string code,
        decimal value,
        string sku,
        bool isFunded = false,
        decimal fundingManufacturerRate = 0m,
        decimal fundingRetailerRate = 1m)
        => new()
        {
            Code = code,
            Name = code,
            Description = code,
            CampaignKey = code,
            Type = PromotionType.PercentDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            Value = value,
            DiscountValueType = DiscountValueType.Percentage,
            IsFunded = isFunded,
            FundingManufacturerRate = fundingManufacturerRate,
            FundingRetailerRate = fundingRetailerRate,
            TargetSkus = new[] { sku }
        };

    private Promotion CreateFixedPromotion(
        string code,
        decimal value,
        string sku,
        bool isCombinable = true,
        decimal budgetCap = 0m,
        decimal budgetDailyCap = 0m,
        decimal? budgetPerCustomerCap = null,
        decimal minimumCartValue = 0m,
        decimal? maximumDiscount = null)
        => new()
        {
            Code = code,
            Name = code,
            Description = code,
            CampaignKey = code,
            Type = PromotionType.FixedAmountDiscount,
            IsActive = true,
            StartsAtUtc = _now.AddDays(-1),
            EndsAtUtc = _now.AddDays(1),
            Value = value,
            IsCombinable = isCombinable,
            BudgetCap = budgetCap,
            BudgetDailyCap = budgetDailyCap,
            BudgetPerCustomerCap = budgetPerCustomerCap,
            MinimumCartValue = minimumCartValue,
            MaximumDiscount = maximumDiscount,
            FundingRetailerRate = 1m,
            TargetSkus = new[] { sku }
        };
}
