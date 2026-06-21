using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var http = new HttpClient(new HttpClientHandler { UseCookies = false });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        
        var cookieRes = await http.GetAsync("https://fc.yahoo.com");
        var cookie = "";
        if (cookieRes.Headers.TryGetValues("Set-Cookie", out var cookies)) {
            foreach (var c in cookies) if (c.Contains("A3=")) { cookie = c.Split(';')[0]; break; }
        }
        
        var req = new HttpRequestMessage(HttpMethod.Get, "https://query1.finance.yahoo.com/v1/test/getcrumb");
        req.Headers.Add("Cookie", cookie);
        var crumbRes = await http.SendAsync(req);
        var crumb = await crumbRes.Content.ReadAsStringAsync();
        
        var url = $"https://query1.finance.yahoo.com/v10/finance/quoteSummary/RELIANCE.NS?modules=incomeStatementHistory,balanceSheetHistory,cashflowStatementHistory&crumb={crumb}";
        var req2 = new HttpRequestMessage(HttpMethod.Get, url);
        req2.Headers.Add("Cookie", cookie);
        var dataRes = await http.SendAsync(req2);
        var json = await dataRes.Content.ReadAsStringAsync();
        
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("quoteSummary").GetProperty("result")[0];
        
        Console.WriteLine("Income:");
        foreach(var stmt in result.GetProperty("incomeStatementHistory").GetProperty("incomeStatementHistory").EnumerateArray()) {
            Console.WriteLine(stmt.GetProperty("endDate").GetProperty("fmt").GetString());
            Console.WriteLine("Rev: " + stmt.GetProperty("totalRevenue").GetProperty("raw").GetDecimal());
        }
        
        Console.WriteLine("Balance:");
        foreach(var stmt in result.GetProperty("balanceSheetHistory").GetProperty("balanceSheetStatements").EnumerateArray()) {
            Console.WriteLine(stmt.GetProperty("endDate").GetProperty("fmt").GetString());
            Console.WriteLine("Assets: " + stmt.GetProperty("totalAssets").GetProperty("raw").GetDecimal());
        }
        
        Console.WriteLine("CashFlow:");
        foreach(var stmt in result.GetProperty("cashflowStatementHistory").GetProperty("cashflowStatements").EnumerateArray()) {
            Console.WriteLine(stmt.GetProperty("endDate").GetProperty("fmt").GetString());
            Console.WriteLine("Operating: " + stmt.GetProperty("totalCashFromOperatingActivities").GetProperty("raw").GetDecimal());
        }
    }
}
