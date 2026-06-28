namespace Nifty50.Core.Entities;

public class SectorBenchmark : BaseEntity
{
    public string Sector { get; set; } = string.Empty;
    public DateTime AsOfDate { get; set; }
    
    // Valuation medians
    public decimal? MedianPE { get; set; }
    public decimal? MedianPB { get; set; }
    public decimal? MedianEVToEBITDA { get; set; }
    
    // Profitability medians
    public decimal? MedianROE { get; set; }
    public decimal? MedianROCE { get; set; }
    public decimal? MedianOperatingMargin { get; set; }
    
    // Growth medians
    public decimal? MedianEPSGrowth { get; set; }
    public decimal? MedianRevenueGrowth { get; set; }
    
    // Leverage norms
    public decimal? TypicalDebtToEquity { get; set; }
    
    // Sector classification flags
    public bool IsBFSI { get; set; }
}
