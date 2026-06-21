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

        // Get current price for Bollinger Band scoring
        decimal? currentPrice = stock?.CurrentPrice;

        int techScore = CalculateTechnicalScore(tech, profile, currentPrice);
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

        var reasoning = GenerateReasoning(stock?.CompanyName ?? "", techScore, fundScore, sentScore, divScore, overallSignal, tech, fund, sent);

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
            AlertMessage = isAlert ? $"🚨 {overallSignal} signal for {stock?.CompanyName} (Score: {overallScore}/100)" : null,
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

    private static int CalculateTechnicalScore(TechnicalIndicator? t, ScoringProfile p, decimal? currentPrice)
    {
        if (t == null) return 50;
        double score = 0;

        // RSI: <30 oversold (buy signal), >70 overbought (sell signal)
        if (t.RSI14.HasValue)
        {
            var rsi = (double)t.RSI14.Value;
            double rsiScore = rsi < 30 ? 90
                : rsi < 40 ? 75
                : rsi < 50 ? 60
                : rsi < 60 ? 50
                : rsi < 70 ? 35
                : 15; // >70 overbought
            score += p.TechRSIWeight / 100.0 * rsiScore;
        }

        // MACD: histogram direction + signal crossover
        if (t.MACDHistogram.HasValue)
        {
            double macdScore = t.MACDHistogram > 0 ? 65 : 35;
            if (t.MACD.HasValue && t.MACDSignal.HasValue)
            {
                if (t.MACD > t.MACDSignal) macdScore += 15; // Bullish crossover
                else macdScore -= 10;
            }
            score += p.TechMACDWeight / 100.0 * Math.Clamp(macdScore, 0, 100);
        }

        // Moving Average: Golden Cross (SMA50 > SMA200) vs Death Cross
        if (t.SMA50.HasValue && t.SMA200.HasValue)
        {
            double maScore = t.SMA50 > t.SMA200 ? 75 : 25;
            // Price above 50-day MA is additional bullish signal
            if (currentPrice.HasValue)
            {
                if (currentPrice > t.SMA50) maScore += 10;
                if (currentPrice > t.SMA200) maScore += 5;
            }
            score += p.TechMovingAvgWeight / 100.0 * Math.Clamp(maScore, 0, 100);
        }

        // Bollinger Bands: score based on price position relative to bands
        if (t.BollingerLower.HasValue && t.BollingerUpper.HasValue && t.BollingerMiddle.HasValue)
        {
            double bbScore = 50; // neutral default
            if (currentPrice.HasValue)
            {
                var lower = (double)t.BollingerLower.Value;
                var upper = (double)t.BollingerUpper.Value;
                var middle = (double)t.BollingerMiddle.Value;
                var price = (double)currentPrice.Value;
                var bandRange = upper - lower;

                if (bandRange > 0)
                {
                    // Price below lower band: oversold, strong buy signal
                    // Price above upper band: overbought, sell signal
                    // Price near middle: neutral
                    if (price <= lower) bbScore = 85;
                    else if (price >= upper) bbScore = 15;
                    else
                    {
                        // Interpolate: lower half of band is bullish, upper half is bearish
                        var positionInBand = (price - lower) / bandRange; // 0=at lower, 1=at upper
                        bbScore = 80 - (positionInBand * 65); // 80 at lower, 15 at upper
                    }
                }
            }
            score += p.TechBollingerWeight / 100.0 * bbScore;
        }

        // ADX: trend strength (>25 = strong trend, good for momentum strategies)
        if (t.ADX14.HasValue)
        {
            var adx = (double)t.ADX14.Value;
            double adxScore = adx > 40 ? 80 : adx > 25 ? 65 : adx > 15 ? 45 : 30;
            score += p.TechADXWeight / 100.0 * adxScore;
        }

        // Volume (OBV): positive OBV trend confirms price moves
        if (t.OBV.HasValue)
        {
            // OBV being positive is a good sign of accumulation
            double obvScore = t.OBV > 0 ? 65 : 40;
            score += p.TechVolumeWeight / 100.0 * obvScore;
        }

        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateFundamentalScore(FundamentalMetric? f, ScoringProfile p)
    {
        if (f == null) return 50;
        double score = 0;

        // Valuation (P/E ratio benchmarks for Indian large-cap stocks)
        double valScore = 50;
        if (f.PERatio.HasValue)
            valScore = f.PERatio < 12 ? 90 : f.PERatio < 20 ? 75 : f.PERatio < 30 ? 50 : f.PERatio < 45 ? 30 : 15;
        score += p.FundValuationWeight / 100.0 * valScore;

        // Profitability (ROE benchmarks)
        double profScore = 50;
        if (f.ROE.HasValue)
            profScore = f.ROE > 25 ? 90 : f.ROE > 18 ? 75 : f.ROE > 12 ? 60 : f.ROE > 6 ? 40 : 20;
        // Boost if operating margin is also strong
        if (f.OperatingMargin.HasValue)
            profScore = Math.Min(100, profScore + (f.OperatingMargin > 20 ? 10 : f.OperatingMargin > 10 ? 5 : 0));
        score += p.FundProfitabilityWeight / 100.0 * profScore;

        // Liquidity (Current Ratio)
        double liqScore = 50;
        if (f.CurrentRatio.HasValue)
            liqScore = f.CurrentRatio > 2.5m ? 85 : f.CurrentRatio > 1.5m ? 70 : f.CurrentRatio > 1 ? 50 : 20;
        score += p.FundLiquidityWeight / 100.0 * liqScore;

        // Leverage (Debt-to-Equity)
        double levScore = 50;
        if (f.DebtToEquity.HasValue)
            levScore = f.DebtToEquity < 0.2m ? 90 : f.DebtToEquity < 0.5m ? 75 : f.DebtToEquity < 1m ? 55 : f.DebtToEquity < 2m ? 30 : 10;
        score += p.FundLeverageWeight / 100.0 * levScore;

        // Growth (Earnings Growth YoY)
        double growScore = 50;
        if (f.EarningsGrowthYoY.HasValue)
            growScore = f.EarningsGrowthYoY > 30 ? 90 : f.EarningsGrowthYoY > 15 ? 75 : f.EarningsGrowthYoY > 5 ? 60 : f.EarningsGrowthYoY > 0 ? 45 : 20;
        // Revenue growth bonus
        if (f.RevenueGrowthYoY.HasValue)
            growScore = Math.Min(100, growScore + (f.RevenueGrowthYoY > 20 ? 8 : f.RevenueGrowthYoY > 10 ? 4 : 0));
        score += p.FundGrowthWeight / 100.0 * growScore;

        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateDividendScore(FundamentalMetric? f)
    {
        if (f == null) return 50;
        double score = 50;

        // Dividend yield (Indian market context: >3% is excellent)
        if (f.DividendYield.HasValue)
            score = f.DividendYield > 5 ? 90 : f.DividendYield > 3 ? 78 : f.DividendYield > 1.5m ? 62 : f.DividendYield > 0.5m ? 45 : 25;

        // Payout ratio penalty: >80% is unsustainable
        if (f.DividendPayoutRatio.HasValue && f.DividendPayoutRatio > 80)
            score = Math.Max(0, score - 15);

        return Math.Clamp((int)score, 0, 100);
    }

    private static SignalType GetSignal(int score, ScoringProfile p) =>
        score >= p.StrongBuyThreshold ? SignalType.StrongBuy
        : score >= p.BuyThreshold ? SignalType.Buy
        : score >= p.HoldThreshold ? SignalType.Hold
        : score >= p.SellThreshold ? SignalType.Sell
        : SignalType.StrongSell;

    private static string GenerateReasoning(string name, int tech, int fund, int sent, int div,
        SignalType signal, TechnicalIndicator? t, FundamentalMetric? f, Nifty50.Core.Entities.SentimentAnalysis? se)
    {
        var parts = new List<string>();
        parts.Add($"{name}: Overall signal is {signal}.");

        // Technical summary
        var techDetail = new List<string>();
        if (t?.RSI14.HasValue == true)
        {
            var rsi = t.RSI14.Value;
            techDetail.Add($"RSI {rsi:F0} ({(rsi < 30 ? "oversold" : rsi > 70 ? "overbought" : "neutral")})");
        }
        if (t?.SMA50.HasValue == true && t?.SMA200.HasValue == true)
            techDetail.Add(t.SMA50 > t.SMA200 ? "Golden Cross" : "Death Cross");
        if (t?.MACDHistogram.HasValue == true)
            techDetail.Add($"MACD {(t.MACDHistogram > 0 ? "bullish" : "bearish")}");
        parts.Add($"Technical ({tech}/100): {(techDetail.Count > 0 ? string.Join(", ", techDetail) : "Limited data")}.");

        // Fundamental summary
        var fundDetail = new List<string>();
        if (f?.PERatio.HasValue == true) fundDetail.Add($"P/E {f.PERatio:F1}x");
        if (f?.ROE.HasValue == true) fundDetail.Add($"ROE {f.ROE:F1}%");
        if (f?.EarningsGrowthYoY.HasValue == true) fundDetail.Add($"EPS growth {f.EarningsGrowthYoY:F1}% YoY");
        if (f?.DebtToEquity.HasValue == true) fundDetail.Add($"D/E {f.DebtToEquity:F2}");
        parts.Add($"Fundamental ({fund}/100): {(fundDetail.Count > 0 ? string.Join(", ", fundDetail) : "Limited data")}.");

        // Sentiment summary
        if (se != null)
            parts.Add($"Sentiment ({sent}/100): {se.OverallSentiment} ({se.PositiveCount}↑ {se.NegativeCount}↓).");
        else
            parts.Add($"Sentiment ({sent}/100): No recent news data.");

        // Dividend summary
        if (f?.DividendYield.HasValue == true && f.DividendYield > 0)
            parts.Add($"Dividend ({div}/100): Yield {f.DividendYield:F2}%.");
        else
            parts.Add($"Dividend ({div}/100): No dividend data.");

        return string.Join(" ", parts);
    }
}
