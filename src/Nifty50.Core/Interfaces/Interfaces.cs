using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;

namespace Nifty50.Core.Interfaces;

public interface IStockRepository
{
    Task<List<Stock>> GetAllAsync(string? search = null, string? sector = null);
    Task<List<(Stock Stock, StockAnalysis? Analysis)>> GetAllWithAnalysisAsync(string? search = null, string? sector = null);
    Task<Stock?> GetByIdAsync(Guid id);
    Task<Stock?> GetBySymbolAsync(string symbol);
    Task<Stock> AddAsync(Stock stock);
    Task UpdateAsync(Stock stock);
    Task SoftDeleteAsync(Guid id);
    Task<List<StockPrice>> GetPricesAsync(Guid stockId, DateTime? from, DateTime? to);
    Task<DateTime?> GetLastPriceDateAsync(Guid stockId);
    Task AddPricesAsync(IEnumerable<StockPrice> prices);
    Task<List<Dividend>> GetDividendsAsync(Guid stockId);
    Task AddDividendsAsync(IEnumerable<Dividend> dividends);
    Task<List<FinancialStatement>> GetFinancialStatementsAsync(Guid stockId, string? type = null, string? period = null);
    Task AddFinancialStatementsAsync(IEnumerable<FinancialStatement> statements);
    Task<FundamentalMetric?> GetLatestFundamentalAsync(Guid stockId);
    Task<List<FundamentalMetric>> GetFundamentalHistoryAsync(Guid stockId);
    Task AddFundamentalMetricAsync(FundamentalMetric metric);
    Task<TechnicalIndicator?> GetLatestTechnicalAsync(Guid stockId);
    Task<List<TechnicalIndicator>> GetTechnicalHistoryAsync(Guid stockId, DateTime? from, DateTime? to);
    Task AddTechnicalIndicatorsAsync(IEnumerable<TechnicalIndicator> indicators);
    Task<SentimentAnalysis?> GetLatestSentimentAsync(Guid stockId);
    Task AddSentimentAsync(SentimentAnalysis sentiment);
    Task<StockAnalysis?> GetLatestAnalysisAsync(Guid stockId);
    Task<List<StockAnalysis>> GetAlertsAsync();
    Task AddAnalysisAsync(StockAnalysis analysis);
    Task ClearAnalysesAsync(DateTime before);
    Task<DashboardDto> GetDashboardDataAsync();
    Task SaveChangesAsync();
    Task<SectorBenchmark?> GetSectorBenchmarkAsync(string sector);
    Task AddScoreHistoryAsync(ScoreHistory history);
    Task<IntrinsicValuation?> GetLatestValuationAsync(Guid stockId);
    Task<QualityMetric?> GetLatestQualityAsync(Guid stockId);
    Task UpsertQualityMetricAsync(QualityMetric metric);
    Task<List<ScoreHistory>> GetScoreHistoryAsync(Guid stockId, int limit = 100);
    Task AddIntrinsicValuationAsync(IntrinsicValuation valuation);
    Task<List<StockDataDto>> GetStocksWithDataAsync(string? sector = null);
}

public interface IStockDataService
{
    Task<List<StockPrice>> FetchHistoricalPricesAsync(string symbol, DateTime from, DateTime to);
    Task<List<Dividend>> FetchDividendsAsync(string symbol, DateTime from, DateTime to);
}

public interface IFundamentalDataService
{
    Task<(List<FinancialStatement> Statements, FundamentalMetric? Metric)> FetchFundamentalsAsync(string symbol);
}

public interface ISentimentService
{
    Task<SentimentAnalysis> AnalyzeSentimentAsync(string companyName, string symbol);
}

public interface ITechnicalAnalysisService
{
    List<TechnicalIndicator> CalculateIndicators(Guid stockId, List<StockPrice> prices);
}

public interface IFundamentalAnalysisService
{
    FundamentalMetric CalculateMetrics(Guid stockId, List<FinancialStatement> statements,
        decimal? currentPrice, long? sharesOutstanding = null, FundamentalMetric? baseMetric = null);
}

public interface IScoringProfileService
{
    Task<List<ScoringProfile>> GetAllProfilesAsync();
    Task<ScoringProfile> GetActiveProfileAsync();
    Task<ScoringProfile?> GetByIdAsync(Guid id);
    Task<ScoringProfile> CreateProfileAsync(ScoringProfileDto dto);
    Task UpdateActiveProfileAsync(ScoringProfileUpdateDto dto);
    Task ActivateProfileAsync(Guid id);
    Task ResetToDefaultAsync();
}

public interface IStockAnalysisEngine
{
    Task<StockAnalysis> AnalyzeStockAsync(Guid stockId, ScoringProfile? profile = null);
    Task<List<StockAnalysis>> RecalculateAllAsync();
}

public interface IApiMonitorService
{
    void RecordApiCall(ApiCallRecord record);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    List<ApiCallRecord> GetRecentCalls(string? apiName, int limit = 50);
}

public interface IDataRefreshService
{
    Task RefreshAllDataAsync(CancellationToken ct = default);
    void RequestManualRefresh();
}

public interface IStockMetadataService
{
    Task<StockMetadataDto?> FetchMetadataAsync(string symbol);
}

public interface ISectorRelativeService
{
    Task<SectorBenchmark?> GetBenchmarkAsync(string? sector);
    bool IsBFSI(string? sector);
    double ScoreRelativePE(decimal? stockPE, SectorBenchmark? benchmark);
    double ScoreRelativeROE(decimal? stockROE, SectorBenchmark? benchmark);
}

public interface IIntrinsicValueService
{
    IntrinsicValuation CalculateIntrinsicValue(Guid stockId, decimal currentPrice, FundamentalMetric metric);
}

public interface IQualityAssessmentService
{
    QualityMetric AssessQuality(Guid stockId, List<FinancialStatement> statements, FundamentalMetric metric, string? sector, decimal? marketCap, QualityMetric? existingCuratedData);
}

public interface IRecommendationService
{
    Task<RecommendationDashboardDto> GetDashboardAsync();
    Task<List<StockRecommendationDto>> ScreenStocksAsync(ScreenerFilters filters);
    Task<List<PeerComparisonDto>> GetPeersAsync(Guid stockId);
}

public interface IIndianMarketDataService
{
    Task<StockMetadataDto?> FetchMetadataAsync(string symbol);
    Task<(List<FinancialStatement> Statements, FundamentalMetric? Metric)> FetchFundamentalsAsync(string symbol);
    Task<ShareholdingDto?> FetchShareholdingAsync(string symbol);
    Task<List<StockPrice>> FetchHistoricalPricesAsync(string symbol, DateTime from, DateTime to);
}
