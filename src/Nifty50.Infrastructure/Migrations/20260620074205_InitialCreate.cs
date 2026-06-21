using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nifty50.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoringProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsPreset = table.Column<bool>(type: "boolean", nullable: false),
                    TechnicalWeight = table.Column<int>(type: "integer", nullable: false),
                    FundamentalWeight = table.Column<int>(type: "integer", nullable: false),
                    SentimentWeight = table.Column<int>(type: "integer", nullable: false),
                    DividendWeight = table.Column<int>(type: "integer", nullable: false),
                    TechRSIWeight = table.Column<int>(type: "integer", nullable: false),
                    TechMACDWeight = table.Column<int>(type: "integer", nullable: false),
                    TechMovingAvgWeight = table.Column<int>(type: "integer", nullable: false),
                    TechBollingerWeight = table.Column<int>(type: "integer", nullable: false),
                    TechADXWeight = table.Column<int>(type: "integer", nullable: false),
                    TechVolumeWeight = table.Column<int>(type: "integer", nullable: false),
                    FundValuationWeight = table.Column<int>(type: "integer", nullable: false),
                    FundProfitabilityWeight = table.Column<int>(type: "integer", nullable: false),
                    FundLiquidityWeight = table.Column<int>(type: "integer", nullable: false),
                    FundLeverageWeight = table.Column<int>(type: "integer", nullable: false),
                    FundGrowthWeight = table.Column<int>(type: "integer", nullable: false),
                    AlertMinOverallScore = table.Column<int>(type: "integer", nullable: false),
                    AlertMinTechnicalScore = table.Column<int>(type: "integer", nullable: false),
                    AlertMinFundamentalScore = table.Column<int>(type: "integer", nullable: false),
                    AlertMinSentimentScore = table.Column<int>(type: "integer", nullable: false),
                    StrongBuyThreshold = table.Column<int>(type: "integer", nullable: false),
                    BuyThreshold = table.Column<int>(type: "integer", nullable: false),
                    HoldThreshold = table.Column<int>(type: "integer", nullable: false),
                    SellThreshold = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MarketCap = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    DayChange = table.Column<decimal>(type: "numeric", nullable: true),
                    DayChangePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    Week52High = table.Column<decimal>(type: "numeric", nullable: true),
                    Week52Low = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dividends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dividends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dividends_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementType = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAssets = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalLiabilities = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalEquity = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentAssets = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentLiabilities = table.Column<decimal>(type: "numeric", nullable: true),
                    CashAndEquivalents = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalDebt = table.Column<decimal>(type: "numeric", nullable: true),
                    NetDebt = table.Column<decimal>(type: "numeric", nullable: true),
                    Inventory = table.Column<decimal>(type: "numeric", nullable: true),
                    AccountsReceivable = table.Column<decimal>(type: "numeric", nullable: true),
                    AccountsPayable = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalRevenue = table.Column<decimal>(type: "numeric", nullable: true),
                    GrossProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    OperatingIncome = table.Column<decimal>(type: "numeric", nullable: true),
                    NetIncome = table.Column<decimal>(type: "numeric", nullable: true),
                    EBITDA = table.Column<decimal>(type: "numeric", nullable: true),
                    EarningsPerShare = table.Column<decimal>(type: "numeric", nullable: true),
                    DilutedEPS = table.Column<decimal>(type: "numeric", nullable: true),
                    CostOfRevenue = table.Column<decimal>(type: "numeric", nullable: true),
                    OperatingExpenses = table.Column<decimal>(type: "numeric", nullable: true),
                    InterestExpense = table.Column<decimal>(type: "numeric", nullable: true),
                    TaxProvision = table.Column<decimal>(type: "numeric", nullable: true),
                    OperatingCashFlow = table.Column<decimal>(type: "numeric", nullable: true),
                    CapitalExpenditures = table.Column<decimal>(type: "numeric", nullable: true),
                    FreeCashFlow = table.Column<decimal>(type: "numeric", nullable: true),
                    DividendsPaid = table.Column<decimal>(type: "numeric", nullable: true),
                    ShareRepurchases = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialStatements_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundamentalMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PERatio = table.Column<decimal>(type: "numeric", nullable: true),
                    PBRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    PSRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    EVToEBITDA = table.Column<decimal>(type: "numeric", nullable: true),
                    EVToFCF = table.Column<decimal>(type: "numeric", nullable: true),
                    PEGRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    ROE = table.Column<decimal>(type: "numeric", nullable: true),
                    ROA = table.Column<decimal>(type: "numeric", nullable: true),
                    ROIC = table.Column<decimal>(type: "numeric", nullable: true),
                    GrossProfitMargin = table.Column<decimal>(type: "numeric", nullable: true),
                    OperatingMargin = table.Column<decimal>(type: "numeric", nullable: true),
                    NetProfitMargin = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    QuickRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    CashRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    DebtToEquity = table.Column<decimal>(type: "numeric", nullable: true),
                    DebtToAssets = table.Column<decimal>(type: "numeric", nullable: true),
                    InterestCoverageRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    AssetTurnover = table.Column<decimal>(type: "numeric", nullable: true),
                    InventoryTurnover = table.Column<decimal>(type: "numeric", nullable: true),
                    ReceivablesTurnover = table.Column<decimal>(type: "numeric", nullable: true),
                    EPS = table.Column<decimal>(type: "numeric", nullable: true),
                    BookValuePerShare = table.Column<decimal>(type: "numeric", nullable: true),
                    FreeCashFlowPerShare = table.Column<decimal>(type: "numeric", nullable: true),
                    DividendYield = table.Column<decimal>(type: "numeric", nullable: true),
                    DividendPayoutRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    RevenueGrowthYoY = table.Column<decimal>(type: "numeric", nullable: true),
                    EarningsGrowthYoY = table.Column<decimal>(type: "numeric", nullable: true),
                    FCFGrowthYoY = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundamentalMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundamentalMetrics_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SentimentAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OverallSentiment = table.Column<int>(type: "integer", nullable: false),
                    SentimentScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PositiveCount = table.Column<int>(type: "integer", nullable: false),
                    NegativeCount = table.Column<int>(type: "integer", nullable: false),
                    NeutralCount = table.Column<int>(type: "integer", nullable: false),
                    TopHeadlines = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentimentAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentimentAnalyses_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoringProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TechnicalSignal = table.Column<int>(type: "integer", nullable: false),
                    FundamentalSignal = table.Column<int>(type: "integer", nullable: false),
                    SentimentSignal = table.Column<int>(type: "integer", nullable: false),
                    OverallSignal = table.Column<int>(type: "integer", nullable: false),
                    TechnicalScore = table.Column<int>(type: "integer", nullable: false),
                    FundamentalScore = table.Column<int>(type: "integer", nullable: false),
                    SentimentScore = table.Column<int>(type: "integer", nullable: false),
                    DividendScore = table.Column<int>(type: "integer", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    WeightsUsed = table.Column<string>(type: "jsonb", nullable: true),
                    Reasoning = table.Column<string>(type: "text", nullable: true),
                    IsAlert = table.Column<bool>(type: "boolean", nullable: false),
                    AlertMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAnalyses_ScoringProfiles_ScoringProfileId",
                        column: x => x.ScoringProfileId,
                        principalTable: "ScoringProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAnalyses_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric", nullable: false),
                    High = table.Column<decimal>(type: "numeric", nullable: false),
                    Low = table.Column<decimal>(type: "numeric", nullable: false),
                    Close = table.Column<decimal>(type: "numeric", nullable: false),
                    AdjClose = table.Column<decimal>(type: "numeric", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPrices_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SMA20 = table.Column<decimal>(type: "numeric", nullable: true),
                    SMA50 = table.Column<decimal>(type: "numeric", nullable: true),
                    SMA200 = table.Column<decimal>(type: "numeric", nullable: true),
                    EMA12 = table.Column<decimal>(type: "numeric", nullable: true),
                    EMA26 = table.Column<decimal>(type: "numeric", nullable: true),
                    RSI14 = table.Column<decimal>(type: "numeric", nullable: true),
                    MACD = table.Column<decimal>(type: "numeric", nullable: true),
                    MACDSignal = table.Column<decimal>(type: "numeric", nullable: true),
                    MACDHistogram = table.Column<decimal>(type: "numeric", nullable: true),
                    BollingerUpper = table.Column<decimal>(type: "numeric", nullable: true),
                    BollingerMiddle = table.Column<decimal>(type: "numeric", nullable: true),
                    BollingerLower = table.Column<decimal>(type: "numeric", nullable: true),
                    ATR14 = table.Column<decimal>(type: "numeric", nullable: true),
                    ADX14 = table.Column<decimal>(type: "numeric", nullable: true),
                    StochK = table.Column<decimal>(type: "numeric", nullable: true),
                    StochD = table.Column<decimal>(type: "numeric", nullable: true),
                    OBV = table.Column<decimal>(type: "numeric", nullable: true),
                    VWAP = table.Column<decimal>(type: "numeric", nullable: true),
                    MFI14 = table.Column<decimal>(type: "numeric", nullable: true),
                    CCI20 = table.Column<decimal>(type: "numeric", nullable: true),
                    WilliamsR14 = table.Column<decimal>(type: "numeric", nullable: true),
                    ParabolicSar = table.Column<decimal>(type: "numeric", nullable: true),
                    IchimokuTenkan = table.Column<decimal>(type: "numeric", nullable: true),
                    IchimokuKijun = table.Column<decimal>(type: "numeric", nullable: true),
                    IchimokuSenkouA = table.Column<decimal>(type: "numeric", nullable: true),
                    IchimokuSenkouB = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalIndicators_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dividends_StockId",
                table: "Dividends",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatements_StockId_StatementType_Period_PeriodEndD~",
                table: "FinancialStatements",
                columns: new[] { "StockId", "StatementType", "Period", "PeriodEndDate" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_FundamentalMetrics_StockId_PeriodEndDate",
                table: "FundamentalMetrics",
                columns: new[] { "StockId", "PeriodEndDate" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringProfiles_Name",
                table: "ScoringProfiles",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SentimentAnalyses_StockId_AnalyzedAt",
                table: "SentimentAnalyses",
                columns: new[] { "StockId", "AnalyzedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockAnalyses_ScoringProfileId",
                table: "StockAnalyses",
                column: "ScoringProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAnalyses_StockId_AnalyzedAt",
                table: "StockAnalyses",
                columns: new[] { "StockId", "AnalyzedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockPrices_StockId_Date",
                table: "StockPrices",
                columns: new[] { "StockId", "Date" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Symbol",
                table: "Stocks",
                column: "Symbol",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicators_StockId_Date",
                table: "TechnicalIndicators",
                columns: new[] { "StockId", "Date" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dividends");

            migrationBuilder.DropTable(
                name: "FinancialStatements");

            migrationBuilder.DropTable(
                name: "FundamentalMetrics");

            migrationBuilder.DropTable(
                name: "SentimentAnalyses");

            migrationBuilder.DropTable(
                name: "StockAnalyses");

            migrationBuilder.DropTable(
                name: "StockPrices");

            migrationBuilder.DropTable(
                name: "TechnicalIndicators");

            migrationBuilder.DropTable(
                name: "ScoringProfiles");

            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
