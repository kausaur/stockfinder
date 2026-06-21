namespace Nifty50.Core.DTOs;

public record StockDto(Guid Id, string Symbol, string CompanyName, string? Sector, string? Industry,
    decimal? MarketCap, decimal? CurrentPrice, decimal? DayChange, decimal? DayChangePercent,
    decimal? Week52High, decimal? Week52Low, bool IsActive);

public record StockListDto(Guid Id, string Symbol, string CompanyName, string? Sector,
    decimal? CurrentPrice, decimal? DayChangePercent, decimal? MarketCap, string? OverallSignal, int? OverallScore);

public record StockPriceDto(DateTime Date, decimal Open, decimal High, decimal Low, decimal Close, decimal AdjClose, long Volume);

public record DividendDto(DateTime ExDate, decimal Amount);

public record FinancialStatementDto(string StatementType, string Period, DateTime PeriodEndDate,
    decimal? TotalAssets, decimal? TotalLiabilities, decimal? TotalEquity, decimal? CurrentAssets,
    decimal? CurrentLiabilities, decimal? CashAndEquivalents, decimal? TotalDebt, decimal? NetDebt,
    decimal? TotalRevenue, decimal? GrossProfit, decimal? OperatingIncome, decimal? NetIncome,
    decimal? EBITDA, decimal? EarningsPerShare, decimal? CostOfRevenue,
    decimal? OperatingCashFlow, decimal? CapitalExpenditures, decimal? FreeCashFlow, decimal? DividendsPaid);

public record FundamentalMetricDto(DateTime PeriodEndDate, DateTime ComputedAt,
    decimal? PERatio, decimal? PBRatio, decimal? PSRatio, decimal? EVToEBITDA,
    decimal? ROE, decimal? ROA, decimal? GrossProfitMargin, decimal? OperatingMargin, decimal? NetProfitMargin,
    decimal? CurrentRatio, decimal? QuickRatio, decimal? DebtToEquity, decimal? DebtToAssets,
    decimal? InterestCoverageRatio, decimal? EPS, decimal? BookValuePerShare, decimal? DividendYield,
    decimal? DividendPayoutRatio, decimal? RevenueGrowthYoY, decimal? EarningsGrowthYoY, decimal? FCFGrowthYoY);

public record TechnicalIndicatorDto(DateTime Date,
    decimal? SMA20, decimal? SMA50, decimal? SMA200, decimal? EMA12, decimal? EMA26, decimal? RSI14,
    decimal? MACD, decimal? MACDSignal, decimal? MACDHistogram,
    decimal? BollingerUpper, decimal? BollingerMiddle, decimal? BollingerLower,
    decimal? ATR14, decimal? ADX14, decimal? StochK, decimal? StochD, decimal? OBV, decimal? VWAP);

public record SentimentDto(DateTime AnalyzedAt, string OverallSentiment, decimal SentimentScore,
    int PositiveCount, int NegativeCount, int NeutralCount, List<string>? TopHeadlines);

public record AnalysisDto(Guid StockId, string Symbol, string CompanyName, DateTime AnalyzedAt,
    string TechnicalSignal, string FundamentalSignal, string SentimentSignal, string OverallSignal,
    int TechnicalScore, int FundamentalScore, int SentimentScore, int DividendScore, int OverallScore,
    string? Reasoning, bool IsAlert, string? AlertMessage, string? ProfileName);

public record ScoringProfileDto(Guid Id, string Name, bool IsDefault, bool IsPreset,
    int TechnicalWeight, int FundamentalWeight, int SentimentWeight, int DividendWeight,
    int TechRSIWeight, int TechMACDWeight, int TechMovingAvgWeight, int TechBollingerWeight, int TechADXWeight, int TechVolumeWeight,
    int FundValuationWeight, int FundProfitabilityWeight, int FundLiquidityWeight, int FundLeverageWeight, int FundGrowthWeight,
    int AlertMinOverallScore, int AlertMinTechnicalScore, int AlertMinFundamentalScore, int AlertMinSentimentScore,
    int StrongBuyThreshold, int BuyThreshold, int HoldThreshold, int SellThreshold);

public record ScoringProfileUpdateDto(
    int TechnicalWeight, int FundamentalWeight, int SentimentWeight, int DividendWeight,
    int TechRSIWeight, int TechMACDWeight, int TechMovingAvgWeight, int TechBollingerWeight, int TechADXWeight, int TechVolumeWeight,
    int FundValuationWeight, int FundProfitabilityWeight, int FundLiquidityWeight, int FundLeverageWeight, int FundGrowthWeight,
    int AlertMinOverallScore, int AlertMinTechnicalScore, int AlertMinFundamentalScore, int AlertMinSentimentScore,
    int StrongBuyThreshold, int BuyThreshold, int HoldThreshold, int SellThreshold);

public record DashboardDto(List<StockListDto> TopGainers, List<StockListDto> TopLosers,
    List<AnalysisDto> LatestAlerts, List<SectorPerformanceDto> SectorPerformance,
    int TotalStocks, int AlertCount);

public record SectorPerformanceDto(string Sector, decimal AverageChangePercent, int StockCount);

public record ApiCallRecord(string ApiName, string Endpoint, DateTime CalledAt, int StatusCode, long LatencyMs, string? ErrorMessage);

public record ApiHealthDto(string ApiName, int TotalCalls, int SuccessCount, int ErrorCount,
    double AverageLatencyMs, DateTime? LastCalledAt, DateTime? LastErrorAt, string? LastErrorMessage,
    List<ApiCallRecord> RecentCalls);

public record AdminDashboardDto(List<ApiHealthDto> ApiHealth, DateTime ServerStartedAt,
    int TotalStocksInDb, DateTime? LastRefreshAt);

public record StockMetadataDto(
    string? Sector,
    string? Industry,
    decimal? MarketCap,
    decimal? Week52High,
    decimal? Week52Low,
    decimal? DayChange,
    decimal? DayChangePercent,
    decimal? CurrentPrice,
    long? SharesOutstanding);
