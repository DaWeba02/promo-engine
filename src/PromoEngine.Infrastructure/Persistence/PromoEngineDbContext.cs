using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PromoEngine.Application.Abstractions;

namespace PromoEngine.Infrastructure.Persistence;

public sealed class PromoEngineDbContext(DbContextOptions<PromoEngineDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<PromotionRedemptionEntity> PromotionRedemptions => Set<PromotionRedemptionEntity>();
    public DbSet<QuoteAuditEntity> QuoteAudits => Set<QuoteAuditEntity>();
    public DbSet<BudgetConsumptionEntity> BudgetConsumptions => Set<BudgetConsumptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PromoEngineDbContext).Assembly);
    }
}

public sealed class PromoEngineDbContextFactory : IDesignTimeDbContextFactory<PromoEngineDbContext>
{
    public PromoEngineDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PromoEngineDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,14333;Database=PromoEngine;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False");
        return new PromoEngineDbContext(optionsBuilder.Options);
    }
}

public sealed class PromotionEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CampaignKey { get; set; } = string.Empty;
    public int Type { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public int Priority { get; set; }
    public bool IsFunded { get; set; }
    public int? Channel { get; set; }
    public int? Segment { get; set; }
    public bool IsCombinable { get; set; }
    public decimal BudgetCap { get; set; }
    public decimal BudgetConsumed { get; set; }
    public decimal BudgetDailyCap { get; set; }
    public decimal? BudgetPerCustomerCap { get; set; }
    public decimal Value { get; set; }
    public int DiscountValueType { get; set; }
    public decimal ThresholdAmount { get; set; }
    public int RequiredQuantity { get; set; }
    public int ChargedQuantity { get; set; }
    public decimal BundlePrice { get; set; }
    public decimal MinimumMarginRate { get; set; }
    public decimal MinimumCartValue { get; set; }
    public decimal? MaximumDiscount { get; set; }
    public decimal FundingManufacturerRate { get; set; }
    public decimal FundingRetailerRate { get; set; }
    public string? CouponCode { get; set; }
    public string TargetSkus { get; set; } = string.Empty;
    public string BundleSkus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class PromotionRedemptionEntity
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Guid PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTimeOffset RedeemedAtUtc { get; set; }
    public PromotionEntity? Promotion { get; set; }
}

public sealed class QuoteAuditEntity
{
    public Guid QuoteId { get; set; }
    public bool IsSimulation { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal NetTotal { get; set; }
    public string RequestJson { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class BudgetConsumptionEntity
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public DateOnly ConsumptionDateUtc { get; set; }
    public string? CustomerId { get; set; }
    public decimal ConsumedAmount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public PromotionEntity? Promotion { get; set; }
}
