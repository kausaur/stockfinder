namespace Nifty50.Core.Entities;

public class IntrinsicValuation : BaseEntity
{
    public Guid StockId { get; set; }
    public DateTime ComputedAt { get; set; }
    
    // Graham Number
    public decimal? GrahamNumber { get; set; }
    public decimal? GrahamMarginOfSafety { get; set; }
    
    // PEG Ratio
    public decimal? PEGRatio { get; set; }
    
    // Earnings Power Value
    public decimal? EarningsPowerValue { get; set; }
    
    // Composite estimate
    public decimal? EstimatedFairValue { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? UpsidePercent { get; set; }
    public string? ValuationVerdict { get; set; }
    
    public Stock Stock { get; set; } = null!;
}
