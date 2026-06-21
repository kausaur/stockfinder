using System.Text.Json;
using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class StockAnalysisEngine : IStockAnalysisEngine
{
    private readonly IStockRepository _repo;
    private readonly IScoringProfileService _profileService;

    public StockAnalysisEngine(IStockRepository repo, IScoringProfileService profileService)
    {
        _repo = repo;
        _profileService = profileService;
    }

    public async Task<StockAnalysis> AnalyzeStockAsync(Guid stockId)
    {
        var profile = await _profileService.GetActiveProfileAsync();
        var tech = await _repo.GetLatestTechnicalAsync(stockId);
        var fund = await _repo.GetLatestFundamentalAsync(stockId);
        var sent = await _repo.GetLatestSentimentAsync(stockId);
        var stock = await _repo.GetByIdAsync(stockId);

        int techScore = CalculateTechnicalScore(tech, profile);
        int fundScore = CalculateFundamentalScore(fund, profile);
        int sentScore = sent != null ? (int)((sent.SentimentScore + 1m) / 2m * 100m) : 50;
        int divScore = CalculateDividendScore(fund);

        double overall = (profile.TechnicalWeight / 100.0 * techScore)
                       + (profile.FundamentalWeight / 100.0 * fundScore)
                       + (profile.SentimentWeight / 100.0 * sentScore)
                       + (profile.DividendWeight / 100.0 * divScore);
        int overallScore = Math.Clamp((int)overall, 0, 100);

        var overallSignal = GetSignal(overallScore, profile);
        bool isAlert = overallScore >= profile.AlertMinOverallScore
                    && techScore >= profile.AlertMinTechnicalScore
                    && fundScore >= profile.AlertMinFundamentalScore
                    && sentScore >= profile.AlertMinSentimentScore;

        var reasoning = GenerateReasoning(stock?.CompanyName ?? "", techScore, fundScore, sentScore, divScore, overallSignal, tech, fund);

        var analysis = new StockAnalysis
        {
            StockId = stockId,
            ScoringProfileId = profile.Id,
            AnalyzedAt = DateTime.UtcNow,
            TechnicalSignal = GetSignal(techScore, profile),
            FundamentalSignal = GetSignal(fundScore, profile),
            SentimentSignal = GetSignal(sentScore, profile),
            OverallSignal = overallSignal,
            TechnicalScore = techScore,
            FundamentalScore = fundScore,
            SentimentScore = sentScore,
            DividendScore = divScore,
            OverallScore = overallScore,
            WeightsUsed = JsonSerializer.Serialize(new { profile.TechnicalWeight, profile.FundamentalWeight, profile.SentimentWeight, profile.DividendWeight }),
            Reasoning = reasoning,
            IsAlert = isAlert,
            AlertMessage = isAlert ? $"🚨 Strong buy signal for {stock?.CompanyName} (Score: {overallScore}/100)" : null,
        };

        await _repo.AddAnalysisAsync(analysis);
        return analysis;
    }

    public async Task RecalculateAllAsync()
    {
        await _repo.ClearAnalysesAsync();
        var stocks = await _repo.GetAllAsync();
        foreach (var stock in stocks)
        {
            try { await AnalyzeStockAsync(stock.Id); } catch { /* Skip failed analyses */ }
        }
    }

    private static int CalculateTechnicalScore(TechnicalIndicator? t, ScoringProfile p)
    {
        if (t == null) return 50;
        double score = 0;

        // RSI component
        if (t.RSI14.HasValue)
        {
            var rsi = (double)t.RSI14.Value;
            var rsiScore = rsi < 30 ? 90 : rsi < 40 ? 70 : rsi > 70 ? 20 : rsi > 60 ? 40 : 55;
            score += p.TechRSIWeight / 100.0 * rsiScore;
        }

        // MACD component
        if (t.MACDHistogram.HasValue)
        {
            var macdScore = t.MACDHistogram > 0 ? 70 : 30;
            if (t.MACD.HasValue && t.MACDSignal.HasValue && t.MACD > t.MACDSignal) macdScore += 15;
            score += p.TechMACDWeight / 100.0 * Math.Min(macdScore, 100);
        }

        // Moving Average component
        if (t.SMA50.HasValue && t.SMA200.HasValue)
        {
            var maScore = t.SMA50 > t.SMA200 ? 75 : 30; // Golden cross vs death cross
            score += p.TechMovingAvgWeight / 100.0 * maScore;
        }

        // Bollinger component
        if (t.BollingerLower.HasValue && t.BollingerUpper.HasValue && t.BollingerMiddle.HasValue)
        {
            var bbScore = 50;
            score += p.TechBollingerWeight / 100.0 * bbScore;
        }

        // ADX component
        if (t.ADX14.HasValue)
        {
            var adxScore = t.ADX14 > 25 ? 70 : 40;
            score += p.TechADXWeight / 100.0 * adxScore;
        }

        // Volume component
        if (t.OBV.HasValue) score += p.TechVolumeWeight / 100.0 * 55;

        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateFundamentalScore(FundamentalMetric? f, ScoringProfile p)
    {
        if (f == null) return 50;
        double score = 0;

        // Valuation
        double valScore = 50;
        if (f.PERatio.HasValue)
            valScore = f.PERatio < 15 ? 85 : f.PERatio < 25 ? 65 : f.PERatio < 35 ? 40 : 20;
        score += p.FundValuationWeight / 100.0 * valScore;

        // Profitability
        double profScore = 50;
        if (f.ROE.HasValue)
            profScore = f.ROE > 20 ? 85 : f.ROE > 15 ? 70 : f.ROE > 10 ? 55 : 35;
        score += p.FundProfitabilityWeight / 100.0 * profScore;

        // Liquidity
        double liqScore = 50;
        if (f.CurrentRatio.HasValue)
            liqScore = f.CurrentRatio > 2 ? 80 : f.CurrentRatio > 1.5m ? 65 : f.CurrentRatio > 1 ? 45 : 25;
        score += p.FundLiquidityWeight / 100.0 * liqScore;

        // Leverage
        double levScore = 50;
        if (f.DebtToEquity.HasValue)
            levScore = f.DebtToEquity < 0.3m ? 85 : f.DebtToEquity < 0.5m ? 70 : f.DebtToEquity < 1 ? 50 : 25;
        score += p.FundLeverageWeight / 100.0 * levScore;

        // Growth
        double growScore = 50;
        if (f.EarningsGrowthYoY.HasValue)
            growScore = f.EarningsGrowthYoY > 20 ? 85 : f.EarningsGrowthYoY > 10 ? 70 : f.EarningsGrowthYoY > 0 ? 55 : 30;
        score += p.FundGrowthWeight / 100.0 * growScore;

        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateDividendScore(FundamentalMetric? f)
    {
        if (f == null) return 50;
        double score = 50;
        if (f.DividendYield.HasValue)
            score = f.DividendYield > 4 ? 85 : f.DividendYield > 2 ? 70 : f.DividendYield > 1 ? 55 : 40;
        return Math.Clamp((int)score, 0, 100);
    }

    private static SignalType GetSignal(int score, ScoringProfile p) =>
        score >= p.StrongBuyThreshold ? SignalType.StrongBuy
        : score >= p.BuyThreshold ? SignalType.Buy
        : score >= p.HoldThreshold ? SignalType.Hold
        : score >= p.SellThreshold ? SignalType.Sell
        : SignalType.StrongSell;

    private static string GenerateReasoning(string name, int tech, int fund, int sent, int div, SignalType signal,
        TechnicalIndicator? t, FundamentalMetric? f)
    {
        var parts = new List<string>();
        parts.Add($"{name}: Overall signal is {signal}.");
        parts.Add($"Technical ({tech}/100): " + (t?.RSI14.HasValue == true ? $"RSI at {t.RSI14:F1}" : "Limited data") + ".");
        parts.Add($"Fundamental ({fund}/100): " + (f?.PERatio.HasValue == true ? $"P/E of {f.PERatio:F1}, ROE {f.ROE:F1}%" : "Limited data") + ".");
        parts.Add($"Sentiment ({sent}/100). Dividend ({div}/100).");
        return string.Join(" ", parts);
    }
}
