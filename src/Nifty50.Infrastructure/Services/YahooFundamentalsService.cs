using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class YahooFundamentalsService : IFundamentalDataService
{
    private readonly HttpClient _http;
    private readonly ILogger<YahooFundamentalsService> _logger;
    private readonly IApiMonitorService _monitor;
    private readonly IYahooCookieManager _cookieManager;

    public YahooFundamentalsService(HttpClient http, ILogger<YahooFundamentalsService> logger, IApiMonitorService monitor, IYahooCookieManager cookieManager)
    {
        _http = http;
        _logger = logger;
        _monitor = monitor;
        _cookieManager = cookieManager;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<(List<FinancialStatement> Statements, FundamentalMetric? Metric)> FetchFundamentalsAsync(string symbol)
    {
        // Yahoo Finance restricted historical arrays. We now pull TTM snapshots from financialData.
        var modules = "financialData,defaultKeyStatistics,incomeStatementHistory";
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
            _monitor.RecordApiCall(new ApiCallRecord("YahooFundamentals", url, DateTime.UtcNow, 200, sw.ElapsedMilliseconds, null));
            return ParseFinancials(json);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Failed to fetch fundamentals for {Symbol}", symbol);
            _monitor.RecordApiCall(new ApiCallRecord("YahooFundamentals", url, DateTime.UtcNow, 500, sw.ElapsedMilliseconds, ex.Message));
            return (new List<FinancialStatement>(), null);
        }
    }

    private static (List<FinancialStatement> Statements, FundamentalMetric? Metric) ParseFinancials(string json)
    {
        var results = new List<FinancialStatement>();
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("quoteSummary").GetProperty("result")[0];

        var ttmDate = DateTime.UtcNow;

        // Try to get income statements if they exist
        if (result.TryGetProperty("incomeStatementHistory", out var incHist) && incHist.TryGetProperty("incomeStatementHistory", out var incStmts))
        {
            foreach (var stmt in incStmts.EnumerateArray())
            {
                if (!TryGetDate(stmt, "endDate", out var endDate)) continue;
                results.Add(new FinancialStatement
                {
                    StatementType = StatementType.IncomeStatement,
                    Period = PeriodType.Annual,
                    PeriodEndDate = endDate,
                    TotalRevenue = TryGetVal(stmt, "totalRevenue"),
                    GrossProfit = TryGetVal(stmt, "grossProfit"),
                    OperatingIncome = TryGetVal(stmt, "operatingIncome"),
                    NetIncome = TryGetVal(stmt, "netIncomeApplicableToCommonShares") ?? TryGetVal(stmt, "netIncome"),
                    OperatingExpenses = TryGetVal(stmt, "totalOperatingExpenses"),
                    InterestExpense = TryGetVal(stmt, "interestExpense"),
                    CostOfRevenue = TryGetVal(stmt, "costOfRevenue"),
                    TaxProvision = TryGetVal(stmt, "incomeTaxExpense"),
                });
            }
        }

        JsonElement? fd = null;
        JsonElement? dks = null;
        if (result.TryGetProperty("financialData", out var fdVal)) fd = fdVal;
        if (result.TryGetProperty("defaultKeyStatistics", out var dksVal)) dks = dksVal;

        if (fd == null && dks == null) return (results, null);

        // Build a synthetic TTM Financial Statement for balance sheet / cash flow data available in financialData
        var ttmStatement = new FinancialStatement
        {
            StatementType = StatementType.CashFlow, // Acts as our TTM summary
            Period = PeriodType.Annual,
            PeriodEndDate = ttmDate,
            TotalRevenue = fd.HasValue ? TryGetVal(fd.Value, "totalRevenue") : null,
            GrossProfit = fd.HasValue ? TryGetVal(fd.Value, "grossProfits") : null,
            EBITDA = fd.HasValue ? TryGetVal(fd.Value, "ebitda") : null,
            NetIncome = dks.HasValue ? TryGetVal(dks.Value, "netIncomeToCommon") : null,
            TotalDebt = fd.HasValue ? TryGetVal(fd.Value, "totalDebt") : null,
            CashAndEquivalents = fd.HasValue ? TryGetVal(fd.Value, "totalCash") : null,
            OperatingCashFlow = fd.HasValue ? TryGetVal(fd.Value, "operatingCashflow") : null,
            FreeCashFlow = fd.HasValue ? TryGetVal(fd.Value, "freeCashflow") : null,
        };
        results.Add(ttmStatement);

        // Build the FundamentalMetric pre-computed from Yahoo
        var metric = new FundamentalMetric
        {
            ComputedAt = DateTime.UtcNow,
            PeriodEndDate = ttmDate,
            // Ratios straight from Yahoo
            CurrentRatio = fd.HasValue ? TryGetVal(fd.Value, "currentRatio") : null,
            QuickRatio = fd.HasValue ? TryGetVal(fd.Value, "quickRatio") : null,
            DebtToEquity = fd.HasValue ? TryGetVal(fd.Value, "debtToEquity") / 100m : null, // Yahoo returns 36.6 for 36.6%
            ROE = fd.HasValue ? TryGetVal(fd.Value, "returnOnEquity") * 100m : null, // Yahoo returns 0.09 for 9%
            ROA = fd.HasValue ? TryGetVal(fd.Value, "returnOnAssets") * 100m : null,
            GrossProfitMargin = fd.HasValue ? TryGetVal(fd.Value, "grossMargins") * 100m : null,
            OperatingMargin = fd.HasValue ? TryGetVal(fd.Value, "operatingMargins") * 100m : null,
            NetProfitMargin = fd.HasValue ? TryGetVal(fd.Value, "profitMargins") * 100m : null,
            RevenueGrowthYoY = fd.HasValue ? TryGetVal(fd.Value, "revenueGrowth") * 100m : null,
            EarningsGrowthYoY = fd.HasValue ? TryGetVal(fd.Value, "earningsGrowth") * 100m : null,
            
            // Key stats
            EPS = dks.HasValue ? TryGetVal(dks.Value, "trailingEps") : null,
            PERatio = dks.HasValue ? TryGetVal(dks.Value, "forwardPE") : null, // forwardPE is usually better filled
            PEGRatio = dks.HasValue ? TryGetVal(dks.Value, "pegRatio") : null,
            BookValuePerShare = dks.HasValue ? TryGetVal(dks.Value, "bookValue") : null,
            PBRatio = dks.HasValue ? TryGetVal(dks.Value, "priceToBook") : null,
        };

        // Fallback to trailing P/E if forward is missing
        if (!metric.PERatio.HasValue && result.TryGetProperty("summaryDetail", out var sd))
            metric.PERatio = TryGetVal(sd, "trailingPE");

        return (results, metric);
    }

    private static bool TryGetDate(JsonElement elem, string prop, out DateTime date)
    {
        date = default;
        if (elem.TryGetProperty(prop, out var val) && val.TryGetProperty("raw", out var raw) && raw.ValueKind == JsonValueKind.Number)
        {
            date = DateTimeOffset.FromUnixTimeSeconds(raw.GetInt64()).UtcDateTime;
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            return true;
        }
        return false;
    }

    private static decimal? TryGetVal(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) && val.TryGetProperty("raw", out var raw) && raw.ValueKind == JsonValueKind.Number)
        {
            return raw.GetDecimal();
        }
        return null;
    }
}
