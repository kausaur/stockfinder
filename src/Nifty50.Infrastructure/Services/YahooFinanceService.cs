using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class YahooFinanceService : IStockDataService
{
    private readonly HttpClient _http;
    private readonly ILogger<YahooFinanceService> _logger;
    private readonly IApiMonitorService _monitor;

    public YahooFinanceService(HttpClient http, ILogger<YahooFinanceService> logger, IApiMonitorService monitor)
    {
        _http = http;
        _logger = logger;
        _monitor = monitor;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<List<StockPrice>> FetchHistoricalPricesAsync(string symbol, DateTime from, DateTime to)
    {
        if (from >= to) return new List<StockPrice>();

        var p1 = new DateTimeOffset(from).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to).ToUnixTimeSeconds();
        var url = $"https://query2.finance.yahoo.com/v8/finance/chart/{symbol}?period1={p1}&period2={p2}&interval=1d";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("YahooFinance_Chart", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            return ParseChartPrices(json);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch prices for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("YahooFinance_Chart", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<StockPrice>();
        }
    }

    public async Task<List<Dividend>> FetchDividendsAsync(string symbol, DateTime from, DateTime to)
    {
        if (from >= to) return new List<Dividend>();

        var p1 = new DateTimeOffset(from).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to).ToUnixTimeSeconds();
        var url = $"https://query2.finance.yahoo.com/v8/finance/chart/{symbol}?period1={p1}&period2={p2}&interval=1d&events=div";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var json = await _http.GetStringAsync(url);
            sw.Stop();
            _monitor.RecordApiCall(new ApiCallRecord("YahooFinance_Divs", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            return ParseChartDividends(json);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch dividends for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("YahooFinance_Divs", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return new List<Dividend>();
        }
    }

    private static List<StockPrice> ParseChartPrices(string json)
    {
        var prices = new List<StockPrice>();
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
        if (!result.TryGetProperty("timestamp", out var timestamps)) return prices;

        var quote = result.GetProperty("indicators").GetProperty("quote")[0];
        var adjclose = result.GetProperty("indicators").TryGetProperty("adjclose", out var ac) ? ac[0].GetProperty("adjclose") : quote.GetProperty("close");

        var opens = quote.GetProperty("open");
        var highs = quote.GetProperty("high");
        var lows = quote.GetProperty("low");
        var closes = quote.GetProperty("close");
        var volumes = quote.GetProperty("volume");

        for (int i = 0; i < timestamps.GetArrayLength(); i++)
        {
            if (closes[i].ValueKind == JsonValueKind.Null) continue; // Skip null entries

            var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime;
            prices.Add(new StockPrice
            {
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Open = opens[i].GetDecimal(),
                High = highs[i].GetDecimal(),
                Low = lows[i].GetDecimal(),
                Close = closes[i].GetDecimal(),
                AdjClose = adjclose[i].GetDecimal(),
                Volume = volumes[i].GetInt64()
            });
        }
        return prices;
    }

    private static List<Dividend> ParseChartDividends(string json)
    {
        var divs = new List<Dividend>();
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
        
        if (result.TryGetProperty("events", out var events) && events.TryGetProperty("dividends", out var dividends))
        {
            foreach (var prop in dividends.EnumerateObject())
            {
                var divObj = prop.Value;
                var date = DateTimeOffset.FromUnixTimeSeconds(divObj.GetProperty("date").GetInt64()).UtcDateTime;
                divs.Add(new Dividend
                {
                    ExDate = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                    Amount = divObj.GetProperty("amount").GetDecimal()
                });
            }
        }
        return divs;
    }
}
