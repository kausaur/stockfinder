using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class FundamentalAnalysisService : IFundamentalAnalysisService
{
    /// <summary>
    /// Calculates all fundamental metrics from fetched financial statements.
    /// Uses real shares outstanding data (fetched from Yahoo Finance defaultKeyStatistics)
    /// for accurate per-share calculations (BookValuePerShare, FreeCashFlowPerShare, PBRatio).
    /// No values are hardcoded or estimated — null is returned when data is unavailable.
    /// </summary>
    public FundamentalMetric CalculateMetrics(Guid stockId, List<FinancialStatement> statements,
        decimal? currentPrice, long? sharesOutstanding = null)
    {
        var latestIncome = statements.Where(s => s.StatementType == StatementType.IncomeStatement && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        var prevIncome = statements.Where(s => s.StatementType == StatementType.IncomeStatement && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).Skip(1).FirstOrDefault();
        var prevPrevIncome = statements.Where(s => s.StatementType == StatementType.IncomeStatement && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).Skip(2).FirstOrDefault();
        var latestBS = statements.Where(s => s.StatementType == StatementType.BalanceSheet && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        var latestCF = statements.Where(s => s.StatementType == StatementType.CashFlow && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        var prevCF = statements.Where(s => s.StatementType == StatementType.CashFlow && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).Skip(1).FirstOrDefault();

        var metric = new FundamentalMetric
        {
            StockId = stockId,
            ComputedAt = DateTime.UtcNow,
            PeriodEndDate = latestIncome?.PeriodEndDate ?? DateTime.UtcNow,
        };

        // --- Income Statement metrics ---
        if (latestIncome != null)
        {
            // EPS: prefer diluted EPS, fall back to basic EPS
            var eps = latestIncome.DilutedEPS ?? latestIncome.EarningsPerShare;
            metric.EPS = eps;

            // P/E ratio
            if (currentPrice.HasValue && eps.HasValue && eps > 0)
                metric.PERatio = currentPrice / eps;

            // Margin ratios
            if (latestIncome.TotalRevenue.HasValue && latestIncome.TotalRevenue > 0)
            {
                if (latestIncome.GrossProfit.HasValue)
                    metric.GrossProfitMargin = latestIncome.GrossProfit / latestIncome.TotalRevenue * 100;
                if (latestIncome.OperatingIncome.HasValue)
                    metric.OperatingMargin = latestIncome.OperatingIncome / latestIncome.TotalRevenue * 100;
                if (latestIncome.NetIncome.HasValue)
                    metric.NetProfitMargin = latestIncome.NetIncome / latestIncome.TotalRevenue * 100;
            }

            // P/S ratio
            if (currentPrice.HasValue && sharesOutstanding.HasValue && sharesOutstanding > 0
                && latestIncome.TotalRevenue.HasValue && latestIncome.TotalRevenue > 0)
            {
                var revenuePerShare = latestIncome.TotalRevenue.Value / sharesOutstanding.Value;
                if (revenuePerShare > 0)
                    metric.PSRatio = currentPrice / (decimal)revenuePerShare;
            }
        }

        // --- Balance Sheet metrics ---
        if (latestBS != null)
        {
            // ROE = NetIncome / TotalEquity
            if (latestBS.TotalEquity.HasValue && latestBS.TotalEquity > 0 && latestIncome?.NetIncome.HasValue == true)
                metric.ROE = (latestIncome.NetIncome / latestBS.TotalEquity) * 100;

            // ROA = NetIncome / TotalAssets
            if (latestBS.TotalAssets.HasValue && latestBS.TotalAssets > 0 && latestIncome?.NetIncome.HasValue == true)
            {
                metric.ROA = (latestIncome.NetIncome / latestBS.TotalAssets) * 100;
                metric.AssetTurnover = latestIncome?.TotalRevenue / latestBS.TotalAssets;
            }

            // Leverage
            if (latestBS.TotalEquity.HasValue && latestBS.TotalEquity > 0)
            {
                if (latestBS.TotalDebt.HasValue)
                    metric.DebtToEquity = latestBS.TotalDebt / latestBS.TotalEquity;
            }
            if (latestBS.TotalAssets.HasValue && latestBS.TotalAssets > 0 && latestBS.TotalLiabilities.HasValue)
                metric.DebtToAssets = latestBS.TotalLiabilities / latestBS.TotalAssets;

            // Interest coverage
            if (latestIncome?.InterestExpense.HasValue == true && latestIncome.InterestExpense < 0
                && latestIncome?.OperatingIncome.HasValue == true)
                metric.InterestCoverageRatio = latestIncome.OperatingIncome / Math.Abs(latestIncome.InterestExpense!.Value);

            // Liquidity
            if (latestBS.CurrentLiabilities.HasValue && latestBS.CurrentLiabilities > 0)
            {
                if (latestBS.CurrentAssets.HasValue)
                    metric.CurrentRatio = latestBS.CurrentAssets / latestBS.CurrentLiabilities;

                var inventory = latestBS.Inventory ?? 0;
                if (latestBS.CurrentAssets.HasValue)
                    metric.QuickRatio = (latestBS.CurrentAssets - inventory) / latestBS.CurrentLiabilities;

                if (latestBS.CashAndEquivalents.HasValue)
                    metric.CashRatio = latestBS.CashAndEquivalents / latestBS.CurrentLiabilities;
            }

            // Per-share metrics (only when shares outstanding is available from Yahoo API)
            if (sharesOutstanding.HasValue && sharesOutstanding > 0)
            {
                var shares = (decimal)sharesOutstanding.Value;

                if (latestBS.TotalEquity.HasValue)
                {
                    metric.BookValuePerShare = latestBS.TotalEquity / shares;

                    // P/B ratio
                    if (currentPrice.HasValue && metric.BookValuePerShare > 0)
                        metric.PBRatio = currentPrice / metric.BookValuePerShare;
                }
            }
        }

        // --- Cash Flow metrics ---
        if (latestCF != null)
        {
            if (sharesOutstanding.HasValue && sharesOutstanding > 0 && latestCF.FreeCashFlow.HasValue)
                metric.FreeCashFlowPerShare = latestCF.FreeCashFlow / (decimal)sharesOutstanding.Value;

            // Dividend yield (from dividends paid and market cap)
            if (latestCF.DividendsPaid.HasValue && currentPrice.HasValue
                && sharesOutstanding.HasValue && sharesOutstanding > 0 && currentPrice > 0)
            {
                var marketCap = currentPrice.Value * sharesOutstanding.Value;
                metric.DividendYield = Math.Abs(latestCF.DividendsPaid.Value) / (decimal)marketCap * 100;
                if (latestIncome?.NetIncome.HasValue == true && latestIncome.NetIncome > 0)
                    metric.DividendPayoutRatio = Math.Abs(latestCF.DividendsPaid.Value) / latestIncome.NetIncome * 100;
            }

            // FCF Growth YoY
            if (prevCF?.FreeCashFlow.HasValue == true && prevCF.FreeCashFlow != 0 && latestCF.FreeCashFlow.HasValue)
                metric.FCFGrowthYoY = ((latestCF.FreeCashFlow ?? 0) - (prevCF.FreeCashFlow ?? 0)) / Math.Abs(prevCF.FreeCashFlow!.Value) * 100;
        }

        // --- Growth metrics ---
        if (latestIncome != null && prevIncome != null)
        {
            if (prevIncome.TotalRevenue.HasValue && prevIncome.TotalRevenue > 0 && latestIncome.TotalRevenue.HasValue)
                metric.RevenueGrowthYoY = ((latestIncome.TotalRevenue ?? 0) - (prevIncome.TotalRevenue ?? 0)) / prevIncome.TotalRevenue * 100;

            if (prevIncome.NetIncome.HasValue && prevIncome.NetIncome > 0 && latestIncome.NetIncome.HasValue)
                metric.EarningsGrowthYoY = ((latestIncome.NetIncome ?? 0) - (prevIncome.NetIncome ?? 0)) / prevIncome.NetIncome * 100;
        }

        return metric;
    }
}
