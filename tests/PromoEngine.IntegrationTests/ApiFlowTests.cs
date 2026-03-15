using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PromoEngine.Application.Dtos;
using PromoEngine.Domain.Enums;
using PromoEngine.IntegrationTests.Infrastructure;

namespace PromoEngine.IntegrationTests;

[Collection("api")]
public sealed class ApiFlowTests(PromoEngineApiFactory factory)
{
    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Promotions_crud_persists_vnext_fields_and_quote_flow_work()
    {
        if (factory.DockerUnavailable)
        {
            return;
        }

        using var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var promotionRequest = CreatePromotion($"PROMO-{suffix}", $"SKU-{suffix}", channel: Channel.Online, segment: CustomerSegment.ExistingCustomer, fundingManufacturerRate: 0.6m, fundingRetailerRate: 0.4m, isCombinable: false, minimumCartValue: 10m, maximumDiscount: 25m);

        var createResponse = await client.PostAsJsonAsync("/promotions", promotionRequest, RequestJsonOptions);
        await EnsureSuccessAsync(createResponse);
        var created = await createResponse.Content.ReadFromJsonAsync<PromotionDto>(ResponseJsonOptions);

        Assert.NotNull(created);
        Assert.Equal(Channel.Online, created!.Channel);
        Assert.Equal(CustomerSegment.ExistingCustomer, created.Segment);
        Assert.False(created.IsCombinable);
        Assert.Equal(0.6m, created.FundingManufacturerRate);
        Assert.Equal(0.4m, created.FundingRetailerRate);

        var list = await client.GetFromJsonAsync<List<PromotionDto>>("/promotions", ResponseJsonOptions);
        Assert.Contains(list!, promotion => promotion.Id == created.Id && promotion.Channel == Channel.Online && promotion.Segment == CustomerSegment.ExistingCustomer);

        var quoteResponse = await client.PostAsJsonAsync("/quotes", CreateQuoteRequest($"customer-{suffix}", $"SKU-{suffix}"), RequestJsonOptions);
        await EnsureSuccessAsync(quoteResponse);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<QuoteResponseDto>(ResponseJsonOptions);

        Assert.NotNull(quote);
        Assert.False(quote!.IsSimulation);
        Assert.Contains(quote.Promotions, promotion => promotion.Status == "Applied");

        var deleteResponse = await client.DeleteAsync($"/promotions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Quotes_return_expected_channel_and_segment_mismatch_reasons()
    {
        if (factory.DockerUnavailable)
        {
            return;
        }

        using var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreatePromotionAsync(client, CreatePromotion($"CHANNEL-{suffix}", $"SKU-CHANNEL-{suffix}", channel: Channel.Store));
        await CreatePromotionAsync(client, CreatePromotion($"SEGMENT-{suffix}", $"SKU-SEGMENT-{suffix}", segment: CustomerSegment.Loyalty));

        var response = await client.PostAsJsonAsync(
            "/quotes",
            new QuoteRequestDto(
                $"customer-{suffix}",
                "EUR",
                null,
                ConflictResolutionStrategy.CustomerBestPrice,
                0m,
                new[]
                {
                    new QuoteLineRequestDto($"SKU-CHANNEL-{suffix}", 1, 20m, 10m, 10),
                    new QuoteLineRequestDto($"SKU-SEGMENT-{suffix}", 1, 20m, 10m, 10)
                },
                Channel.Online,
                CustomerSegment.ExistingCustomer),
            RequestJsonOptions);
        await EnsureSuccessAsync(response);
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponseDto>(ResponseJsonOptions);

        Assert.NotNull(quote);
        Assert.Contains(quote!.Promotions, promotion => promotion.PromotionCode == $"CHANNEL-{suffix}" && promotion.ReasonCode == "ChannelMismatch");
        Assert.Contains(quote.Promotions, promotion => promotion.PromotionCode == $"SEGMENT-{suffix}" && promotion.ReasonCode == "SegmentMismatch");
    }

    [Fact]
    public async Task Budget_daily_and_per_customer_caps_persist_for_quotes_but_not_simulate()
    {
        if (factory.DockerUnavailable)
        {
            return;
        }

        using var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sku = $"SKU-BUDGET-{suffix}";
        await CreatePromotionAsync(client, CreatePromotion($"BUDGET-{suffix}", sku, type: PromotionType.FixedAmountDiscount, value: 5m, budgetDailyCap: 10m, budgetPerCustomerCap: 5m));

        var firstQuote = await PostForQuoteAsync(client, "/quotes", CreateQuoteRequest($"customer-a-{suffix}", sku));
        Assert.Contains(firstQuote.Promotions, promotion => promotion.Status == "Applied");

        var simulation = await PostForQuoteAsync(client, "/simulate", CreateQuoteRequest($"customer-a-{suffix}", sku));
        Assert.Contains(simulation.Promotions, promotion => promotion.ReasonCode == "BudgetPerCustomerExceeded");

        var secondQuote = await PostForQuoteAsync(client, "/quotes", CreateQuoteRequest($"customer-b-{suffix}", sku));
        Assert.Contains(secondQuote.Promotions, promotion => promotion.Status == "Applied");

        var thirdQuote = await PostForQuoteAsync(client, "/quotes", CreateQuoteRequest($"customer-c-{suffix}", sku));
        Assert.Contains(thirdQuote.Promotions, promotion => promotion.ReasonCode == "BudgetDailyExceeded");
    }

    [Fact]
    public async Task Simulate_compare_returns_result_per_strategy()
    {
        if (factory.DockerUnavailable)
        {
            return;
        }

        using var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sku = $"SKU-COMPARE-{suffix}";
        await CreatePromotionAsync(client, CreatePromotion($"COMPARE-{suffix}", sku, type: PromotionType.PercentDiscount, value: 10m));

        var response = await client.PostAsJsonAsync(
            "/simulate/compare",
            new SimulationCompareRequestDto(
                $"customer-{suffix}",
                "EUR",
                null,
                new[] { ConflictResolutionStrategy.CustomerBestPrice, ConflictResolutionStrategy.MarginFirst },
                0m,
                new[] { new QuoteLineRequestDto(sku, 2, 20m, 10m, 10) },
                Channel.Online,
                CustomerSegment.ExistingCustomer),
            RequestJsonOptions);
        await EnsureSuccessAsync(response);
        var comparison = await response.Content.ReadFromJsonAsync<SimulationCompareResponseDto>(ResponseJsonOptions);

        Assert.NotNull(comparison);
        Assert.Equal(2, comparison!.Results.Count);
        Assert.All(comparison.Results, result =>
        {
            Assert.True(result.Totals.Subtotal > 0m);
            Assert.NotEmpty(result.Promotions);
        });
    }

    private static async Task CreatePromotionAsync(HttpClient client, UpsertPromotionRequest request)
    {
        var response = await client.PostAsJsonAsync("/promotions", request, RequestJsonOptions);
        await EnsureSuccessAsync(response);
    }

    private static async Task<QuoteResponseDto> PostForQuoteAsync(HttpClient client, string path, QuoteRequestDto request)
    {
        var response = await client.PostAsJsonAsync(path, request, RequestJsonOptions);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<QuoteResponseDto>(ResponseJsonOptions))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new Xunit.Sdk.XunitException($"Unexpected status {(int)response.StatusCode}: {body}");
    }

    private static UpsertPromotionRequest CreatePromotion(
        string code,
        string sku,
        PromotionType type = PromotionType.PercentDiscount,
        decimal value = 10m,
        Channel? channel = null,
        CustomerSegment? segment = null,
        bool isCombinable = true,
        decimal budgetDailyCap = 0m,
        decimal? budgetPerCustomerCap = null,
        decimal minimumCartValue = 0m,
        decimal? maximumDiscount = null,
        decimal? fundingManufacturerRate = null,
        decimal? fundingRetailerRate = null)
        => new(
            Code: code,
            Name: $"Name-{code}",
            Description: $"Description-{code}",
            CampaignKey: $"Campaign-{code}",
            Type: type,
            IsActive: true,
            StartsAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
            EndsAtUtc: DateTimeOffset.UtcNow.AddHours(4),
            Priority: 100,
            IsFunded: fundingManufacturerRate.GetValueOrDefault() > 0m,
            BudgetCap: 1000m,
            BudgetConsumed: 0m,
            Value: value,
            DiscountValueType: type == PromotionType.PercentDiscount ? DiscountValueType.Percentage : DiscountValueType.FixedAmount,
            ThresholdAmount: 0m,
            RequiredQuantity: 0,
            ChargedQuantity: 0,
            BundlePrice: 0m,
            MinimumMarginRate: 0m,
            CouponCode: null,
            TargetSkus: new[] { sku },
            BundleSkus: Array.Empty<string>(),
            Channel: channel,
            Segment: segment,
            IsCombinable: isCombinable,
            BudgetDailyCap: budgetDailyCap,
            BudgetPerCustomerCap: budgetPerCustomerCap,
            MinimumCartValue: minimumCartValue,
            MaximumDiscount: maximumDiscount,
            FundingManufacturerRate: fundingManufacturerRate,
            FundingRetailerRate: fundingRetailerRate);

    private static QuoteRequestDto CreateQuoteRequest(string customerId, string sku) => new(
        CustomerId: customerId,
        Currency: "EUR",
        CouponCode: null,
        Strategy: ConflictResolutionStrategy.CustomerBestPrice,
        MinimumMarginRate: 0m,
        Items: new[] { new QuoteLineRequestDto(sku, 1, 20m, 8m, 20) },
        Channel: Channel.Online,
        Segment: CustomerSegment.ExistingCustomer);
}
