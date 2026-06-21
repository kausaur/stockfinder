using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class FundamentalAnalysisService : IFundamentalAnalysisService
{
    public FundamentalMetric CalculateMetrics(Guid stockId, List<FinancialStatement> statements,
        decimal? currentPrice, long? sharesOutstanding = null, FundamentalMetric? baseMetric = null)
    {
        var latestIncome = statements.Where(s => s.StatementType == StatementType.IncomeStatement && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        var latestBS = statements.Where(s => s.StatementType == StatementType.BalanceSheet && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        var latestCF = statements.Where(s => s.StatementType == StatementType.CashFlow && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();

        // Start with the base metric from Yahoo, or create a new one
        var metric = baseMetric ?? new FundamentalMetric();
        metric.StockId = stockId;
        metric.ComputedAt = DateTime.UtcNow;
        if (metric.PeriodEndDate == default) 
            metric.PeriodEndDate = latestIncome?.PeriodEndDate ?? DateTime.UtcNow;

        // --- Income Statement metrics (fallback) ---
        if (latestIncome != null)
        {
            var eps = latestIncome.DilutedEPS ?? latestIncome.EarningsPerShare;
            metric.EPS ??= eps;

            if (currentPrice.HasValue && eps.HasValue && eps > 0)
                metric.PERatio ??= currentPrice / eps;

            if (latestIncome.TotalRevenue.HasValue && latestIncome.TotalRevenue > 0)
            {
                if (latestIncome.GrossProfit.HasValue)
                    metric.GrossProfitMargin ??= latestIncome.GrossProfit / latestIncome.TotalRevenue * 100;
                if (latestIncome.OperatingIncome.HasValue)
                    metric.OperatingMargin ??= latestIncome.OperatingIncome / latestIncome.TotalRevenue * 100;
                if (latestIncome.NetIncome.HasValue)
                    metric.NetProfitMargin ??= latestIncome.NetIncome / latestIncome.TotalRevenue * 100;
            }

            if (currentPrice.HasValue && sharesOutstanding.HasValue && sharesOutstanding > 0
                && latestIncome.TotalRevenue.HasValue && latestIncome.TotalRevenue > 0)
            {
                var revenuePerShare = latestIncome.TotalRevenue.Value / sharesOutstanding.Value;
                if (revenuePerShare > 0)
                    metric.PSRatio ??= currentPrice / (decimal)revenuePerShare;
            }
            
            // FCF / Share using TTM Statement
            if (sharesOutstanding.HasValue && sharesOutstanding > 0 && latestCF?.FreeCashFlow.HasValue == true)
            {
                metric.FreeCashFlowPerShare ??= latestCF.FreeCashFlow / (decimal)sharesOutstanding.Value;
            }
        }

        // --- Balance Sheet metrics (fallback) ---
        if (latestBS != null)
        {
            if (latestBS.TotalEquity.HasValue && latestBS.TotalEquity > 0 && latestIncome?.NetIncome.HasValue == true)
                metric.ROE ??= (latestIncome.NetIncome / latestBS.TotalEquity) * 100;

            if (latestBS.TotalAssets.HasValue && latestBS.TotalAssets > 0 && latestIncome?.NetIncome.HasValue == true)
            {
                metric.ROA ??= (latestIncome.NetIncome / latestBS.TotalAssets) * 100;
                metric.AssetTurnover ??= latestIncome?.TotalRevenue / latestBS.TotalAssets;
            }

            if (latestBS.TotalEquity.HasValue && latestBS.TotalEquity > 0)
            {
                if (latestBS.TotalDebt.HasValue)
                    metric.DebtToEquity ??= latestBS.TotalDebt / latestBS.TotalEquity;
            }
            if (latestBS.TotalAssets.HasValue && latestBS.TotalAssets > 0 && latestBS.TotalLiabilities.HasValue)
                metric.DebtToAssets ??= latestBS.TotalLiabilities / latestBS.TotalAssets;

            if (latestIncome?.InterestExpense.HasValue == true && latestIncome.InterestExpense < 0
                && latestIncome?.OperatingIncome.HasValue == true)
                metric.InterestCoverageRatio ??= latestIncome.OperatingIncome / Math.Abs(latestIncome.InterestExpense!.Value);

            if (latestBS.CurrentLiabilities.HasValue && latestBS.CurrentLiabilities > 0)
            {
                if (latestBS.CurrentAssets.HasValue)
                    metric.CurrentRatio ??= latestBS.CurrentAssets / latestBS.CurrentLiabilities;

                var inventory = latestBS.Inventory ?? 0;
                if (latestBS.CurrentAssets.HasValue)
                    metric.QuickRatio ??= (latestBS.CurrentAssets - inventory) / latestBS.CurrentLiabilities;

                if (latestBS.CashAndEquivalents.HasValue)
                    metric.CashRatio ??= latestBS.CashAndEquivalents / latestBS.CurrentLiabilities;
            }

            if (sharesOutstanding.HasValue && sharesOutstanding > 0)
            {
                var shares = (decimal)sharesOutstanding.Value;
                if (latestBS.TotalEquity.HasValue)
                {
                    metric.BookValuePerShare ??= latestBS.TotalEquity / shares;
                    if (currentPrice.HasValue && metric.BookValuePerShare > 0)
                        metric.PBRatio ??= currentPrice / metric.BookValuePerShare;
                }
            }
        }

        return metric;
    }
}
