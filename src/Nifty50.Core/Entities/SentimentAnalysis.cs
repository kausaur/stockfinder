using Nifty50.Core.Enums;

namespace Nifty50.Core.Entities;

public class SentimentAnalysis : BaseEntity
{
    public Guid StockId { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public SentimentType OverallSentiment { get; set; }
    public decimal SentimentScore { get; set; }
    public int PositiveCount { get; set; }
    public int NegativeCount { get; set; }
    public int NeutralCount { get; set; }
    /// <summary>JSON array of headline strings</summary>
    public string? TopHeadlines { get; set; }

    public Stock Stock { get; set; } = null!;
}
