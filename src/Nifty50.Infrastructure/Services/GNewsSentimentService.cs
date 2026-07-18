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

    // Keyword lists and ScoreHeadline moved to SentimentScoringHelper.cs

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
        var headlines = new List<string>();
        string source = "None";
        var cleanSymbol = symbol.Replace(".NS", "").Replace(".BO", "");

        // 1. Try GNews first if API key is configured (already targeted, no filtering needed)
        if (!string.IsNullOrEmpty(_apiKey) && _apiKey != "YOUR_GNEWS_API_KEY")
        {
            headlines = await FetchGNewsHeadlinesAsync(companyName, symbol);
            source = "GNews";
        }

        // 2. Fall back to Google News RSS (free, no API key, stock-specific)
        if (headlines.Count == 0)
        {
            headlines = await FetchGoogleNewsHeadlinesAsync(companyName, cleanSymbol);
            source = headlines.Count > 0 ? "GoogleNews" : "None";
        }

        // 3. Fall back to Yahoo Finance search
        if (headlines.Count == 0)
        {
            headlines = await FetchYahooHeadlinesAsync(companyName, cleanSymbol);
            source = headlines.Count > 0 ? "Yahoo" : "None";
        }

        // Filter out irrelevant headlines for non-GNews sources (GNews is already targeted)
        if (source != "GNews" && headlines.Count > 0)
        {
            headlines = FilterRelevantHeadlines(headlines, companyName, cleanSymbol);
        }

        _logger.LogInformation("Sentiment for {Symbol}: {Count} relevant headlines from {Source}", symbol, headlines.Count, source);

        int pos = 0, neg = 0, neut = 0;
        decimal totalScore = 0;

        foreach (var headline in headlines)
        {
            var score = SentimentScoringHelper.ScoreHeadline(headline);
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
    private async Task<List<string>> FetchGNewsHeadlinesAsync(string companyName, string symbol)
    {
        if (string.IsNullOrEmpty(_apiKey)) return new List<string>();

        var cleanSymbol = symbol.Replace(".NS", "").Replace(".BO", "");
        var query = Uri.EscapeDataString($"{companyName} {cleanSymbol} stock India NSE");
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
            _logger.LogWarning(ex, "GNews failed for {Company}, will fall back", companyName);
            _monitor.RecordApiCall(new ApiCallRecord("GNews", url.Replace(_apiKey!, "***"), DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<string>();
        }
    }

    /// <summary>Fetches headlines from Google News RSS — free, no API key needed, returns targeted results</summary>
    private async Task<List<string>> FetchGoogleNewsHeadlinesAsync(string companyName, string cleanSymbol)
    {
        // Build a focused search query: "TCS stock" or "Reliance Industries stock"
        var shortName = companyName.Split(new[] { " Ltd", " Limited" }, StringSplitOptions.None)[0].Trim();
        var query = Uri.EscapeDataString($"{shortName} {cleanSymbol} stock NSE");
        var url = $"https://news.google.com/rss/search?q={query}&hl=en-IN&gl=IN&ceid=IN:en";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var xml = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("GoogleNewsRSS", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));

            var headlines = new List<string>();
            // Parse RSS XML — extract <title> elements from <item> entries
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            var items = doc.Descendants("item");
            foreach (var item in items.Take(10))
            {
                var title = item.Element("title")?.Value;
                if (!string.IsNullOrWhiteSpace(title))
                    headlines.Add(title);
            }
            return headlines;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Google News RSS failed for {Symbol}", cleanSymbol);
            _monitor.RecordApiCall(new ApiCallRecord("GoogleNewsRSS", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<string>();
        }
    }

    /// <summary>Fallback: fetches headlines from Yahoo Finance search using company name + symbol</summary>
    private async Task<List<string>> FetchYahooHeadlinesAsync(string companyName, string cleanSymbol)
    {
        // Search by company name + symbol for better results than symbol alone
        var shortName = companyName.Split(new[] { " Ltd", " Limited" }, StringSplitOptions.None)[0].Trim();
        var query = Uri.EscapeDataString($"{shortName} {cleanSymbol}");
        var url = $"https://query2.finance.yahoo.com/v1/finance/search?q={query}&newsCount=10";
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
            _logger.LogWarning(ex, "Yahoo news fallback also failed for {Symbol}", cleanSymbol);
            _monitor.RecordApiCall(new ApiCallRecord("YahooNews", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<string>();
        }
    }

    /// <summary>Filters headlines to only include those relevant to the company</summary>
    private static List<string> FilterRelevantHeadlines(List<string> headlines, string companyName, string cleanSymbol)
    {
        if (headlines.Count == 0) return headlines;

        // Build a set of keywords to match against
        var keywords = new List<string> { cleanSymbol.ToLowerInvariant() };

        // Split company name into significant words (skip short/common words)
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ltd", "limited", "corp", "corporation", "inc", "industries", "company",
            "the", "of", "and", "&", "co", "pvt", "private"
        };
        foreach (var word in companyName.Split(new[] { ' ', '.', ',', '-', '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length >= 3 && !stopWords.Contains(word))
                keywords.Add(word.ToLowerInvariant());
        }

        var relevant = headlines.Where(h =>
        {
            var lower = h.ToLowerInvariant();
            return keywords.Any(k => lower.Contains(k));
        }).ToList();

        return relevant;
    }

    // ScoreHeadline method moved to SentimentScoringHelper.cs
}
