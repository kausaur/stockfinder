using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class IntrinsicValueService : IIntrinsicValueService
{
    // Tax rate for India corporate
    private const decimal TaxRate = 0.25m;
    // Discount rate for Indian equities
    private const decimal DiscountRate = 0.12m;

    public IntrinsicValuation CalculateIntrinsicValue(Guid stockId, decimal currentPrice, FundamentalMetric metric)
    {
        var valuation = new IntrinsicValuation
        {
            StockId = stockId,
            ComputedAt = DateTime.UtcNow,
            CurrentPrice = currentPrice
        };

        // 1. Graham Number: sqrt(22.5 * EPS * BVPS)
        if (metric.EPS.HasValue && metric.EPS.Value > 0 && 
            metric.BookValuePerShare.HasValue && metric.BookValuePerShare.Value > 0)
        {
            var value = (double)(22.5m * metric.EPS.Value * metric.BookValuePerShare.Value);
            if (value > 0)
            {
                valuation.GrahamNumber = (decimal)Math.Sqrt(value);
                
                if (valuation.GrahamNumber.Value > 0)
                {
                    valuation.GrahamMarginOfSafety = ((valuation.GrahamNumber.Value - currentPrice) / valuation.GrahamNumber.Value) * 100m;
                }
            }
        }

        // 2. PEG Ratio: PE / EPS Growth %
        if (metric.PERatio.HasValue && metric.PERatio.Value > 0 && 
            metric.EarningsGrowthYoY.HasValue && metric.EarningsGrowthYoY.Value > 0)
        {
            valuation.PEGRatio = metric.PERatio.Value / metric.EarningsGrowthYoY.Value;
        }

        // 3. Earnings Power Value: (OperatingIncome * (1 - TaxRate)) / DiscountRate / Shares
        // Note: For EPV we need per share operating income.
        // Approx: OperatingIncome per share = EPS / NetProfitMargin * OperatingMargin
        if (metric.EPS.HasValue && metric.EPS.Value > 0 &&
            metric.NetProfitMargin.HasValue && metric.NetProfitMargin.Value > 0 &&
            metric.OperatingMargin.HasValue)
        {
            var ebitPerShare = metric.EPS.Value * (metric.OperatingMargin.Value / metric.NetProfitMargin.Value);
            if (ebitPerShare > 0)
            {
                var nopatPerShare = ebitPerShare * (1 - TaxRate);
                valuation.EarningsPowerValue = nopatPerShare / DiscountRate;
            }
        }

        // 4. Composite Fair Value Estimate
        var estimates = new List<decimal>();
        if (valuation.GrahamNumber.HasValue) estimates.Add(valuation.GrahamNumber.Value);
        if (valuation.EarningsPowerValue.HasValue) estimates.Add(valuation.EarningsPowerValue.Value);

        if (estimates.Any())
        {
            valuation.EstimatedFairValue = estimates.Average();
            
            if (valuation.EstimatedFairValue.Value > 0 && currentPrice > 0)
            {
                valuation.UpsidePercent = ((valuation.EstimatedFairValue.Value - currentPrice) / currentPrice) * 100m;
                
                // Determine Verdict
                if (valuation.UpsidePercent > 25m) valuation.ValuationVerdict = "Significantly Undervalued";
                else if (valuation.UpsidePercent > 10m) valuation.ValuationVerdict = "Moderately Undervalued";
                else if (valuation.UpsidePercent > -10m) valuation.ValuationVerdict = "Fairly Valued";
                else if (valuation.UpsidePercent > -25m) valuation.ValuationVerdict = "Moderately Overvalued";
                else valuation.ValuationVerdict = "Significantly Overvalued";
            }
            else
            {
                valuation.ValuationVerdict = "Unknown (Insufficient Data)";
            }
        }
        else
        {
            valuation.ValuationVerdict = "Unknown (Insufficient Data)";
        }

        return valuation;
    }
}
