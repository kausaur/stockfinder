using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nifty50.Core.DTOs;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

/// <summary>
/// Fetches real-time stock metadata from Yahoo Finance quoteSummary API:
/// sector, industry, market cap, 52-week high/low, day change, and shares outstanding.
/// This ensures no stock metadata fields are ever null due to lack of fetching.
/// </summary>
public class YahooMetadataService : IStockMetadataService
{
    private readonly HttpClient _http;
    private readonly ILogger<YahooMetadataService> _logger;
    private readonly IApiMonitorService _monitor;
    private readonly IYahooCookieManager _cookieManager;

    public YahooMetadataService(HttpClient http, ILogger<YahooMetadataService> logger,
        IApiMonitorService monitor, IYahooCookieManager cookieManager)
    {
        _http = http;
        _logger = logger;
        _monitor = monitor;
        _cookieManager = cookieManager;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<StockMetadataDto?> FetchMetadataAsync(string symbol)
    {
        var modules = "price,summaryProfile,defaultKeyStatistics";
        var (cookie, crumb) = await _cookieManager.GetCookieAndCrumbAsync();
        var crumbQuery = string.IsNullOrEmpty(crumb) ? "" : $"&crumb={crumb}";
        var url = $"https://query1.finance.yahoo.com/v10/finance/quoteSummary/{symbol}?modules={modules}{crumbQuery}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(cookie)) req.Headers.Add("Cookie", cookie);
            var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("YahooMetadata", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            return ParseMetadata(json);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch metadata for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("YahooMetadata", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return null;
        }
    }

    private static StockMetadataDto? ParseMetadata(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.GetProperty("quoteSummary");
            if (root.GetProperty("error").ValueKind != JsonValueKind.Null) return null;
            var result = root.GetProperty("result")[0];

            // --- price module ---
            string? sector = null, industry = null;
            decimal? marketCap = null, week52High = null, week52Low = null;
            decimal? dayChange = null, dayChangePercent = null, currentPrice = null;
            long? sharesOutstanding = null;

            if (result.TryGetProperty("price", out var price))
            {
                currentPrice = TryGetDecimal(price, "regularMarketPrice");
                dayChange = TryGetDecimal(price, "regularMarketChange");
                dayChangePercent = TryGetDecimal(price, "regularMarketChangePercent");
                marketCap = TryGetDecimal(price, "marketCap");
            }

            // --- summaryProfile module ---
            if (result.TryGetProperty("summaryProfile", out var profile))
            {
                sector = TryGetString(profile, "sector");
                industry = TryGetString(profile, "industry");
            }

            // --- defaultKeyStatistics module ---
            if (result.TryGetProperty("defaultKeyStatistics", out var stats))
            {
                week52High = TryGetDecimal(stats, "52WeekChange") == null
                    ? TryGetDecimalRaw(stats, "52WeekChange")
                    : null;
                sharesOutstanding = TryGetLong(stats, "sharesOutstanding");
            }

            // 52-week high/low may also live in summaryDetail
            if (result.TryGetProperty("summaryDetail", out var detail))
            {
                week52High ??= TryGetDecimalRaw(detail, "fiftyTwoWeekHigh");
                week52Low ??= TryGetDecimalRaw(detail, "fiftyTwoWeekLow");
            }

            // Fallback: try price module for 52w
            if (result.TryGetProperty("price", out var price2))
            {
                week52High ??= TryGetDecimalRaw(price2, "fiftyTwoWeekHigh");
                week52Low ??= TryGetDecimalRaw(price2, "fiftyTwoWeekLow");
            }

            return new StockMetadataDto(sector, industry, marketCap, week52High, week52Low,
                dayChange, dayChangePercent, currentPrice, sharesOutstanding);
        }
        catch
        {
            return null;
        }
    }

    // Helpers for Yahoo "raw" value pattern: { "raw": 123.45, "fmt": "123.45" }
    private static decimal? TryGetDecimal(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) &&
            val.TryGetProperty("raw", out var raw) &&
            raw.ValueKind == JsonValueKind.Number)
            return raw.GetDecimal();
        return null;
    }

    private static decimal? TryGetDecimalRaw(JsonElement elem, string prop)
        => TryGetDecimal(elem, prop);

    private static long? TryGetLong(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) &&
            val.TryGetProperty("raw", out var raw) &&
            raw.ValueKind == JsonValueKind.Number)
            return raw.GetInt64();
        return null;
    }

    private static string? TryGetString(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
            return val.GetString();
        return null;
    }
}
