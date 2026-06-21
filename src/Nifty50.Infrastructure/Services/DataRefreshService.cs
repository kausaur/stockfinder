using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class DataRefreshService : BackgroundService, IDataRefreshService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DataRefreshService> _logger;

    // Nifty50 seed list (Yahoo Finance symbols with .NS suffix)
    private static readonly Dictionary<string, string> Nifty50Seeds = new()
    {
        ["RELIANCE.NS"] = "Reliance Industries", ["TCS.NS"] = "Tata Consultancy Services",
        ["HDFCBANK.NS"] = "HDFC Bank", ["INFY.NS"] = "Infosys", ["ICICIBANK.NS"] = "ICICI Bank",
        ["HINDUNILVR.NS"] = "Hindustan Unilever", ["SBIN.NS"] = "State Bank of India",
        ["BHARTIARTL.NS"] = "Bharti Airtel", ["ITC.NS"] = "ITC", ["KOTAKBANK.NS"] = "Kotak Mahindra Bank",
        ["LT.NS"] = "Larsen & Toubro", ["AXISBANK.NS"] = "Axis Bank", ["ASIANPAINT.NS"] = "Asian Paints",
        ["MARUTI.NS"] = "Maruti Suzuki", ["SUNPHARMA.NS"] = "Sun Pharma", ["TITAN.NS"] = "Titan Company",
        ["BAJFINANCE.NS"] = "Bajaj Finance", ["DMART.NS"] = "Avenue Supermarts",
        ["ULTRACEMCO.NS"] = "UltraTech Cement", ["WIPRO.NS"] = "Wipro",
        ["NESTLEIND.NS"] = "Nestle India", ["NTPC.NS"] = "NTPC", ["TATAMOTORS.NS"] = "Tata Motors",
        ["M&M.NS"] = "Mahindra & Mahindra", ["POWERGRID.NS"] = "Power Grid Corp",
        ["ONGC.NS"] = "Oil & Natural Gas Corp", ["JSWSTEEL.NS"] = "JSW Steel",
        ["TATASTEEL.NS"] = "Tata Steel", ["ADANIENT.NS"] = "Adani Enterprises",
        ["ADANIPORTS.NS"] = "Adani Ports", ["COALINDIA.NS"] = "Coal India",
        ["HCLTECH.NS"] = "HCL Technologies", ["BAJAJFINSV.NS"] = "Bajaj Finserv",
        ["TECHM.NS"] = "Tech Mahindra", ["INDUSINDBK.NS"] = "IndusInd Bank",
        ["HINDALCO.NS"] = "Hindalco Industries", ["DRREDDY.NS"] = "Dr. Reddy's Laboratories",
        ["DIVISLAB.NS"] = "Divi's Laboratories", ["CIPLA.NS"] = "Cipla",
        ["GRASIM.NS"] = "Grasim Industries", ["BRITANNIA.NS"] = "Britannia Industries",
        ["APOLLOHOSP.NS"] = "Apollo Hospitals", ["EICHERMOT.NS"] = "Eicher Motors",
        ["HEROMOTOCO.NS"] = "Hero MotoCorp", ["BPCL.NS"] = "Bharat Petroleum",
        ["TATACONSUM.NS"] = "Tata Consumer Products", ["SBILIFE.NS"] = "SBI Life Insurance",
        ["BAJAJ-AUTO.NS"] = "Bajaj Auto", ["BEL.NS"] = "Bharat Electronics",
        ["HDFCLIFE.NS"] = "HDFC Life Insurance", ["SHRIRAMFIN.NS"] = "Shriram Finance",
    };

    public DataRefreshService(IServiceProvider services, ILogger<DataRefreshService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for app to fully start
        await Task.Delay(3000, stoppingToken);
        _logger.LogInformation("Starting Nifty50 data refresh...");
        await RefreshAllDataAsync(stoppingToken);
        _logger.LogInformation("Data refresh completed.");
    }

    public async Task RefreshAllDataAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStockRepository>();
        var priceService = scope.ServiceProvider.GetRequiredService<IStockDataService>();
        var fundDataService = scope.ServiceProvider.GetRequiredService<IFundamentalDataService>();
        var techService = scope.ServiceProvider.GetRequiredService<ITechnicalAnalysisService>();
        var fundAnalysis = scope.ServiceProvider.GetRequiredService<IFundamentalAnalysisService>();
        var sentimentService = scope.ServiceProvider.GetRequiredService<ISentimentService>();
        var analysisEngine = scope.ServiceProvider.GetRequiredService<IStockAnalysisEngine>();

        int count = 0;
        foreach (var (symbol, name) in Nifty50Seeds)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                count++;
                _logger.LogInformation("[{Count}/50] Processing {Symbol} ({Name})...", count, symbol, name);

                // 1. Ensure stock exists
                var stock = await repo.GetBySymbolAsync(symbol);
                if (stock == null)
                {
                    stock = new Stock { Symbol = symbol, CompanyName = name, IsActive = true };
                    await repo.AddAsync(stock);
                }

                // 2. Fetch prices (incremental)
                var lastDate = await repo.GetLastPriceDateAsync(stock.Id);
                var from = lastDate?.AddDays(1) ?? DateTime.UtcNow.AddYears(-8);
                var prices = await priceService.FetchHistoricalPricesAsync(symbol, from, DateTime.UtcNow);
                if (prices.Count > 0)
                {
                    foreach (var p in prices) p.StockId = stock.Id;
                    await repo.AddPricesAsync(prices);
                    var lastPrice = prices.OrderByDescending(p => p.Date).First();
                    stock.CurrentPrice = lastPrice.Close;
                    await repo.UpdateAsync(stock);
                }

                // 3. Fetch dividends
                var divs = await priceService.FetchDividendsAsync(symbol, DateTime.UtcNow.AddYears(-8), DateTime.UtcNow);
                if (divs.Count > 0)
                {
                    foreach (var d in divs) d.StockId = stock.Id;
                    await repo.AddDividendsAsync(divs);
                }

                // 4. Fetch financial statements
                var statements = await fundDataService.FetchFinancialStatementsAsync(symbol);
                if (statements.Count > 0)
                {
                    foreach (var s in statements) s.StockId = stock.Id;
                    await repo.AddFinancialStatementsAsync(statements);

                    // 5. Calculate fundamental metrics
                    var metric = fundAnalysis.CalculateMetrics(stock.Id, statements, stock.CurrentPrice);
                    await repo.AddFundamentalMetricAsync(metric);
                }

                // 6. Calculate technical indicators
                var allPrices = await repo.GetPricesAsync(stock.Id, null, null);
                if (allPrices.Count >= 30)
                {
                    var indicators = techService.CalculateIndicators(stock.Id, allPrices);
                    // Only save last 30 days of indicators to reduce DB size
                    var recentIndicators = indicators.TakeLast(30).ToList();
                    await repo.AddTechnicalIndicatorsAsync(recentIndicators);
                }

                // 7. Analyze sentiment (limit to avoid API rate limits)
                if (count <= 20) // GNews free tier: 100 req/day
                {
                    var sentiment = await sentimentService.AnalyzeSentimentAsync(name, symbol);
                    sentiment.StockId = stock.Id;
                    await repo.AddSentimentAsync(sentiment);
                }

                // Small delay between stocks to avoid rate limiting
                await Task.Delay(500, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {Symbol}", symbol);
            }
        }

        // 8. Run analysis engine for all stocks
        try
        {
            _logger.LogInformation("Running analysis engine for all stocks...");
            await analysisEngine.RecalculateAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running analysis engine");
        }
    }
}
