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

    public async Task<List<FinancialStatement>> FetchFinancialStatementsAsync(string symbol)
    {
        var modules = "incomeStatementHistory,balanceSheetHistory,cashflowStatementHistory,incomeStatementHistoryQuarterly,balanceSheetHistoryQuarterly,cashflowStatementHistoryQuarterly";
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
            return new List<FinancialStatement>();
        }
    }

    private static List<FinancialStatement> ParseFinancials(string json)
    {
        var results = new List<FinancialStatement>();
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("quoteSummary").GetProperty("result")[0];

        // Income Statement
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
                    EBITDA = TryGetVal(stmt, "ebitda"),
                    CostOfRevenue = TryGetVal(stmt, "costOfRevenue"),
                    TaxProvision = TryGetVal(stmt, "incomeTaxExpense"),
                    // EPS: prefer diluted, fall back to basic — required for P/E ratio calculation
                    DilutedEPS = TryGetVal(stmt, "dilutedEps"),
                    EarningsPerShare = TryGetVal(stmt, "basicEps") ?? TryGetVal(stmt, "dilutedEps"),
                });
            }
        }

        // Balance Sheet
        if (result.TryGetProperty("balanceSheetHistory", out var balHist) && balHist.TryGetProperty("balanceSheetStatements", out var balStmts))
        {
            foreach (var stmt in balStmts.EnumerateArray())
            {
                if (!TryGetDate(stmt, "endDate", out var endDate)) continue;
                results.Add(new FinancialStatement
                {
                    StatementType = StatementType.BalanceSheet,
                    Period = PeriodType.Annual,
                    PeriodEndDate = endDate,
                    TotalAssets = TryGetVal(stmt, "totalAssets"),
                    TotalLiabilities = TryGetVal(stmt, "totalLiab"),
                    TotalEquity = TryGetVal(stmt, "totalStockholderEquity"),
                    CurrentAssets = TryGetVal(stmt, "totalCurrentAssets"),
                    CurrentLiabilities = TryGetVal(stmt, "totalCurrentLiabilities"),
                    CashAndEquivalents = TryGetVal(stmt, "cash"),
                    TotalDebt = TryGetVal(stmt, "shortLongTermDebt") + TryGetVal(stmt, "longTermDebt"),
                    Inventory = TryGetVal(stmt, "inventory"),
                    AccountsReceivable = TryGetVal(stmt, "netReceivables"),
                    AccountsPayable = TryGetVal(stmt, "accountsPayable")
                });
            }
        }

        // Cash Flow
        if (result.TryGetProperty("cashflowStatementHistory", out var cfHist) && cfHist.TryGetProperty("cashflowStatements", out var cfStmts))
        {
            foreach (var stmt in cfStmts.EnumerateArray())
            {
                if (!TryGetDate(stmt, "endDate", out var endDate)) continue;
                results.Add(new FinancialStatement
                {
                    StatementType = StatementType.CashFlow,
                    Period = PeriodType.Annual,
                    PeriodEndDate = endDate,
                    OperatingCashFlow = TryGetVal(stmt, "totalCashFromOperatingActivities"),
                    CapitalExpenditures = TryGetVal(stmt, "capitalExpenditures"),
                    DividendsPaid = TryGetVal(stmt, "dividendsPaid"),
                    ShareRepurchases = TryGetVal(stmt, "repurchaseOfStock"),
                    // FreeCashFlow = OperatingCashFlow + CapEx (Yahoo reports CapEx as negative)
                    FreeCashFlow = TryGetVal(stmt, "totalCashFromOperatingActivities") is decimal ocf
                        && TryGetVal(stmt, "capitalExpenditures") is decimal capex
                        ? ocf + capex
                        : null,
                });
            }
        }

        return results;
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
