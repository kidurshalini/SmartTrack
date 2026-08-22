using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrack.Migrations
{
    /// <inheritdoc />
    public partial class stockmanagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmartTrackDailyUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HouseholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UsageDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdjustmentFactor = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    NormalUsage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ActualUsage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StockBefore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartTrackDailyUsages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmartTrackStockAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HouseholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdjustmentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartTrackStockAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmartTrackStockStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HouseholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NormalDailyConsumption = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LastPurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPurchaseQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LastProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAdjustmentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastAdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartTrackStockStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmartTrackDailyUsages_HouseholdId_ProductName_UsageDate",
                table: "SmartTrackDailyUsages",
                columns: new[] { "HouseholdId", "ProductName", "UsageDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmartTrackStockAdjustments_HouseholdId_ProductName_AdjustmentDate",
                table: "SmartTrackStockAdjustments",
                columns: new[] { "HouseholdId", "ProductName", "AdjustmentDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmartTrackStockStates_HouseholdId_ProductName",
                table: "SmartTrackStockStates",
                columns: new[] { "HouseholdId", "ProductName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartTrackDailyUsages");

            migrationBuilder.DropTable(
                name: "SmartTrackStockAdjustments");

            migrationBuilder.DropTable(
                name: "SmartTrackStockStates");
        }
    }
}
