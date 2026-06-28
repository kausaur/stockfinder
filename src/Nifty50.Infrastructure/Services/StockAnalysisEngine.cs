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

        // Get current price and 52-week data for scoring
        decimal? currentPrice = stock?.CurrentPrice;
        decimal? week52High = stock?.Week52High;
        decimal? week52Low = stock?.Week52Low;

        var val = await _repo.GetLatestValuationAsync(stockId);
        var qual = await _repo.GetLatestQualityAsync(stockId);

        int techScore = CalculateTechnicalScore(tech, profile, currentPrice, week52High, week52Low);
        int fundScore = CalculateFundamentalScore(fund, profile);
        int sentScore = CalculateSentimentScore(sent);
        int divScore = CalculateDividendScore(fund);
        int valScore = CalculateValuationScore(val, fund, profile);
        int qualScore = CalculateQualityScore(qual, profile);

        double overall = (profile.TechnicalWeight / 100.0 * techScore)
                       + (profile.FundamentalWeight / 100.0 * fundScore)
                       + (profile.SentimentWeight / 100.0 * sentScore)
                       + (profile.DividendWeight / 100.0 * divScore)
                       + (profile.ValuationWeight / 100.0 * valScore)
                       + (profile.QualityWeight / 100.0 * qualScore);
        int overallScore = Math.Clamp((int)overall, 0, 100);

        var overallSignal = GetSignal(overallScore, profile);
        // An alert fires if the stock meets ALL of the user-defined minimum thresholds.
        // If a threshold is set to 0, it essentially disables that specific filter.
        bool isAlert = overallScore >= profile.AlertMinOverallScore
                    && techScore >= profile.AlertMinTechnicalScore
                    && fundScore >= profile.AlertMinFundamentalScore
                    && sentScore >= profile.AlertMinSentimentScore;

        var reasoning = GenerateReasoning(stock?.CompanyName ?? "", techScore, fundScore, sentScore, divScore, valScore, qualScore, overallSignal, tech, fund, sent, val, qual);

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
            ValuationScore = valScore,
            QualityScore = qualScore,
            OverallScore = overallScore,
            WeightsUsed = JsonSerializer.Serialize(new { profile.TechnicalWeight, profile.FundamentalWeight, profile.SentimentWeight, profile.DividendWeight, profile.ValuationWeight, profile.QualityWeight }),
            Reasoning = reasoning,
            IsAlert = isAlert,
            AlertMessage = isAlert ? $"🚨 {overallSignal} signal for {stock?.CompanyName} (Score: {overallScore}/100)" : null,
        };

        await _repo.AddAnalysisAsync(analysis);

        var history = new ScoreHistory
        {
            StockId = stockId,
            RecordedAt = DateTime.UtcNow,
            OverallScore = overallScore,
            TechnicalScore = techScore,
            FundamentalScore = fundScore,
            ValuationScore = valScore,
            QualityScore = qualScore,
            DividendScore = divScore,
            SentimentScore = sentScore
        };
        await _repo.AddScoreHistoryAsync(history);

        return analysis;
    }

    public async Task<List<StockAnalysis>> RecalculateAllAsync()
    {
        var alerts = new List<StockAnalysis>();
        await _repo.ClearAnalysesAsync();
        var stocks = await _repo.GetAllAsync();
        foreach (var stock in stocks)
        {
            try 
            { 
                var analysis = await AnalyzeStockAsync(stock.Id); 
                if (analysis.IsAlert)
                {
                    alerts.Add(analysis);
                }
            } 
            catch { /* Skip failed analyses */ }
        }
        return alerts;
    }

    private static int CalculateTechnicalScore(TechnicalIndicator? t, ScoringProfile p, decimal? currentPrice,
        decimal? week52High = null, decimal? week52Low = null)
    {
        if (t == null) return 50;
        double score = 0;
        double totalWeightUsed = 0;

        // RSI: <30 oversold (buy signal), >70 overbought (sell signal)
        if (t.RSI14.HasValue)
        {
            var rsi = (double)t.RSI14.Value;
            // Continuous interpolation for more granular scoring
            double rsiScore;
            if (rsi <= 20) rsiScore = 95;
            else if (rsi <= 30) rsiScore = 95 - (rsi - 20) / 10 * 10; // 95→85
            else if (rsi <= 40) rsiScore = 85 - (rsi - 30) / 10 * 15; // 85→70
            else if (rsi <= 50) rsiScore = 70 - (rsi - 40) / 10 * 15; // 70→55
            else if (rsi <= 60) rsiScore = 55 - (rsi - 50) / 10 * 15; // 55→40
            else if (rsi <= 70) rsiScore = 40 - (rsi - 60) / 10 * 15; // 40→25
            else rsiScore = Math.Max(5, 25 - (rsi - 70) / 10 * 15);  // 25→10
            score += p.TechRSIWeight / 100.0 * rsiScore;
            totalWeightUsed += p.TechRSIWeight;
        }

        // MACD: histogram direction + signal crossover + histogram magnitude
        if (t.MACDHistogram.HasValue)
        {
            double macdScore = t.MACDHistogram > 0 ? 70 : 30;
            if (t.MACD.HasValue && t.MACDSignal.HasValue)
            {
                if (t.MACD > t.MACDSignal) macdScore += 15; // Bullish crossover
                else macdScore -= 15; // Bearish crossover
            }
            // Histogram strength bonus: bigger histogram = stronger conviction
            var histAbs = Math.Abs((double)t.MACDHistogram.Value);
            if (histAbs > 5) macdScore += (t.MACDHistogram > 0 ? 5 : -5);
            score += p.TechMACDWeight / 100.0 * Math.Clamp(macdScore, 0, 100);
            totalWeightUsed += p.TechMACDWeight;
        }

        // Moving Average: Golden Cross (SMA50 > SMA200) vs Death Cross
        if (t.SMA50.HasValue && t.SMA200.HasValue)
        {
            double maScore = t.SMA50 > t.SMA200 ? 78 : 22;
            if (currentPrice.HasValue)
            {
                // Price position relative to MAs gives stronger signals
                if (currentPrice > t.SMA50 && currentPrice > t.SMA200) maScore += 12;
                else if (currentPrice > t.SMA50) maScore += 5;
                else if (currentPrice < t.SMA50 && currentPrice < t.SMA200) maScore -= 8;
            }
            score += p.TechMovingAvgWeight / 100.0 * Math.Clamp(maScore, 0, 100);
            totalWeightUsed += p.TechMovingAvgWeight;
        }

        // Bollinger Bands: price position relative to bands
        if (t.BollingerLower.HasValue && t.BollingerUpper.HasValue && t.BollingerMiddle.HasValue)
        {
            double bbScore = 50;
            if (currentPrice.HasValue)
            {
                var lower = (double)t.BollingerLower.Value;
                var upper = (double)t.BollingerUpper.Value;
                var price = (double)currentPrice.Value;
                var bandRange = upper - lower;

                if (bandRange > 0)
                {
                    if (price <= lower) bbScore = 90;
                    else if (price >= upper) bbScore = 10;
                    else
                    {
                        var positionInBand = (price - lower) / bandRange;
                        bbScore = 88 - (positionInBand * 76); // 88 at lower → 12 at upper
                    }
                }
            }
            score += p.TechBollingerWeight / 100.0 * bbScore;
            totalWeightUsed += p.TechBollingerWeight;
        }

        // ADX: trend strength (>25 = strong trend, good for momentum strategies)
        if (t.ADX14.HasValue)
        {
            var adx = (double)t.ADX14.Value;
            double adxScore = adx > 50 ? 90 : adx > 40 ? 80 : adx > 25 ? 65 : adx > 15 ? 40 : 25;
            score += p.TechADXWeight / 100.0 * adxScore;
            totalWeightUsed += p.TechADXWeight;
        }

        // Volume (OBV): positive OBV = accumulation, negative = distribution
        if (t.OBV.HasValue)
        {
            double obvScore = t.OBV > 0 ? 70 : 35;
            score += p.TechVolumeWeight / 100.0 * obvScore;
            totalWeightUsed += p.TechVolumeWeight;
        }

        // 52-Week proximity bonus (not weighted by sub-weights, acts as a bonus)
        if (currentPrice.HasValue && week52High.HasValue && week52Low.HasValue && week52High > week52Low)
        {
            var range = (double)(week52High.Value - week52Low.Value);
            var position = (double)(currentPrice.Value - week52Low.Value) / range; // 0=at low, 1=at high
            // Near 52-week low: value opportunity (buy signal); near 52-week high: momentum (slightly bullish)
            // Sweet spot: 0.3-0.6 (recovering from lows but not overbought)
            double w52Score;
            if (position < 0.15) w52Score = 80;       // Deep value territory
            else if (position < 0.35) w52Score = 72;  // Value zone
            else if (position < 0.55) w52Score = 58;  // Neutral-bullish
            else if (position < 0.75) w52Score = 48;  // Getting expensive
            else if (position < 0.90) w52Score = 35;  // Near highs, caution
            else w52Score = 20;                        // At 52-week high, overbought
            // Apply as a small bonus (10% effective weight)
            score = score * 0.90 + w52Score * 0.10;
        }

        // Normalize if not all sub-indicators had data
        if (totalWeightUsed > 0 && totalWeightUsed < 100)
            score = score * (100.0 / totalWeightUsed);

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

    private static int CalculateValuationScore(IntrinsicValuation? v, FundamentalMetric? f, ScoringProfile p)
    {
        double score = 50; // default middle
        if (v?.UpsidePercent != null)
        {
            var up = v.UpsidePercent.Value;
            if (up > 25) score = 95;
            else if (up > 10) score = 80;
            else if (up > 0) score = 65;
            else if (up > -10) score = 45;
            else if (up > -25) score = 25;
            else score = 10;
        }
        else if (f?.PERatio != null) // Graceful degradation if no intrinsic valuation
        {
            var pe = f.PERatio.Value;
            if (pe < 12) score = 90;
            else if (pe < 20) score = 75;
            else if (pe < 30) score = 50;
            else if (pe < 45) score = 30;
            else score = 15;
        }
        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateQualityScore(QualityMetric? q, ScoringProfile p)
    {
        if (q == null) return 50;
        double score = 50;

        if (q.PiotroskiFScore.HasValue)
        {
            var pio = q.PiotroskiFScore.Value;
            if (pio >= 8) score = 95;
            else if (pio >= 7) score = 85;
            else if (pio >= 5) score = 60;
            else if (pio >= 3) score = 40;
            else score = 20;
        }

        // Apply bonus/penalty based on FCF trend
        if (q.FCFTrend == "Positive_Growing") score = Math.Min(100, score + 10);
        else if (q.FCFTrend == "Negative") score = Math.Max(0, score - 15);

        return Math.Clamp((int)score, 0, 100);
    }

    /// <summary>Converts raw sentiment data into a 0-100 score with more variance</summary>
    private static int CalculateSentimentScore(Nifty50.Core.Entities.SentimentAnalysis? sent)
    {
        if (sent == null) return 50;

        // Use the raw score (-1 to +1) but apply a wider mapping
        var raw = (double)sent.SentimentScore; // -1 to +1
        // Apply sigmoid-like curve to amplify differences around neutral
        double amplified = raw * 2.5; // Amplify small differences
        amplified = Math.Clamp(amplified, -1, 1);
        double baseScore = (amplified + 1.0) / 2.0 * 100.0; // Map to 0-100

        // Bonus/penalty from article count balance
        int totalArticles = sent.PositiveCount + sent.NegativeCount + sent.NeutralCount;
        if (totalArticles > 0)
        {
            double posRatio = (double)sent.PositiveCount / totalArticles;
            double negRatio = (double)sent.NegativeCount / totalArticles;
            // Shift score based on article sentiment ratio
            baseScore += (posRatio - negRatio) * 20; // Up to ±20 point swing
        }

        // Volume bonus: more articles = more confidence in the score
        if (totalArticles >= 5) baseScore += 3;
        else if (totalArticles <= 1) baseScore -= 3; // Low confidence penalty

        return Math.Clamp((int)baseScore, 0, 100);
    }

    private static int CalculateDividendScore(FundamentalMetric? f)
    {
        if (f == null || !f.DividendYield.HasValue) return 30; // No dividend = below average
        double score;

        // Dividend yield (Indian market context: >3% is excellent)
        var yield = (double)f.DividendYield.Value;
        if (yield > 6) score = 95;
        else if (yield > 4) score = 88;
        else if (yield > 3) score = 80;
        else if (yield > 2) score = 68;
        else if (yield > 1) score = 55;
        else if (yield > 0.3) score = 42;
        else score = 30;

        // Payout ratio adjustment: graduated rather than cliff penalty
        if (f.DividendPayoutRatio.HasValue)
        {
            var payout = (double)f.DividendPayoutRatio.Value;
            if (payout > 90) score -= 20;       // Dangerously high
            else if (payout > 80) score -= 12;  // Unsustainable
            else if (payout > 60) score -= 3;   // Moderate (slight concern)
            else if (payout >= 25 && payout <= 60) score += 5; // Healthy payout range bonus
        }

        return Math.Clamp((int)score, 0, 100);
    }

    private static SignalType GetSignal(int score, ScoringProfile p) =>
        score >= p.StrongBuyThreshold ? SignalType.StrongBuy
        : score >= p.BuyThreshold ? SignalType.Buy
        : score >= p.HoldThreshold ? SignalType.Hold
        : score >= p.SellThreshold ? SignalType.Sell
        : SignalType.StrongSell;

    private static string GenerateReasoning(string name, int tech, int fund, int sent, int div, int val, int qual,
        SignalType signal, TechnicalIndicator? t, FundamentalMetric? f, Nifty50.Core.Entities.SentimentAnalysis? se, IntrinsicValuation? v, QualityMetric? q)
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

        // Valuation summary
        if (v?.UpsidePercent.HasValue == true)
            parts.Add($"Valuation ({val}/100): {v.ValuationVerdict} ({v.UpsidePercent:F1}% upside).");

        // Quality summary
        if (q?.PiotroskiFScore.HasValue == true)
            parts.Add($"Quality ({qual}/100): Piotroski F-Score {q.PiotroskiFScore}.");

        return string.Join(" ", parts);
    }
}
