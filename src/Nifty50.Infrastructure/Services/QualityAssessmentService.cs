using System.Text.Json;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class QualityAssessmentService : IQualityAssessmentService
{
    private readonly ISectorRelativeService _sectorService;

    public QualityAssessmentService(ISectorRelativeService sectorService)
    {
        _sectorService = sectorService;
    }

    public QualityMetric AssessQuality(Guid stockId, List<FinancialStatement> statements, FundamentalMetric metric, string? sector, decimal? marketCap, QualityMetric? existingCuratedData)
    {
        var quality = new QualityMetric
        {
            StockId = stockId,
            AsOfDate = DateTime.UtcNow,
            // Carry over curated data if provided
            PromoterHolding = existingCuratedData?.PromoterHolding,
            PromoterPledgePercent = existingCuratedData?.PromoterPledgePercent,
            PromoterHoldingTrend = existingCuratedData?.PromoterHoldingTrend,
            FIIHolding = existingCuratedData?.FIIHolding,
            FIIHoldingTrend = existingCuratedData?.FIIHoldingTrend,
            DIIHolding = existingCuratedData?.DIIHolding,
            ConsecutiveProfitYears = existingCuratedData?.ConsecutiveProfitYears,
            ConsecutiveDividendYears = existingCuratedData?.ConsecutiveDividendYears
        };

        quality.ROICLatest = metric.ROIC;
        
        // FCF Trend
        if (metric.FCFGrowthYoY.HasValue)
        {
            if (metric.FCFGrowthYoY.Value > 5) quality.FCFTrend = "Positive_Growing";
            else if (metric.FCFGrowthYoY.Value >= -5) quality.FCFTrend = "Positive_Flat";
            else quality.FCFTrend = "Negative";
        }

        // Altman Z-Score (Skip for BFSI)
        if (!_sectorService.IsBFSI(sector))
        {
            CalculateAltmanZScore(quality, statements, marketCap);
        }

        // Piotroski F-Score
        CalculatePiotroskiFScore(quality, statements);

        return quality;
    }

    private void CalculateAltmanZScore(QualityMetric quality, List<FinancialStatement> statements, decimal? marketCap)
    {
        var latest = statements.OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        if (latest == null || !marketCap.HasValue || latest.TotalAssets == null || latest.TotalAssets <= 0) return;

        var ta = latest.TotalAssets.Value;
        var wc = (latest.CurrentAssets ?? 0) - (latest.CurrentLiabilities ?? 0);
        var re = latest.TotalEquity ?? 0; // Approx Retained Earnings with Total Equity
        var ebit = latest.OperatingIncome ?? 0;
        var tl = latest.TotalLiabilities ?? 0;
        var sales = latest.TotalRevenue ?? 0;

        if (tl <= 0) return;

        var z1 = (wc / ta) * 1.2m;
        var z2 = (re / ta) * 1.4m;
        var z3 = (ebit / ta) * 3.3m;
        var z4 = (marketCap.Value / tl) * 0.6m;
        var z5 = (sales / ta) * 1.0m;

        quality.AltmanZScore = z1 + z2 + z3 + z4 + z5;

        if (quality.AltmanZScore > 2.99m) quality.AltmanZone = "Safe";
        else if (quality.AltmanZScore > 1.8m) quality.AltmanZone = "Grey";
        else quality.AltmanZone = "Distress";
    }

    private void CalculatePiotroskiFScore(QualityMetric quality, List<FinancialStatement> statements)
    {
        var sorted = statements.OrderByDescending(s => s.PeriodEndDate).ToList();
        if (sorted.Count == 0) return;

        var current = sorted[0];
        var previous = sorted.Count > 1 ? sorted[1] : null;

        int score = 0;
        var breakdown = new Dictionary<string, bool>();

        // 1. Net Income > 0
        bool niPositive = (current.NetIncome ?? 0) > 0;
        if (niPositive) score++;
        breakdown["NetIncomePositive"] = niPositive;

        // 2. Operating Cash Flow > 0
        bool ocfPositive = (current.OperatingCashFlow ?? 0) > 0;
        if (ocfPositive) score++;
        breakdown["OperatingCashFlowPositive"] = ocfPositive;

        // 4. OCF > Net Income
        bool ocfGreaterThanNi = (current.OperatingCashFlow ?? 0) > (current.NetIncome ?? 0);
        if (ocfGreaterThanNi) score++;
        breakdown["OcfGreaterThanNi"] = ocfGreaterThanNi;

        if (previous != null)
        {
            // 3. ROA Improved
            var currentRoa = current.TotalAssets > 0 ? (current.NetIncome ?? 0) / current.TotalAssets.Value : 0;
            var prevRoa = previous.TotalAssets > 0 ? (previous.NetIncome ?? 0) / previous.TotalAssets.Value : 0;
            bool roaImproved = currentRoa > prevRoa;
            if (roaImproved) score++;
            breakdown["RoaImproved"] = roaImproved;

            // 5. Long-term Debt / Assets Decreased
            var currentLev = current.TotalAssets > 0 ? (current.TotalDebt ?? 0) / current.TotalAssets.Value : 0;
            var prevLev = previous.TotalAssets > 0 ? (previous.TotalDebt ?? 0) / previous.TotalAssets.Value : 0;
            bool leverageDecreased = currentLev < prevLev;
            if (leverageDecreased) score++;
            breakdown["LeverageDecreased"] = leverageDecreased;

            // 6. Current Ratio Improved
            var currentCr = current.CurrentLiabilities > 0 ? (current.CurrentAssets ?? 0) / current.CurrentLiabilities.Value : 0;
            var prevCr = previous.CurrentLiabilities > 0 ? (previous.CurrentAssets ?? 0) / previous.CurrentLiabilities.Value : 0;
            bool crImproved = currentCr > prevCr;
            if (crImproved) score++;
            breakdown["CurrentRatioImproved"] = crImproved;

            // 7. No Share Dilution
            // Skipped because we do not currently track SharesOutstanding history. F-Score is out of 8.

            // 8. Gross Margin Improved
            var currentGm = current.TotalRevenue > 0 ? (current.GrossProfit ?? 0) / current.TotalRevenue.Value : 0;
            var prevGm = previous.TotalRevenue > 0 ? (previous.GrossProfit ?? 0) / previous.TotalRevenue.Value : 0;
            bool gmImproved = currentGm > prevGm;
            if (gmImproved) score++;
            breakdown["GrossMarginImproved"] = gmImproved;

            // 9. Asset Turnover Improved
            var currentAto = current.TotalAssets > 0 ? (current.TotalRevenue ?? 0) / current.TotalAssets.Value : 0;
            var prevAto = previous.TotalAssets > 0 ? (previous.TotalRevenue ?? 0) / previous.TotalAssets.Value : 0;
            bool atoImproved = currentAto > prevAto;
            if (atoImproved) score++;
            breakdown["AssetTurnoverImproved"] = atoImproved;
        }

        quality.PiotroskiFScore = score;
        quality.PiotroskiBreakdown = JsonSerializer.Serialize(breakdown);
    }
}
