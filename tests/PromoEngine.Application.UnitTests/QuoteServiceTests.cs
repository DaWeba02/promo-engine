using PromoEngine.Application.Abstractions;
using PromoEngine.Application.Dtos;
using PromoEngine.Application.Services;
using PromoEngine.Domain.Enums;
using PromoEngine.Domain.Models;
using PromoEngine.Domain.Services;

namespace PromoEngine.Application.UnitTests;

public sealed class QuoteServiceTests
{
    [Fact]
    public async Task Quote_persists_audit_redemptions_and_budget_consumption()
    {
        var promotionRepo = new InMemoryPromotionRepository(
            new Promotion
            {
                Code = "PERCENT",
                Name = "Percent",
                Description = "Discount",
                CampaignKey = "SPRING",
                Type = PromotionType.PercentDiscount,
                IsActive = true,
                StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                EndsAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                Value = 10m,
                DiscountValueType = DiscountValueType.Percentage,
                FundingRetailerRate = 1m,
                TargetSkus = new[] { "SKU-1" }
            });

        var auditRepo = new FakeQuoteAuditRepository();
        var budgetRepo = new FakeBudgetConsumptionRepository();
        var redemptionRepo = new FakePromotionRedemptionRepository();
        var service = new QuoteService(promotionRepo, auditRepo, budgetRepo, redemptionRepo, new FakeUnitOfWork(), new PricingEngine(), new FakeClock());

        var result = await service.CreateQuoteAsync(CreateRequest(), false, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.QuoteId);
        Assert.Single(auditRepo.Items);
        Assert.Single(redemptionRepo.Items);
        Assert.Single(budgetRepo.AddedEntries);
    }

    [Fact]
    public async Task Simulate_does_not_persist_side_effects()
    {
        var auditRepo = new FakeQuoteAuditRepository();
        var budgetRepo = new FakeBudgetConsumptionRepository();
        var redemptionRepo = new FakePromotionRedemptionRepository();
        var service = new QuoteService(new InMemoryPromotionRepository(), auditRepo, budgetRepo, redemptionRepo, new FakeUnitOfWork(), new PricingEngine(), new FakeClock());

        var result = await service.CreateQuoteAsync(CreateRequest(), true, CancellationToken.None);

        Assert.True(result.IsSimulation);
        Assert.Empty(auditRepo.Items);
        Assert.Empty(redemptionRepo.Items);
        Assert.Empty(budgetRepo.AddedEntries);
    }

    [Fact]
    public async Task Compare_returns_one_result_per_strategy_and_remains_dry_run()
    {
        var promotionRepo = new InMemoryPromotionRepository(
            new Promotion
            {
                Code = "PERCENT",
                Name = "Percent",
                Description = "Discount",
                CampaignKey = "SPRING",
                Type = PromotionType.PercentDiscount,
                IsActive = true,
                StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                EndsAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                Value = 10m,
                DiscountValueType = DiscountValueType.Percentage,
                FundingRetailerRate = 1m,
                TargetSkus = new[] { "SKU-1" }
            });
        var auditRepo = new FakeQuoteAuditRepository();
        var budgetRepo = new FakeBudgetConsumptionRepository();
        var redemptionRepo = new FakePromotionRedemptionRepository();
        var service = new QuoteService(promotionRepo, auditRepo, budgetRepo, redemptionRepo, new FakeUnitOfWork(), new PricingEngine(), new FakeClock());

        var response = await service.CompareStrategiesAsync(CreateCompareRequest(), CancellationToken.None);

        Assert.Equal(2, response.Results.Count);
        Assert.Empty(auditRepo.Items);
        Assert.Empty(redemptionRepo.Items);
        Assert.Empty(budgetRepo.AddedEntries);
    }

    private static QuoteRequestDto CreateRequest() => new(
        "customer-1",
        "EUR",
        null,
        ConflictResolutionStrategy.CustomerBestPrice,
        0m,
        new[] { new QuoteLineRequestDto("SKU-1", 2, 25m, 10m, 10) },
        Channel.Online,
        CustomerSegment.ExistingCustomer);

    private static SimulationCompareRequestDto CreateCompareRequest() => new(
        "customer-1",
        "EUR",
        null,
        new[] { ConflictResolutionStrategy.CustomerBestPrice, ConflictResolutionStrategy.MarginFirst },
        0m,
        new[] { new QuoteLineRequestDto("SKU-1", 2, 25m, 10m, 10) },
        Channel.Online,
        CustomerSegment.ExistingCustomer);

    private sealed class InMemoryPromotionRepository(params Promotion[] promotions) : IPromotionRepository
    {
        private readonly List<Promotion> _promotions = promotions.ToList();

        public Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Promotion>>(_promotions);
        public Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Promotion>>(_promotions);
        public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_promotions.FirstOrDefault(x => x.Id == id));
        public Task AddAsync(Promotion promotion, CancellationToken cancellationToken) { _promotions.Add(promotion); return Task.CompletedTask; }
        public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeQuoteAuditRepository : IQuoteAuditRepository
    {
        public List<QuoteAuditEntry> Items { get; } = new();
        public Task AddAsync(QuoteAuditEntry entry, CancellationToken cancellationToken) { Items.Add(entry); return Task.CompletedTask; }
    }

    private sealed class FakeBudgetConsumptionRepository : IBudgetConsumptionRepository
    {
        public List<BudgetConsumptionEntry> AddedEntries { get; } = new();

        public Task<IReadOnlyDictionary<Guid, BudgetConsumptionSnapshot>> GetSnapshotsAsync(
            IReadOnlyCollection<Guid> promotionIds,
            DateOnly consumptionDateUtc,
            string customerId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, BudgetConsumptionSnapshot>>(new Dictionary<Guid, BudgetConsumptionSnapshot>());

        public Task AddRangeAsync(IEnumerable<BudgetConsumptionEntry> entries, CancellationToken cancellationToken)
        {
            AddedEntries.AddRange(entries);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePromotionRedemptionRepository : IPromotionRedemptionRepository
    {
        public List<PromotionRedemptionEntry> Items { get; } = new();
        public Task AddRangeAsync(IEnumerable<PromotionRedemptionEntry> entries, CancellationToken cancellationToken) { Items.AddRange(entries); return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
