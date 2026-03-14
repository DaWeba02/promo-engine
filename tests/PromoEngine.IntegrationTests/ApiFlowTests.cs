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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Promotions_crud_and_quote_flow_work()
    {
        if (factory.DockerUnavailable)
        {
            return;
        }

        using var client = factory.CreateClient();
        var promotionRequest = CreatePromotion();

        var createResponse = await client.PostAsJsonAsync("/promotions", promotionRequest, JsonOptions);
        await EnsureSuccessAsync(createResponse);
        var created = await createResponse.Content.ReadFromJsonAsync<PromotionDto>(JsonOptions);

        Assert.NotNull(created);

        var list = await client.GetFromJsonAsync<List<PromotionDto>>("/promotions", JsonOptions);
        Assert.Contains(list!, promotion => promotion.Id == created!.Id);

        var quoteResponse = await client.PostAsJsonAsync("/quotes", CreateQuoteRequest(), JsonOptions);
        await EnsureSuccessAsync(quoteResponse);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<QuoteResponseDto>(JsonOptions);

        Assert.NotNull(quote);
        Assert.False(quote!.IsSimulation);
        Assert.True(quote.Promotions.Count > 0);

        var deleteResponse = await client.DeleteAsync($"/promotions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Simulate_is_dry_run_and_health_endpoints_are_available()
    {
        if (factory.DockerUnavailable)
        {
            return;
        }

        using var client = factory.CreateClient();
        var simulateResponse = await client.PostAsJsonAsync("/simulate", CreateQuoteRequest(), JsonOptions);
        await EnsureSuccessAsync(simulateResponse);
        var quote = await simulateResponse.Content.ReadFromJsonAsync<QuoteResponseDto>(JsonOptions);

        Assert.True(quote!.IsSimulation);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ping")).StatusCode);
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

    private static UpsertPromotionRequest CreatePromotion() => new(
        Code: "PROMO-INT",
        Name: "Integration Promo",
        Description: "Integration test promotion",
        CampaignKey: "INTEGRATION",
        Type: PromotionType.PercentDiscount,
        IsActive: true,
        StartsAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
        EndsAtUtc: DateTimeOffset.UtcNow.AddHours(4),
        Priority: 100,
        IsFunded: true,
        BudgetCap: 1000m,
        BudgetConsumed: 0m,
        Value: 10m,
        DiscountValueType: DiscountValueType.Percentage,
        ThresholdAmount: 0m,
        RequiredQuantity: 0,
        ChargedQuantity: 0,
        BundlePrice: 0m,
        MinimumMarginRate: 0m,
        CouponCode: null,
        TargetSkus: new[] { "SKU-1" },
        BundleSkus: Array.Empty<string>());

    private static QuoteRequestDto CreateQuoteRequest() => new(
        CustomerId: "integration-customer",
        Currency: "EUR",
        CouponCode: null,
        Strategy: ConflictResolutionStrategy.CustomerBestPrice,
        MinimumMarginRate: 0m,
        Items: new[]
        {
            new QuoteLineRequestDto("SKU-1", 2, 20m, 8m, 20),
            new QuoteLineRequestDto("SKU-2", 1, 10m, 5m, 50)
        });
}
