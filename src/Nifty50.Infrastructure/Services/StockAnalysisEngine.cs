using System.Text.Json;
using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Nifty50.Infrastructure.Services;

public class StockAnalysisEngine : IStockAnalysisEngine
{
    private readonly IStockRepository _repo;
    private readonly IScoringProfileService _profileService;
    private readonly Microsoft.Extensions.Logging.ILogger<StockAnalysisEngine> _logger;

    public StockAnalysisEngine(IStockRepository repo, IScoringProfileService profileService, Microsoft.Extensions.Logging.ILogger<StockAnalysisEngine> logger)
    {
        _repo = repo;
        _profileService = profileService;
        _logger = logger;
    }

    public async Task<StockAnalysis> AnalyzeStockAsync(Guid stockId, ScoringProfile? profile = null)
    {
        profile ??= await _profileService.GetActiveProfileAsync();
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
            SentimentScore = sentScore,
            ScoringProfileName = profile.Name,
            Signal = overallSignal.ToString()
        };
        await _repo.AddScoreHistoryAsync(history);

        return analysis;
    }

    public async Task<List<StockAnalysis>> RecalculateAllAsync()
    {
        var alerts = new List<StockAnalysis>();
        var startTime = DateTime.UtcNow;
        var stocks = await _repo.GetAllAsync();
        var profile = await _profileService.GetActiveProfileAsync();
        foreach (var stock in stocks)
        {
            try 
            { 
                var analysis = await AnalyzeStockAsync(stock.Id, profile); 
                if (analysis.IsAlert)
                {
                    alerts.Add(analysis);
                }
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze stock {StockId}", stock.Id);
            }
        }
        await _repo.ClearAnalysesAsync(startTime);
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

        // MACD: score based on histogram magnitude and direction
        if (t.MACDHistogram.HasValue)
        {
            var hist = (double)t.MACDHistogram.Value;
            double macdScore;

            if (hist > 0)
            {
                // Bullish: base 60, stronger histogram → higher score
                macdScore = 60;
                // Normalize histogram strength relative to current price
                if (currentPrice.HasValue && currentPrice > 0)
                {
                    var normalizedHist = Math.Abs(hist) / (double)currentPrice.Value;
                    if (normalizedHist > 0.005) macdScore = 85;      // Strong bullish
                    else if (normalizedHist > 0.002) macdScore = 75;  // Moderate bullish
                }
            }
            else
            {
                // Bearish: base 40, stronger histogram → lower score
                macdScore = 40;
                if (currentPrice.HasValue && currentPrice > 0)
                {
                    var normalizedHist = Math.Abs(hist) / (double)currentPrice.Value;
                    if (normalizedHist > 0.005) macdScore = 15;      // Strong bearish
                    else if (normalizedHist > 0.002) macdScore = 25;  // Moderate bearish
                }
            }

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

        // ADX: trend strength + direction (uses +DI/-DI to determine bullish vs bearish)
        if (t.ADX14.HasValue)
        {
            var adx = (double)t.ADX14.Value;
            double adxScore;

            if (adx < 20)
            {
                // Weak/no trend — neutral
                adxScore = 50;
            }
            else if (t.PlusDI.HasValue && t.MinusDI.HasValue)
            {
                // Strong trend — use +DI vs -DI for direction
                if (t.PlusDI > t.MinusDI)
                {
                    // Bullish trend: the stronger the trend, the higher the score
                    adxScore = 55 + Math.Min(35, (adx - 20) / 80.0 * 35); // 55 → 90
                }
                else
                {
                    // Bearish trend: the stronger the trend, the lower the score
                    adxScore = 45 - Math.Min(35, (adx - 20) / 80.0 * 35); // 45 → 10
                }
            }
            else
            {
                // No directional data available — neutral
                adxScore = 50;
            }

            score += p.TechADXWeight / 100.0 * adxScore;
            totalWeightUsed += p.TechADXWeight;
        }

        // Volume (OBV): rising OBV = accumulation (bullish), falling OBV = distribution (bearish)
        if (t.OBV.HasValue && t.OBVSMA20.HasValue)
        {
            double obvScore;
            if (t.OBV > t.OBVSMA20)
                obvScore = 75; // OBV above its SMA = accumulation / bullish
            else
                obvScore = 25; // OBV below its SMA = distribution / bearish

            score += p.TechVolumeWeight / 100.0 * obvScore;
            totalWeightUsed += p.TechVolumeWeight;
        }
        else if (t.OBV.HasValue)
        {
            // No SMA available — skip volume scoring rather than guessing
            // Do NOT add to totalWeightUsed so it gets normalized out
        }

        // Normalize if not all sub-indicators had data
        if (totalWeightUsed > 0 && totalWeightUsed < 100)
            score = score * (100.0 / totalWeightUsed);

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

        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateFundamentalScore(FundamentalMetric? f, ScoringProfile p)
    {
        if (f == null) return 50;
        double score = 0;
        double totalWeight = 0;

        // Valuation (P/E ratio benchmarks for Indian large-cap stocks)
        double valScore = 50;
        if (f.PERatio.HasValue)
            valScore = f.PERatio < 12 ? 90 : f.PERatio < 20 ? 75 : f.PERatio < 30 ? 50 : f.PERatio < 45 ? 30 : 15;
        score += p.FundValuationWeight / 100.0 * valScore;
        totalWeight += p.FundValuationWeight;

        // PEG Ratio
        if (p.FundPEGWeight > 0 && f.PERatio.HasValue && f.EarningsGrowthYoY.HasValue && f.EarningsGrowthYoY > 0)
        {
            var peg = f.PERatio.Value / f.EarningsGrowthYoY.Value;
            double pegScore = peg < 1 ? 95 : peg < 1.5m ? 80 : peg < 2 ? 60 : peg < 3 ? 30 : 10;
            score += p.FundPEGWeight / 100.0 * pegScore;
            totalWeight += p.FundPEGWeight;
        }

        // Profitability (ROE benchmarks)
        double profScore = 50;
        if (f.ROE.HasValue)
            profScore = f.ROE > 25 ? 90 : f.ROE > 18 ? 75 : f.ROE > 12 ? 60 : f.ROE > 6 ? 40 : 20;
        // Boost if operating margin is also strong
        if (f.OperatingMargin.HasValue)
            profScore = Math.Min(100, profScore + (f.OperatingMargin > 20 ? 10 : f.OperatingMargin > 10 ? 5 : 0));
        score += p.FundProfitabilityWeight / 100.0 * profScore;
        totalWeight += p.FundProfitabilityWeight;

        // ROCE (Return on Capital Employed)
        if (p.FundROCEWeight > 0 && f.ROIC.HasValue)
        {
            double roceScore = 50;
            if (f.ROIC > 20) roceScore = 90;
            else if (f.ROIC > 15) roceScore = 75;
            else if (f.ROIC > 10) roceScore = 55;
            else if (f.ROIC > 5) roceScore = 30;
            else roceScore = 15;
            score += p.FundROCEWeight / 100.0 * roceScore;
            totalWeight += p.FundROCEWeight;
        }

        // Liquidity (Current Ratio)
        double liqScore = 50;
        if (f.CurrentRatio.HasValue)
            liqScore = f.CurrentRatio > 2.5m ? 85 : f.CurrentRatio > 1.5m ? 70 : f.CurrentRatio > 1m ? 50 : 20;
        score += p.FundLiquidityWeight / 100.0 * liqScore;
        totalWeight += p.FundLiquidityWeight;

        // Leverage (Debt-to-Equity)
        double levScore = 50;
        if (f.DebtToEquity.HasValue)
            levScore = f.DebtToEquity < 0.2m ? 90 : f.DebtToEquity < 0.5m ? 75 : f.DebtToEquity < 1m ? 55 : f.DebtToEquity < 2m ? 30 : 10;
        score += p.FundLeverageWeight / 100.0 * levScore;
        totalWeight += p.FundLeverageWeight;

        // Growth (Earnings Growth YoY)
        double growScore = 50;
        if (f.EarningsGrowthYoY.HasValue)
            growScore = f.EarningsGrowthYoY > 30 ? 90 : f.EarningsGrowthYoY > 15 ? 75 : f.EarningsGrowthYoY > 5 ? 60 : f.EarningsGrowthYoY > 0 ? 45 : 20;
        // Revenue growth bonus
        if (f.RevenueGrowthYoY.HasValue)
            growScore = Math.Min(100, growScore + (f.RevenueGrowthYoY > 20 ? 8 : f.RevenueGrowthYoY > 10 ? 4 : 0));
        score += p.FundGrowthWeight / 100.0 * growScore;
        totalWeight += p.FundGrowthWeight;

        if (totalWeight > 0 && totalWeight < 100)
            score = score * (100.0 / totalWeight);

        if (totalWeight == 0) return 50;

        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateValuationScore(IntrinsicValuation? v, FundamentalMetric? f, ScoringProfile p)
    {
        // Only score based on intrinsic valuation (Fair Value / Graham Number).
        // P/E is already scored in CalculateFundamentalScore; do NOT duplicate here.
        if (v?.UpsidePercent == null) return 50; // No valuation data — neutral default

        double score;
        var up = v.UpsidePercent.Value;
        if (up > 25) score = 95;
        else if (up > 10) score = 80;
        else if (up > 0) score = 65;
        else if (up > -10) score = 45;
        else if (up > -25) score = 25;
        else score = 10;

        return Math.Clamp((int)score, 0, 100);
    }

    private static int CalculateQualityScore(QualityMetric? q, ScoringProfile p)
    {
        if (q == null) return 50;
        double score = 0;
        double totalWeight = 0;

        if (p.QualPiotroskiWeight > 0 && q.PiotroskiFScore.HasValue)
        {
            var pio = q.PiotroskiFScore.Value;
            double s = 20;
            if (pio >= 8) s = 95;
            else if (pio >= 7) s = 85;
            else if (pio >= 5) s = 60;
            else if (pio >= 3) s = 40;
            score += p.QualPiotroskiWeight / 100.0 * s;
            totalWeight += p.QualPiotroskiWeight;
        }

        if (p.QualAltmanWeight > 0 && q.AltmanZScore.HasValue)
        {
            double s = 20;
            if (q.AltmanZone == "Safe") s = 90;
            else if (q.AltmanZone == "Grey") s = 50;
            score += p.QualAltmanWeight / 100.0 * s;
            totalWeight += p.QualAltmanWeight;
        }

        if (p.QualPromoterWeight > 0 && (q.PromoterHoldingTrend != null || q.PromoterHolding.HasValue))
        {
            double s = 50;
            if (q.PromoterHoldingTrend == "Increasing") s = 85;
            else if (q.PromoterHoldingTrend == "Decreasing") s = 20;
            else if (q.PromoterHolding.HasValue && q.PromoterHolding > 50) s = 70;
            score += p.QualPromoterWeight / 100.0 * s;
            totalWeight += p.QualPromoterWeight;
        }

        if (p.QualFIIWeight > 0 && (q.FIIHoldingTrend != null || q.FIIHolding.HasValue))
        {
            double s = 50;
            if (q.FIIHoldingTrend == "Increasing") s = 80;
            else if (q.FIIHoldingTrend == "Decreasing") s = 30;
            score += p.QualFIIWeight / 100.0 * s;
            totalWeight += p.QualFIIWeight;
        }

        if (p.QualDividendConsistencyWeight > 0 && q.ConsecutiveDividendYears.HasValue)
        {
            double s = 20;
            var yrs = q.ConsecutiveDividendYears.Value;
            if (yrs >= 10) s = 95;
            else if (yrs >= 5) s = 75;
            else if (yrs >= 3) s = 50;
            score += p.QualDividendConsistencyWeight / 100.0 * s;
            totalWeight += p.QualDividendConsistencyWeight;
        }

        if (p.QualFCFTrendWeight > 0 && q.FCFTrend != null)
        {
            double s = 50;
            if (q.FCFTrend == "Positive_Growing") s = 90;
            else if (q.FCFTrend == "Negative") s = 20;
            else if (q.FCFTrend == "Positive_Flat") s = 60;
            score += p.QualFCFTrendWeight / 100.0 * s;
            totalWeight += p.QualFCFTrendWeight;
        }

        if (totalWeight > 0 && totalWeight < 100)
            score = score * (100.0 / totalWeight);

        if (totalWeight == 0) return 50;

        return Math.Clamp((int)score, 0, 100);
    }

    /// <summary>Converts raw sentiment data into a 0-100 score using a sigmoid curve</summary>
    private static int CalculateSentimentScore(Nifty50.Core.Entities.SentimentAnalysis? sent)
    {
        if (sent == null) return 45; // No data = slightly below neutral (conservative)

        // Use the raw score (-1 to +1) with a sigmoid (tanh) curve for smooth graduation
        var raw = (double)sent.SentimentScore; // -1 to +1
        double curved = Math.Tanh(raw * 1.8); // Smooth S-curve, avoids hard clamping
        double baseScore = (curved + 1.0) / 2.0 * 100.0; // Map to 0-100

        // Confidence adjustment: fewer articles = regress toward neutral (50)
        int totalArticles = sent.PositiveCount + sent.NegativeCount + sent.NeutralCount;
        if (totalArticles <= 1)
        {
            // Very low confidence — pull heavily toward 50
            baseScore = baseScore * 0.4 + 50 * 0.6;
        }
        else if (totalArticles <= 3)
        {
            // Low confidence — pull somewhat toward 50
            baseScore = baseScore * 0.7 + 50 * 0.3;
        }
        // No pos/neg ratio bonus — this was double-counting (S4 fix)

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
