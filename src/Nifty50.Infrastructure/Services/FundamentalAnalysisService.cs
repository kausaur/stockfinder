using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class FundamentalAnalysisService : IFundamentalAnalysisService
{
    public FundamentalMetric CalculateMetrics(Guid stockId, List<FinancialStatement> statements, decimal? currentPrice)
    {
        var latestIncome = statements.Where(s => s.StatementType == StatementType.IncomeStatement && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        var prevIncome = statements.Where(s => s.StatementType == StatementType.IncomeStatement && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).Skip(1).FirstOrDefault();
        var latestBS = statements.Where(s => s.StatementType == StatementType.BalanceSheet && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();
        var latestCF = statements.Where(s => s.StatementType == StatementType.CashFlow && s.Period == PeriodType.Annual)
            .OrderByDescending(s => s.PeriodEndDate).FirstOrDefault();

        var metric = new FundamentalMetric
        {
            StockId = stockId,
            ComputedAt = DateTime.UtcNow,
            PeriodEndDate = latestIncome?.PeriodEndDate ?? DateTime.UtcNow,
        };

        if (latestIncome != null)
        {
            var eps = latestIncome.EarningsPerShare ?? latestIncome.DilutedEPS;
            metric.EPS = eps;
            if (currentPrice.HasValue && eps.HasValue && eps > 0)
                metric.PERatio = currentPrice / eps;

            if (latestIncome.TotalRevenue > 0)
            {
                metric.GrossProfitMargin = latestIncome.GrossProfit / latestIncome.TotalRevenue * 100;
                metric.OperatingMargin = latestIncome.OperatingIncome / latestIncome.TotalRevenue * 100;
                metric.NetProfitMargin = latestIncome.NetIncome / latestIncome.TotalRevenue * 100;
            }
        }

        if (latestBS != null)
        {
            if (latestBS.TotalEquity > 0)
            {
                metric.ROE = (latestIncome?.NetIncome / latestBS.TotalEquity) * 100;
                metric.DebtToEquity = latestBS.TotalDebt / latestBS.TotalEquity;
                metric.BookValuePerShare = latestBS.TotalEquity / 1; // Simplified
                if (currentPrice.HasValue)
                    metric.PBRatio = currentPrice / (latestBS.TotalEquity / 1);
            }
            if (latestBS.TotalAssets > 0)
            {
                metric.ROA = (latestIncome?.NetIncome / latestBS.TotalAssets) * 100;
                metric.DebtToAssets = latestBS.TotalLiabilities / latestBS.TotalAssets;
                metric.AssetTurnover = latestIncome?.TotalRevenue / latestBS.TotalAssets;
            }
            if (latestBS.CurrentLiabilities > 0)
            {
                metric.CurrentRatio = latestBS.CurrentAssets / latestBS.CurrentLiabilities;
                metric.QuickRatio = (latestBS.CurrentAssets - (latestBS.Inventory ?? 0)) / latestBS.CurrentLiabilities;
                metric.CashRatio = latestBS.CashAndEquivalents / latestBS.CurrentLiabilities;
            }
            if (latestIncome?.InterestExpense > 0)
                metric.InterestCoverageRatio = latestIncome?.OperatingIncome / latestIncome?.InterestExpense;
        }

        if (latestCF != null)
            metric.FreeCashFlowPerShare = latestCF.FreeCashFlow / 1; // Simplified

        // Growth YoY
        if (latestIncome != null && prevIncome != null)
        {
            if (prevIncome.TotalRevenue > 0)
                metric.RevenueGrowthYoY = ((latestIncome.TotalRevenue ?? 0) - (prevIncome.TotalRevenue ?? 0)) / prevIncome.TotalRevenue * 100;
            if (prevIncome.NetIncome > 0)
                metric.EarningsGrowthYoY = ((latestIncome.NetIncome ?? 0) - (prevIncome.NetIncome ?? 0)) / prevIncome.NetIncome * 100;
        }

        return metric;
    }
}
