namespace Nifty50.Core.Entities;

public class FundamentalMetric : BaseEntity
{
    public Guid StockId { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodEndDate { get; set; }

    // Valuation
    public decimal? PERatio { get; set; }
    public decimal? PBRatio { get; set; }
    public decimal? PSRatio { get; set; }
    public decimal? EVToEBITDA { get; set; }
    public decimal? EVToFCF { get; set; }
    public decimal? PEGRatio { get; set; }

    // Profitability
    public decimal? ROE { get; set; }
    public decimal? ROA { get; set; }
    public decimal? ROIC { get; set; }
    public decimal? GrossProfitMargin { get; set; }
    public decimal? OperatingMargin { get; set; }
    public decimal? NetProfitMargin { get; set; }

    // Liquidity
    public decimal? CurrentRatio { get; set; }
    public decimal? QuickRatio { get; set; }
    public decimal? CashRatio { get; set; }

    // Leverage
    public decimal? DebtToEquity { get; set; }
    public decimal? DebtToAssets { get; set; }
    public decimal? InterestCoverageRatio { get; set; }

    // Efficiency
    public decimal? AssetTurnover { get; set; }
    public decimal? InventoryTurnover { get; set; }
    public decimal? ReceivablesTurnover { get; set; }

    // Per-Share
    public decimal? EPS { get; set; }
    public decimal? BookValuePerShare { get; set; }
    public decimal? FreeCashFlowPerShare { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? DividendPayoutRatio { get; set; }

    // Growth
    public decimal? RevenueGrowthYoY { get; set; }
    public decimal? EarningsGrowthYoY { get; set; }
    public decimal? FCFGrowthYoY { get; set; }

    public Stock Stock { get; set; } = null!;
}
