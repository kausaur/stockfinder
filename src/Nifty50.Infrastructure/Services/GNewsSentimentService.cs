using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class GNewsSentimentService : ISentimentService
{
    private readonly HttpClient _http;
    private readonly ILogger<GNewsSentimentService> _logger;
    private readonly IApiMonitorService _monitor;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

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

    public GNewsSentimentService(HttpClient http, ILogger<GNewsSentimentService> logger, IApiMonitorService monitor, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _monitor = monitor;
        _config = config;
    }

    public async Task<SentimentAnalysis> AnalyzeSentimentAsync(string companyName, string symbol)
    {
        var headlines = await FetchHeadlinesAsync(companyName);
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

    private async Task<List<string>> FetchHeadlinesAsync(string companyName)
    {
        var query = Uri.EscapeDataString($"{companyName} stock");
        var apiKey = _config["GNewsApiKey"];
        var url = $"https://gnews.io/api/v4/search?q={query}&lang=en&max=10&apikey={apiKey}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("GNews", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            using var doc = JsonDocument.Parse(json);
            var articles = doc.RootElement.GetProperty("articles");
            return articles.EnumerateArray()
                .Select(a => a.GetProperty("title").GetString() ?? "")
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch news for {Company}", companyName);
            _monitor.RecordApiCall(new ApiCallRecord("GNews", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
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
