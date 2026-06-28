namespace Nifty50.Core.Entities;

public class ScoreHistory : BaseEntity
{
    public Guid StockId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string ScoringProfileName { get; set; } = string.Empty;
    
    public int OverallScore { get; set; }
    public int FundamentalScore { get; set; }
    public int TechnicalScore { get; set; }
    public int SentimentScore { get; set; }
    public int DividendScore { get; set; }
    public int? ValuationScore { get; set; }
    public int? QualityScore { get; set; }
    
    public string Signal { get; set; } = string.Empty;
    
    public Stock Stock { get; set; } = null!;
}
