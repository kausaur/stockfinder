using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class YahooSentimentService : ISentimentService
{
    private readonly HttpClient _http;
    private readonly ILogger<YahooSentimentService> _logger;
    private readonly IApiMonitorService _monitor;

    private static readonly HashSet<string> PositiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "profit", "growth", "upgrade", "beat", "record", "surge", "strong", "rally", "bullish",
        "outperform", "revenue", "earnings", "dividend", "buy", "gain", "rise", "positive",
        "expansion", "innovation", "breakthrough", "recovery", "optimistic", "boost"
    };

    private static readonly HashSet<string> NegativeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "loss", "downgrade", "miss", "decline", "weak", "default", "fraud", "crash", "bearish",
        "underperform", "debt", "lawsuit", "sell", "fall", "drop", "negative", "recession",
        "warning", "risk", "concern", "investigation", "penalty", "slump"
    };

    public YahooSentimentService(HttpClient http, ILogger<YahooSentimentService> logger, IApiMonitorService monitor)
    {
        _http = http;
        _logger = logger;
        _monitor = monitor;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<SentimentAnalysis> AnalyzeSentimentAsync(string companyName, string symbol)
    {
        var headlines = await FetchHeadlinesAsync(symbol);
        int pos = 0, neg = 0, neut = 0;
        decimal totalScore = 0;

        foreach (var headline in headlines)
        {
            var score = ScoreHeadline(headline);
            totalScore += score;
            if (score > 0.1m) pos++;
            else if (score < -0.1m) neg++;
            else neut++;
        }

        var avgScore = headlines.Count > 0 ? totalScore / headlines.Count : 0;
        var sentiment = avgScore > 0.1m ? SentimentType.Bullish
                      : avgScore < -0.1m ? SentimentType.Bearish
                      : SentimentType.Neutral;

        return new SentimentAnalysis
        {
            AnalyzedAt = DateTime.UtcNow,
            OverallSentiment = sentiment,
            SentimentScore = Math.Clamp(avgScore, -1m, 1m),
            PositiveCount = pos,
            NegativeCount = neg,
            NeutralCount = neut,
            TopHeadlines = JsonSerializer.Serialize(headlines.Take(10))
        };
    }

    private async Task<List<string>> FetchHeadlinesAsync(string symbol)
    {
        var url = $"https://query2.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(symbol)}&newsCount=10";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("YahooNews", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("news", out var newsArray))
            {
                return newsArray.EnumerateArray()
                    .Select(n => n.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t!)
                    .ToList();
            }
            return new List<string>();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch news for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("YahooNews", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<string>();
        }
    }

    private static decimal ScoreHeadline(string headline)
    {
        var words = headline.Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '(', ')', '"', '\'' },
            StringSplitOptions.RemoveEmptyEntries);
        int posCount = 0, negCount = 0;
        foreach (var word in words)
        {
            if (PositiveKeywords.Contains(word)) posCount++;
            if (NegativeKeywords.Contains(word)) negCount++;
        }
        int total = posCount + negCount;
        if (total == 0) return 0m;
        return (decimal)(posCount - negCount) / total;
    }
}
