using System;
using System.Collections.Generic;

namespace Nifty50.Infrastructure.Services;

public static class SentimentScoringHelper
{
    private static readonly HashSet<string> PositiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Financial performance
        "profit", "growth", "upgrade", "beat", "record", "surge", "rally", "bullish",
        "outperform", "dividend", "gain", "positive", "expansion", "recovery", "optimistic",
        "boost", "soar", "impressive", "stellar", "robust", "upbeat", "exceeds", "surpass",
        // Indian market specific
        "buyback", "capex", "bonus", "accumulation",
    };

    private static readonly HashSet<string> NegativeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Financial distress
        "loss", "downgrade", "miss", "decline", "weak", "default", "fraud", "crash", "bearish",
        "underperform", "lawsuit", "recession", "warning", "concern", "investigation",
        "penalty", "slump", "plunge", "disappointing", "struggle", "collapse", "probe",
        // Indian market specific
        "insolvency", "pledge", "writeoff", "scam",
    };

    private static readonly HashSet<string> NegationWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "not", "no", "never", "neither", "nor", "don't", "doesn't", "didn't",
        "won't", "wouldn't", "can't", "cannot", "unlikely", "fail", "failed", "fails"
    };

    public static decimal ScoreHeadline(string headline)
    {
        if (string.IsNullOrWhiteSpace(headline)) return 0m;

        var lower = headline.ToLowerInvariant();
        var words = headline.Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '(', ')', '"', '\'' },
            StringSplitOptions.RemoveEmptyEntries);

        int posCount = 0, negCount = 0;
        bool negated = false;
        int negationWindow = 0;

        foreach (var word in words)
        {
            // Check if this word is a negation trigger
            if (NegationWords.Contains(word))
            {
                negated = true;
                negationWindow = 3; // Negate the next 3 words
                continue;
            }

            if (PositiveKeywords.Contains(word))
            {
                if (negated) negCount++; // "not profitable" → negative
                else posCount++;
            }
            if (NegativeKeywords.Contains(word))
            {
                if (negated) posCount++; // "no loss" → positive
                else negCount++;
            }

            // Decay the negation window
            if (negated)
            {
                negationWindow--;
                if (negationWindow <= 0) negated = false;
            }
        }

        // Bigram / phrase patterns for stronger signals
        if (lower.Contains("all-time high") || lower.Contains("52-week high")) posCount += 2;
        if (lower.Contains("52-week low") || lower.Contains("all-time low")) negCount += 2;
        if (lower.Contains("target raised") || lower.Contains("target upgraded")) posCount++;
        if (lower.Contains("target cut") || lower.Contains("rating cut")) negCount++;
        if (lower.Contains("strong buy") || lower.Contains("top pick")) posCount += 2;
        if (lower.Contains("strong sell") || lower.Contains("avoid")) negCount += 2;
        
        // Indian market specific phrases
        if (lower.Contains("fii buying") || lower.Contains("dii inflow")) posCount += 2;
        if (lower.Contains("fii selling") || lower.Contains("dii outflow")) negCount += 2;
        if (lower.Contains("promoter buying") || lower.Contains("stake increase")) posCount++;
        if (lower.Contains("promoter selling") || lower.Contains("stake sale")) negCount++;
        if (lower.Contains("sebi approval") || lower.Contains("order book")) posCount++;
        if (lower.Contains("sebi penalty") || lower.Contains("sebi ban")) negCount += 2;
        if (lower.Contains("npa") || lower.Contains("bad loan")) negCount++;
        if (lower.Contains("nclt") || lower.Contains("insolvency")) negCount += 2;
        if (lower.Contains("gst raid") || lower.Contains("income tax raid")) negCount++;
        if (lower.Contains("pat growth") || lower.Contains("listing gain")) posCount++;

        int total = posCount + negCount;
        if (total == 0) return 0m;
        return (decimal)(posCount - negCount) / total;
    }
}
