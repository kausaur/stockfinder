namespace Nifty50.Core.Entities;

public class ScoringProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsPreset { get; set; }

    // Top-level weights (must sum to 100)
    public int TechnicalWeight { get; set; } = 40;
    public int FundamentalWeight { get; set; } = 25;
    public int SentimentWeight { get; set; } = 20;
    public int DividendWeight { get; set; } = 15;
    public int ValuationWeight { get; set; } = 0;
    public int QualityWeight { get; set; } = 0;

    // Technical sub-weights (sum to 100 within category)
    public int TechRSIWeight { get; set; } = 20;
    public int TechMACDWeight { get; set; } = 25;
    public int TechMovingAvgWeight { get; set; } = 25;
    public int TechBollingerWeight { get; set; } = 10;
    public int TechADXWeight { get; set; } = 10;
    public int TechVolumeWeight { get; set; } = 10;

    // Fundamental sub-weights (sum to 100 within category)
    public int FundValuationWeight { get; set; } = 25;
    public int FundProfitabilityWeight { get; set; } = 25;
    public int FundLiquidityWeight { get; set; } = 15;
    public int FundLeverageWeight { get; set; } = 15;
    public int FundGrowthWeight { get; set; } = 20;
    public int FundROCEWeight { get; set; } = 0;
    public int FundPEGWeight { get; set; } = 0;

    // Quality sub-weights (sum to 100 within category)
    public int QualPiotroskiWeight { get; set; } = 30;
    public int QualAltmanWeight { get; set; } = 15;
    public int QualPromoterWeight { get; set; } = 20;
    public int QualFIIWeight { get; set; } = 15;
    public int QualDividendConsistencyWeight { get; set; } = 10;
    public int QualFCFTrendWeight { get; set; } = 10;

    // Alert thresholds
    public int AlertMinOverallScore { get; set; } = 80;
    public int AlertMinTechnicalScore { get; set; } = 75;
    public int AlertMinFundamentalScore { get; set; } = 70;
    public int AlertMinSentimentScore { get; set; } = 60;

    // Signal thresholds
    public int StrongBuyThreshold { get; set; } = 80;
    public int BuyThreshold { get; set; } = 65;
    public int HoldThreshold { get; set; } = 45;
    public int SellThreshold { get; set; } = 30;

    public ICollection<StockAnalysis> StockAnalyses { get; set; } = new List<StockAnalysis>();
}
