using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PromoEngine.Infrastructure.Persistence;

#nullable disable

namespace PromoEngine.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PromoEngineDbContext))]
[Migration("20260314220000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Promotions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                CampaignKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Priority = table.Column<int>(type: "int", nullable: false),
                IsFunded = table.Column<bool>(type: "bit", nullable: false),
                BudgetCap = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                BudgetConsumed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                DiscountValueType = table.Column<int>(type: "int", nullable: false),
                ThresholdAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RequiredQuantity = table.Column<int>(type: "int", nullable: false),
                ChargedQuantity = table.Column<int>(type: "int", nullable: false),
                BundlePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                MinimumMarginRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                CouponCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                TargetSkus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                BundleSkus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Promotions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "QuoteAudits",
            columns: table => new
            {
                QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsSimulation = table.Column<bool>(type: "bit", nullable: false),
                Strategy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                DiscountTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                NetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuoteAudits", x => x.QuoteId);
            });

        migrationBuilder.CreateTable(
            name: "PromotionRedemptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PromotionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RedeemedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PromotionRedemptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_PromotionRedemptions_Promotions_PromotionId",
                    column: x => x.PromotionId,
                    principalTable: "Promotions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PromotionRedemptions_PromotionId",
            table: "PromotionRedemptions",
            column: "PromotionId");

        migrationBuilder.CreateIndex(
            name: "IX_Promotions_Code",
            table: "Promotions",
            column: "Code",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PromotionRedemptions");
        migrationBuilder.DropTable(name: "QuoteAudits");
        migrationBuilder.DropTable(name: "Promotions");
    }
}
