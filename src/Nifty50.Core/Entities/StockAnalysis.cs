using Nifty50.Core.Enums;

namespace Nifty50.Core.Entities;

public class StockAnalysis : BaseEntity
{
    public Guid StockId { get; set; }
    public Guid ScoringProfileId { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    public SignalType TechnicalSignal { get; set; }
    public SignalType FundamentalSignal { get; set; }
    public SignalType SentimentSignal { get; set; }
    public SignalType OverallSignal { get; set; }

    public int TechnicalScore { get; set; }
    public int FundamentalScore { get; set; }
    public int SentimentScore { get; set; }
    public int DividendScore { get; set; }
    public int OverallScore { get; set; }

    /// <summary>JSON snapshot of weights at analysis time</summary>
    public string? WeightsUsed { get; set; }
    public string? Reasoning { get; set; }
    public bool IsAlert { get; set; }
    public string? AlertMessage { get; set; }

    public Stock Stock { get; set; } = null!;
    public ScoringProfile ScoringProfile { get; set; } = null!;
}
