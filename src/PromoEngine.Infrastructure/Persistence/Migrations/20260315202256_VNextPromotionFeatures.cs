using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromoEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VNextPromotionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetDailyCap",
                table: "Promotions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPerCustomerCap",
                table: "Promotions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FundingManufacturerRate",
                table: "Promotions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FundingRetailerRate",
                table: "Promotions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCombinable",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumDiscount",
                table: "Promotions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumCartValue",
                table: "Promotions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Segment",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE Promotions
SET FundingManufacturerRate = CASE WHEN IsFunded = 1 THEN 0.5 ELSE 0 END,
    FundingRetailerRate = CASE WHEN IsFunded = 1 THEN 0.5 ELSE 1 END
WHERE FundingManufacturerRate = 0 AND FundingRetailerRate = 1;");

            migrationBuilder.CreateTable(
                name: "BudgetConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumptionDateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ConsumedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetConsumptions_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetConsumptions_PromotionId_ConsumptionDateUtc",
                table: "BudgetConsumptions",
                columns: new[] { "PromotionId", "ConsumptionDateUtc" },
                unique: true,
                filter: "[CustomerId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetConsumptions_PromotionId_ConsumptionDateUtc_CustomerId",
                table: "BudgetConsumptions",
                columns: new[] { "PromotionId", "ConsumptionDateUtc", "CustomerId" },
                unique: true,
                filter: "[CustomerId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetConsumptions");

            migrationBuilder.DropColumn(
                name: "BudgetDailyCap",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "BudgetPerCustomerCap",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "FundingManufacturerRate",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "FundingRetailerRate",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsCombinable",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaximumDiscount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MinimumCartValue",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Segment",
                table: "Promotions");
        }
    }
}
