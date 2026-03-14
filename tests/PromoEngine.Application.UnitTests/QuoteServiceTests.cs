using PromoEngine.Application.Abstractions;
using PromoEngine.Application.Dtos;
using PromoEngine.Application.Services;
using PromoEngine.Domain.Abstractions;
using PromoEngine.Domain.Enums;
using PromoEngine.Domain.Models;
using PromoEngine.Domain.Services;

namespace PromoEngine.Application.UnitTests;

public sealed class QuoteServiceTests
{
    [Fact]
    public async Task Quote_persists_audit_and_redemptions()
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
                TargetSkus = new[] { "SKU-1" }
            });

        var auditRepo = new FakeQuoteAuditRepository();
        var redemptionRepo = new FakePromotionRedemptionRepository();
        var service = new QuoteService(promotionRepo, auditRepo, redemptionRepo, new FakeUnitOfWork(), new PricingEngine(), new FakeClock());

        var result = await service.CreateQuoteAsync(CreateRequest(), false, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.QuoteId);
        Assert.Single(auditRepo.Items);
        Assert.Single(redemptionRepo.Items);
    }

    [Fact]
    public async Task Simulate_does_not_persist_side_effects()
    {
        var auditRepo = new FakeQuoteAuditRepository();
        var redemptionRepo = new FakePromotionRedemptionRepository();
        var service = new QuoteService(new InMemoryPromotionRepository(), auditRepo, redemptionRepo, new FakeUnitOfWork(), new PricingEngine(), new FakeClock());

        var result = await service.CreateQuoteAsync(CreateRequest(), true, CancellationToken.None);

        Assert.True(result.IsSimulation);
        Assert.Empty(auditRepo.Items);
        Assert.Empty(redemptionRepo.Items);
    }

    private static QuoteRequestDto CreateRequest() => new(
        "customer-1",
        "EUR",
        null,
        ConflictResolutionStrategy.CustomerBestPrice,
        0m,
        new[] { new QuoteLineRequestDto("SKU-1", 2, 25m, 10m, 10) });

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
