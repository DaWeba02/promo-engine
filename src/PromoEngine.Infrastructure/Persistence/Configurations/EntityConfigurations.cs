using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromoEngine.Infrastructure.Persistence;

namespace PromoEngine.Infrastructure.Persistence.Configurations;

public sealed class PromotionEntityConfiguration : IEntityTypeConfiguration<PromotionEntity>
{
    public void Configure(EntityTypeBuilder<PromotionEntity> builder)
    {
        builder.ToTable("Promotions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.CampaignKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CouponCode).HasMaxLength(128);
        builder.Property(x => x.TargetSkus).HasColumnType("nvarchar(max)");
        builder.Property(x => x.BundleSkus).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IsCombinable).HasDefaultValue(true);
        builder.Property(x => x.BudgetCap).HasPrecision(18, 2);
        builder.Property(x => x.BudgetConsumed).HasPrecision(18, 2);
        builder.Property(x => x.BudgetDailyCap).HasPrecision(18, 2);
        builder.Property(x => x.BudgetPerCustomerCap).HasPrecision(18, 2);
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.Property(x => x.ThresholdAmount).HasPrecision(18, 2);
        builder.Property(x => x.BundlePrice).HasPrecision(18, 2);
        builder.Property(x => x.MinimumMarginRate).HasPrecision(18, 4);
        builder.Property(x => x.MinimumCartValue).HasPrecision(18, 2);
        builder.Property(x => x.MaximumDiscount).HasPrecision(18, 2);
        builder.Property(x => x.FundingManufacturerRate).HasPrecision(18, 4);
        builder.Property(x => x.FundingRetailerRate).HasPrecision(18, 4);
    }
}

public sealed class PromotionRedemptionEntityConfiguration : IEntityTypeConfiguration<PromotionRedemptionEntity>
{
    public void Configure(EntityTypeBuilder<PromotionRedemptionEntity> builder)
    {
        builder.ToTable("PromotionRedemptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PromotionCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.Promotion)
            .WithMany()
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class QuoteAuditEntityConfiguration : IEntityTypeConfiguration<QuoteAuditEntity>
{
    public void Configure(EntityTypeBuilder<QuoteAuditEntity> builder)
    {
        builder.ToTable("QuoteAudits");
        builder.HasKey(x => x.QuoteId);
        builder.Property(x => x.Strategy).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
        builder.Property(x => x.NetTotal).HasPrecision(18, 2);
        builder.Property(x => x.RequestJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ResponseJson).HasColumnType("nvarchar(max)");
    }
}

public sealed class BudgetConsumptionEntityConfiguration : IEntityTypeConfiguration<BudgetConsumptionEntity>
{
    public void Configure(EntityTypeBuilder<BudgetConsumptionEntity> builder)
    {
        builder.ToTable("BudgetConsumptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerId).HasMaxLength(128);
        builder.Property(x => x.ConsumedAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.PromotionId, x.ConsumptionDateUtc, x.CustomerId })
            .IsUnique()
            .HasFilter("[CustomerId] IS NOT NULL");
        builder.HasIndex(x => new { x.PromotionId, x.ConsumptionDateUtc })
            .IsUnique()
            .HasFilter("[CustomerId] IS NULL");
        builder.HasOne(x => x.Promotion)
            .WithMany()
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
