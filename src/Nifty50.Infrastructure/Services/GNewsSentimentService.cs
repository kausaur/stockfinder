using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

/// <summary>
/// Primary sentiment service using GNews API for high-quality, stock-specific news headlines.
/// Falls back to Yahoo Finance search if GNews is unavailable or API key is not configured.
/// </summary>
public class GNewsSentimentService : ISentimentService
{
    private readonly HttpClient _http;
    private readonly ILogger<GNewsSentimentService> _logger;
    private readonly IApiMonitorService _monitor;
    private readonly string? _apiKey;

    private static readonly HashSet<string> PositiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "profit", "growth", "upgrade", "beat", "record", "surge", "strong", "rally", "bullish",
        "outperform", "revenue", "earnings", "dividend", "buy", "gain", "rise", "positive",
        "expansion", "innovation", "breakthrough", "recovery", "optimistic", "boost", "soar",
        "high", "up", "jump", "momentum", "leader", "winner", "opportunity", "upbeat",
        "robust", "accelerate", "exceed", "top", "best", "impressive", "stellar"
    };

    private static readonly HashSet<string> NegativeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "loss", "downgrade", "miss", "decline", "weak", "default", "fraud", "crash", "bearish",
        "underperform", "debt", "lawsuit", "sell", "fall", "drop", "negative", "recession",
        "warning", "risk", "concern", "investigation", "penalty", "slump", "plunge",
        "low", "down", "cut", "layoff", "closure", "trouble", "crisis", "volatile",
        "disappointing", "worst", "struggle", "fail", "collapse", "delay", "probe"
    };

    public GNewsSentimentService(HttpClient http, ILogger<GNewsSentimentService> logger,
        IApiMonitorService monitor, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _monitor = monitor;
        _apiKey = config["GNewsApiKey"];
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<SentimentAnalysis> AnalyzeSentimentAsync(string companyName, string symbol)
    {
        // Try GNews first if API key is configured
        var headlines = new List<string>();
        string source = "None";

        if (!string.IsNullOrEmpty(_apiKey) && _apiKey != "YOUR_GNEWS_API_KEY")
        {
            headlines = await FetchGNewsHeadlinesAsync(companyName);
            source = "GNews";
        }

        // Fall back to Yahoo Finance search if GNews returned nothing
        if (headlines.Count == 0)
        {
            headlines = await FetchYahooHeadlinesAsync(symbol);
            source = headlines.Count > 0 ? "Yahoo" : "None";
        }

        _logger.LogInformation("Sentiment for {Symbol}: {Count} headlines from {Source}", symbol, headlines.Count, source);

        int pos = 0, neg = 0, neut = 0;
        decimal totalScore = 0;

        foreach (var headline in headlines)
        {
            var score = ScoreHeadline(headline);
            totalScore += score;
            if (score > 0.05m) pos++;
            else if (score < -0.05m) neg++;
            else neut++;
        }

        var avgScore = headlines.Count > 0 ? totalScore / headlines.Count : 0;
        var sentiment = avgScore > 0.08m ? SentimentType.Bullish
                      : avgScore < -0.08m ? SentimentType.Bearish
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

    /// <summary>Fetches headlines from GNews API — returns stock-specific, high-quality results</summary>
    private async Task<List<string>> FetchGNewsHeadlinesAsync(string companyName)
    {
        var query = Uri.EscapeDataString($"{companyName} stock");
        var url = $"https://gnews.io/api/v4/search?q={query}&lang=en&max=10&apikey={_apiKey}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("GNews", url.Replace(_apiKey!, "***"), DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("articles", out var articles))
            {
                return articles.EnumerateArray()
                    .Select(a => a.TryGetProperty("title", out var t) ? t.GetString() : null)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t!)
                    .ToList();
            }
            return new List<string>();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "GNews failed for {Company}, will fall back to Yahoo", companyName);
            _monitor.RecordApiCall(new ApiCallRecord("GNews", url.Replace(_apiKey!, "***"), DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<string>();
        }
    }

    /// <summary>Fallback: fetches headlines from Yahoo Finance search endpoint</summary>
    private async Task<List<string>> FetchYahooHeadlinesAsync(string symbol)
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
            _logger.LogWarning(ex, "Yahoo news fallback also failed for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("YahooNews", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<string>();
        }
    }

    /// <summary>Scores a headline based on keyword matching with bigram awareness</summary>
    private static decimal ScoreHeadline(string headline)
    {
        var lower = headline.ToLowerInvariant();
        var words = headline.Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '(', ')', '"', '\'' },
            StringSplitOptions.RemoveEmptyEntries);

        int posCount = 0, negCount = 0;

        foreach (var word in words)
        {
            if (PositiveKeywords.Contains(word)) posCount++;
            if (NegativeKeywords.Contains(word)) negCount++;
        }

        // Bigram / phrase patterns for stronger signals
        if (lower.Contains("all-time high") || lower.Contains("52-week high")) posCount += 2;
        if (lower.Contains("52-week low") || lower.Contains("all-time low")) negCount += 2;
        if (lower.Contains("target raised") || lower.Contains("price target")) posCount++;
        if (lower.Contains("target cut") || lower.Contains("rating cut")) negCount++;
        if (lower.Contains("strong buy") || lower.Contains("top pick")) posCount += 2;
        if (lower.Contains("strong sell") || lower.Contains("avoid")) negCount += 2;

        int total = posCount + negCount;
        if (total == 0) return 0m;
        return (decimal)(posCount - negCount) / total;
    }
}
