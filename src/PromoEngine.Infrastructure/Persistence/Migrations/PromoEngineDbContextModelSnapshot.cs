using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PromoEngine.Infrastructure.Persistence;

#nullable disable

namespace PromoEngine.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PromoEngineDbContext))]
partial class PromoEngineDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("PromoEngine.Infrastructure.Persistence.PromotionEntity", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
                b.Property<decimal>("BudgetCap").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.Property<decimal>("BudgetConsumed").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.Property<decimal>("BundlePrice").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.Property<string>("BundleSkus").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("CampaignKey").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
                b.Property<int>("ChargedQuantity").HasColumnType("int");
                b.Property<string>("Code").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
                b.Property<string>("CouponCode").HasMaxLength(128).HasColumnType("nvarchar(128)");
                b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
                b.Property<string>("Description").IsRequired().HasMaxLength(1024).HasColumnType("nvarchar(1024)");
                b.Property<int>("DiscountValueType").HasColumnType("int");
                b.Property<DateTimeOffset>("EndsAtUtc").HasColumnType("datetimeoffset");
                b.Property<bool>("IsActive").HasColumnType("bit");
                b.Property<bool>("IsFunded").HasColumnType("bit");
                b.Property<decimal>("MinimumMarginRate").HasPrecision(18, 4).HasColumnType("decimal(18,4)");
                b.Property<string>("Name").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
                b.Property<int>("Priority").HasColumnType("int");
                b.Property<int>("RequiredQuantity").HasColumnType("int");
                b.Property<DateTimeOffset>("StartsAtUtc").HasColumnType("datetimeoffset");
                b.Property<string>("TargetSkus").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<decimal>("ThresholdAmount").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.Property<int>("Type").HasColumnType("int");
                b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("datetimeoffset");
                b.Property<decimal>("Value").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.HasKey("Id");
                b.HasIndex("Code").IsUnique();
                b.ToTable("Promotions", (string)null);
            });

        modelBuilder.Entity("PromoEngine.Infrastructure.Persistence.PromotionRedemptionEntity", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
                b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.Property<Guid>("PromotionId").HasColumnType("uniqueidentifier");
                b.Property<string>("PromotionCode").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
                b.Property<Guid>("QuoteId").HasColumnType("uniqueidentifier");
                b.Property<DateTimeOffset>("RedeemedAtUtc").HasColumnType("datetimeoffset");
                b.HasKey("Id");
                b.HasIndex("PromotionId");
                b.ToTable("PromotionRedemptions", (string)null);
            });

        modelBuilder.Entity("PromoEngine.Infrastructure.Persistence.QuoteAuditEntity", b =>
            {
                b.Property<Guid>("QuoteId").ValueGeneratedNever().HasColumnType("uniqueidentifier");
                b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
                b.Property<decimal>("DiscountTotal").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.Property<bool>("IsSimulation").HasColumnType("bit");
                b.Property<decimal>("NetTotal").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.Property<string>("RequestJson").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("ResponseJson").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("Strategy").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
                b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
                b.HasKey("QuoteId");
                b.ToTable("QuoteAudits", (string)null);
            });

        modelBuilder.Entity("PromoEngine.Infrastructure.Persistence.PromotionRedemptionEntity", b =>
            {
                b.HasOne("PromoEngine.Infrastructure.Persistence.PromotionEntity", "Promotion")
                    .WithMany()
                    .HasForeignKey("PromotionId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Promotion");
            });
#pragma warning restore 612, 618
    }
}
