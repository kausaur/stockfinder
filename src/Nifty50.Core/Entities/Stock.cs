namespace Nifty50.Core.Entities;

public class Stock : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePercent { get; set; }
    public decimal? Week52High { get; set; }
    public decimal? Week52Low { get; set; }
    public long? SharesOutstanding { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<StockPrice> Prices { get; set; } = new List<StockPrice>();
    public ICollection<Dividend> Dividends { get; set; } = new List<Dividend>();
    public ICollection<FinancialStatement> FinancialStatements { get; set; } = new List<FinancialStatement>();
    public ICollection<FundamentalMetric> FundamentalMetrics { get; set; } = new List<FundamentalMetric>();
    public ICollection<TechnicalIndicator> TechnicalIndicators { get; set; } = new List<TechnicalIndicator>();
    public ICollection<SentimentAnalysis> SentimentAnalyses { get; set; } = new List<SentimentAnalysis>();
    public ICollection<StockAnalysis> StockAnalyses { get; set; } = new List<StockAnalysis>();
    public ICollection<IntrinsicValuation> IntrinsicValuations { get; set; } = new List<IntrinsicValuation>();
    public ICollection<QualityMetric> QualityMetrics { get; set; } = new List<QualityMetric>();
    public ICollection<ScoreHistory> ScoreHistories { get; set; } = new List<ScoreHistory>();
}
