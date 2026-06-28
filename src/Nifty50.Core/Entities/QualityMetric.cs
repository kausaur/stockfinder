namespace Nifty50.Core.Entities;

public class QualityMetric : BaseEntity
{
    public Guid StockId { get; set; }
    public DateTime AsOfDate { get; set; }
    
    // Piotroski F-Score
    public int? PiotroskiFScore { get; set; }
    public string? PiotroskiBreakdown { get; set; }
    
    // Altman Z-Score
    public decimal? AltmanZScore { get; set; }
    public string? AltmanZone { get; set; }
    
    // Holdings
    public decimal? PromoterHolding { get; set; }
    public decimal? PromoterPledgePercent { get; set; }
    public string? PromoterHoldingTrend { get; set; }
    public decimal? FIIHolding { get; set; }
    public string? FIIHoldingTrend { get; set; }
    public decimal? DIIHolding { get; set; }
    
    // Consistency
    public int? ConsecutiveProfitYears { get; set; }
    public int? ConsecutiveDividendYears { get; set; }
    public decimal? ROCELatest { get; set; }
    
    // FCF quality
    public string? FCFTrend { get; set; }
    
    public Stock Stock { get; set; } = null!;
}
