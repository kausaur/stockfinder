using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class IndianApiService : IIndianMarketDataService
{
    private readonly HttpClient _http;
    private readonly ILogger<IndianApiService> _logger;
    private readonly IApiMonitorService _monitor;
    private readonly string? _apiKey;

    public IndianApiService(HttpClient http, ILogger<IndianApiService> logger, IApiMonitorService monitor, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _monitor = monitor;
        _apiKey = config["IndianApiKey"];
        _http.DefaultRequestHeaders.Add("X-API-KEY", _apiKey ?? "");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<StockMetadataDto?> FetchMetadataAsync(string symbol)
    {
        var url = $"https://indianapi.in/api/v1/stock?name={Uri.EscapeDataString(symbol)}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Meta", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            using var doc = JsonDocument.Parse(json);

            decimal? marketCap = TryGetDecimal(doc.RootElement, "marketCap");
            decimal? currentPrice = TryGetDecimal(doc.RootElement, "currentPrice");
            decimal? dayChange = TryGetDecimal(doc.RootElement, "dayChange");
            decimal? dayChangePercent = TryGetDecimal(doc.RootElement, "dayChangePercent");
            decimal? week52High = TryGetDecimal(doc.RootElement, "week52High");
            decimal? week52Low = TryGetDecimal(doc.RootElement, "week52Low");
            string? sector = TryGetString(doc.RootElement, "sector");
            string? industry = TryGetString(doc.RootElement, "industry");
            long? sharesOutstanding = TryGetLong(doc.RootElement, "sharesOutstanding");

            return new StockMetadataDto(sector, industry, marketCap, week52High, week52Low, dayChange, dayChangePercent, currentPrice, sharesOutstanding);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch metadata from IndianAPI for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Meta", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return null;
        }
    }

    public async Task<(List<FinancialStatement> Statements, FundamentalMetric? Metric)> FetchFundamentalsAsync(string symbol)
    {
        var url = $"https://indianapi.in/api/v1/fundamentals?name={Uri.EscapeDataString(symbol)}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Fund", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            
            // Mocking parsed results for simplicity in this transition
            // Ideally we parse the actual standard response from IndianAPI
            using var doc = JsonDocument.Parse(json);
            
            var statements = new List<FinancialStatement>();
            var metric = new FundamentalMetric
            {
                PeriodEndDate = DateTime.UtcNow,
                ComputedAt = DateTime.UtcNow,
                PERatio = TryGetDecimal(doc.RootElement, "peRatio"),
                PBRatio = TryGetDecimal(doc.RootElement, "pbRatio"),
                ROE = TryGetDecimal(doc.RootElement, "roe"),
                ROIC = TryGetDecimal(doc.RootElement, "roce"),
                DebtToEquity = TryGetDecimal(doc.RootElement, "debtToEquity"),
                DividendYield = TryGetDecimal(doc.RootElement, "dividendYield"),
                EPS = TryGetDecimal(doc.RootElement, "eps")
            };

            return (statements, metric);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch fundamentals from IndianAPI for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Fund", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return (new List<FinancialStatement>(), null);
        }
    }

    public async Task<ShareholdingDto?> FetchShareholdingAsync(string symbol)
    {
        var url = $"https://indianapi.in/api/v1/shareholding?name={Uri.EscapeDataString(symbol)}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Shareholding", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            using var doc = JsonDocument.Parse(json);

            decimal? promoter = TryGetDecimal(doc.RootElement, "promoter");
            decimal? fii = TryGetDecimal(doc.RootElement, "fii");
            decimal? dii = TryGetDecimal(doc.RootElement, "dii");
            decimal? publicHold = TryGetDecimal(doc.RootElement, "public");

            return new ShareholdingDto(promoter, fii, dii, publicHold);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch shareholding from IndianAPI for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Shareholding", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return null;
        }
    }

    public async Task<List<StockPrice>> FetchHistoricalPricesAsync(string symbol, DateTime from, DateTime to)
    {
        // Using Yahoo Finance structure as placeholder since IndianAPI historical might be different
        var url = $"https://indianapi.in/api/v1/historical?name={Uri.EscapeDataString(symbol)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Prices", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            
            // Example stub parsing
            return new List<StockPrice>();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch historical prices from IndianAPI for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("IndianAPI_Prices", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<StockPrice>();
        }
    }

    private static decimal? TryGetDecimal(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetDecimal();
        return null;
    }

    private static long? TryGetLong(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetInt64();
        return null;
    }

    private static string? TryGetString(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
            return val.GetString();
        return null;
    }
}
