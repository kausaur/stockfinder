using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        try {
            var json = await http.GetStringAsync("https://query2.finance.yahoo.com/v1/finance/search?q=RELIANCE.NS&newsCount=10");
            Console.WriteLine(json.Substring(0, Math.Min(1000, json.Length)));
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }
}
