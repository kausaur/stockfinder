using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nifty50.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLongTermInvestorEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QualityScore",
                table: "StockAnalyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualitySignal",
                table: "StockAnalyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValuationScore",
                table: "StockAnalyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValuationSignal",
                table: "StockAnalyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FundPEGWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FundROCEWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualAltmanWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualDividendConsistencyWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualFCFTrendWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualFIIWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualPiotroskiWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualPromoterWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualityWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValuationWeight",
                table: "ScoringProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "IndexMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndexName = table.Column<string>(type: "text", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndexMemberships_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntrinsicValuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrahamNumber = table.Column<decimal>(type: "numeric", nullable: true),
                    GrahamMarginOfSafety = table.Column<decimal>(type: "numeric", nullable: true),
                    PEGRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    EarningsPowerValue = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedFairValue = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    UpsidePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    ValuationVerdict = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntrinsicValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntrinsicValuations_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsOfDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PiotroskiFScore = table.Column<int>(type: "integer", nullable: true),
                    PiotroskiBreakdown = table.Column<string>(type: "text", nullable: true),
                    AltmanZScore = table.Column<decimal>(type: "numeric", nullable: true),
                    AltmanZone = table.Column<string>(type: "text", nullable: true),
                    PromoterHolding = table.Column<decimal>(type: "numeric", nullable: true),
                    PromoterPledgePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    PromoterHoldingTrend = table.Column<string>(type: "text", nullable: true),
                    FIIHolding = table.Column<decimal>(type: "numeric", nullable: true),
                    FIIHoldingTrend = table.Column<string>(type: "text", nullable: true),
                    DIIHolding = table.Column<decimal>(type: "numeric", nullable: true),
                    ConsecutiveProfitYears = table.Column<int>(type: "integer", nullable: true),
                    ConsecutiveDividendYears = table.Column<int>(type: "integer", nullable: true),
                    ROCELatest = table.Column<decimal>(type: "numeric", nullable: true),
                    FCFTrend = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityMetrics_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScoringProfileName = table.Column<string>(type: "text", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    FundamentalScore = table.Column<int>(type: "integer", nullable: false),
                    TechnicalScore = table.Column<int>(type: "integer", nullable: false),
                    SentimentScore = table.Column<int>(type: "integer", nullable: false),
                    DividendScore = table.Column<int>(type: "integer", nullable: false),
                    ValuationScore = table.Column<int>(type: "integer", nullable: true),
                    QualityScore = table.Column<int>(type: "integer", nullable: true),
                    Signal = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreHistories_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectorBenchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sector = table.Column<string>(type: "text", nullable: false),
                    AsOfDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MedianPE = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianPB = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianEVToEBITDA = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianROE = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianROCE = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianOperatingMargin = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianEPSGrowth = table.Column<decimal>(type: "numeric", nullable: true),
                    MedianRevenueGrowth = table.Column<decimal>(type: "numeric", nullable: true),
                    TypicalDebtToEquity = table.Column<decimal>(type: "numeric", nullable: true),
                    IsBFSI = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorBenchmarks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndexMemberships_IndexName_StockId",
                table: "IndexMemberships",
                columns: new[] { "IndexName", "StockId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_IndexMemberships_StockId",
                table: "IndexMemberships",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_IntrinsicValuations_StockId_ComputedAt",
                table: "IntrinsicValuations",
                columns: new[] { "StockId", "ComputedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_QualityMetrics_StockId_AsOfDate",
                table: "QualityMetrics",
                columns: new[] { "StockId", "AsOfDate" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreHistories_StockId_RecordedAt",
                table: "ScoreHistories",
                columns: new[] { "StockId", "RecordedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SectorBenchmarks_Sector_AsOfDate",
                table: "SectorBenchmarks",
                columns: new[] { "Sector", "AsOfDate" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexMemberships");

            migrationBuilder.DropTable(
                name: "IntrinsicValuations");

            migrationBuilder.DropTable(
                name: "QualityMetrics");

            migrationBuilder.DropTable(
                name: "ScoreHistories");

            migrationBuilder.DropTable(
                name: "SectorBenchmarks");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "StockAnalyses");

            migrationBuilder.DropColumn(
                name: "QualitySignal",
                table: "StockAnalyses");

            migrationBuilder.DropColumn(
                name: "ValuationScore",
                table: "StockAnalyses");

            migrationBuilder.DropColumn(
                name: "ValuationSignal",
                table: "StockAnalyses");

            migrationBuilder.DropColumn(
                name: "FundPEGWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "FundROCEWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "QualAltmanWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "QualDividendConsistencyWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "QualFCFTrendWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "QualFIIWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "QualPiotroskiWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "QualPromoterWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "QualityWeight",
                table: "ScoringProfiles");

            migrationBuilder.DropColumn(
                name: "ValuationWeight",
                table: "ScoringProfiles");
        }
    }
}
