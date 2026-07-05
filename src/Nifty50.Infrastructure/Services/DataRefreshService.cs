using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class DataRefreshService : BackgroundService, IDataRefreshService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DataRefreshService> _logger;
    private readonly IConfiguration _config;

    // Nifty50 seed list (Yahoo Finance symbols with .NS suffix).
    // These are the only hardcoded values in the system — just symbol→name mappings
    // that define which stocks belong to the Nifty50 index. All actual data
    // (prices, fundamentals, metadata, sentiment) is fetched from real external APIs.
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

    public DataRefreshService(IServiceProvider services, ILogger<DataRefreshService> logger, IConfiguration config)
    {
        _services = services;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for app to fully start
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // On Render free tier, the instance spins down after ~15 min of inactivity.
            // Every cold start re-runs this loop, so check if a recent refresh already exists
            // to avoid burning Yahoo/GNews API quotas on every wake-up.
            var skipRefresh = false;
            var skipHours = _config.GetValue<double>("DataRefresh:SkipIfRecentHours", 12.0);
            try
            {
                using var checkScope = _services.CreateScope();
                var checkDb = checkScope.ServiceProvider.GetRequiredService<Nifty50.Infrastructure.Data.AppDbContext>();
                var lastRefresh = await checkDb.StockPrices.MaxAsync(p => (DateTime?)p.UpdatedAt, stoppingToken);
                if (lastRefresh.HasValue && DateTime.UtcNow - lastRefresh.Value < TimeSpan.FromHours(skipHours))
                {
                    skipRefresh = true;
                    _logger.LogInformation(
                        "Skipping data refresh — last refresh was {Ago:F1} hours ago (threshold: {Threshold}h).",
                        (DateTime.UtcNow - lastRefresh.Value).TotalHours, skipHours);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check last refresh time, proceeding with refresh.");
            }

            if (!skipRefresh)
            {
                _logger.LogInformation("Starting Nifty50 data refresh...");
                try
                {
                    await RefreshAllDataAsync(stoppingToken);
                    _logger.LogInformation("Data refresh completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error during data refresh.");
                }
            }

            var intervalHours = _config.GetValue<double>("DataRefresh:IntervalHours", 24.0);
            _logger.LogInformation("Next refresh scheduled in {Hours} hours.", intervalHours);
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    public async Task RefreshAllDataAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStockRepository>();
        var priceService = scope.ServiceProvider.GetRequiredService<IStockDataService>();
        var fundDataService = scope.ServiceProvider.GetRequiredService<IFundamentalDataService>();
        var metadataService = scope.ServiceProvider.GetRequiredService<IStockMetadataService>();
        var techService = scope.ServiceProvider.GetRequiredService<ITechnicalAnalysisService>();
        var fundAnalysis = scope.ServiceProvider.GetRequiredService<IFundamentalAnalysisService>();
        var sentimentService = scope.ServiceProvider.GetRequiredService<ISentimentService>();
        var analysisEngine = scope.ServiceProvider.GetRequiredService<IStockAnalysisEngine>();
        var intrinsicValueService = scope.ServiceProvider.GetRequiredService<IIntrinsicValueService>();
        var qualityAssessmentService = scope.ServiceProvider.GetRequiredService<IQualityAssessmentService>();
        var indianApiService = scope.ServiceProvider.GetRequiredService<IIndianMarketDataService>();


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

                // 2. Fetch real-time metadata from IndianAPI (fallback to Yahoo)
                var metadata = await indianApiService.FetchMetadataAsync(symbol);
                if (metadata == null)
                {
                    _logger.LogWarning("IndianAPI metadata failed for {Symbol}, falling back to Yahoo", symbol);
                    metadata = await metadataService.FetchMetadataAsync(symbol);
                }

                if (metadata != null)
                {
                    if (metadata.Sector != null) stock.Sector = metadata.Sector;
                    if (metadata.Industry != null) stock.Industry = metadata.Industry;
                    if (metadata.MarketCap.HasValue) stock.MarketCap = metadata.MarketCap;
                    if (metadata.Week52High.HasValue) stock.Week52High = metadata.Week52High;
                    if (metadata.Week52Low.HasValue) stock.Week52Low = metadata.Week52Low;
                    if (metadata.DayChange.HasValue) stock.DayChange = metadata.DayChange;
                    if (metadata.DayChangePercent.HasValue) stock.DayChangePercent = metadata.DayChangePercent;
                    if (metadata.CurrentPrice.HasValue) stock.CurrentPrice = metadata.CurrentPrice;
                    if (metadata.SharesOutstanding.HasValue) stock.SharesOutstanding = metadata.SharesOutstanding;
                    await repo.UpdateAsync(stock);
                }

                // 3. Fetch prices (incremental from last stored date)
                var lastDate = await repo.GetLastPriceDateAsync(stock.Id);
                var from = lastDate?.AddDays(1) ?? DateTime.UtcNow.AddYears(-8);
                
                var prices = await indianApiService.FetchHistoricalPricesAsync(symbol, from, DateTime.UtcNow);
                if (prices == null || prices.Count == 0)
                {
                    _logger.LogWarning("IndianAPI prices failed for {Symbol}, falling back to Yahoo", symbol);
                    prices = await priceService.FetchHistoricalPricesAsync(symbol, from, DateTime.UtcNow);
                }

                if (prices != null && prices.Count > 0)
                {
                    foreach (var p in prices) p.StockId = stock.Id;
                    await repo.AddPricesAsync(prices);

                    // Update current price from latest close if metadata didn't provide it
                    if (metadata?.CurrentPrice == null)
                    {
                        var lastPrice = prices.OrderByDescending(p => p.Date).First();
                        stock.CurrentPrice = lastPrice.Close;
                        await repo.UpdateAsync(stock);
                    }
                }

                // 4. Fetch dividends (8 year history)
                var divs = await priceService.FetchDividendsAsync(symbol, DateTime.UtcNow.AddYears(-8), DateTime.UtcNow);
                if (divs.Count > 0)
                {
                    foreach (var d in divs) d.StockId = stock.Id;
                    await repo.AddDividendsAsync(divs);
                }

                // 5. Fetch financial statements and TTM fundamentals from IndianAPI (fallback to Yahoo)
                var (statements, baseMetric) = await indianApiService.FetchFundamentalsAsync(symbol);
                if ((statements == null || statements.Count == 0) && baseMetric == null)
                {
                    _logger.LogWarning("IndianAPI fundamentals failed for {Symbol}, falling back to Yahoo", symbol);
                    (statements, baseMetric) = await fundDataService.FetchFundamentalsAsync(symbol);
                }

                // 5b. Fetch Shareholding pattern from IndianAPI
                var shareholding = await indianApiService.FetchShareholdingAsync(symbol);

                if (statements != null && statements.Count > 0)
                {
                    foreach (var s in statements) s.StockId = stock.Id;
                    await repo.AddFinancialStatementsAsync(statements);

                    // 6. Calculate fundamental metrics using real shares outstanding
                    var metric = fundAnalysis.CalculateMetrics(stock.Id, statements, stock.CurrentPrice, stock.SharesOutstanding, baseMetric);
                    await repo.AddFundamentalMetricAsync(metric);

                    // 6a. Intrinsic Valuation
                    if (stock.CurrentPrice != null && stock.CurrentPrice > 0)
                    {
                        var valuation = intrinsicValueService.CalculateIntrinsicValue(stock.Id, stock.CurrentPrice.Value, metric);
                        await repo.AddIntrinsicValuationAsync(valuation);
                    }

                    // 6b. Quality Assessment
                    var existingQuality = await repo.GetLatestQualityAsync(stock.Id) ?? new QualityMetric { StockId = stock.Id };
                    
                    // Populate real shareholding data if available
                    if (shareholding != null)
                    {
                        existingQuality.PromoterHolding = shareholding.PromoterHolding ?? existingQuality.PromoterHolding;
                        existingQuality.FIIHolding = shareholding.FIIHolding ?? existingQuality.FIIHolding;
                        existingQuality.DIIHolding = shareholding.DIIHolding ?? existingQuality.DIIHolding;
                    }

                    var quality = qualityAssessmentService.AssessQuality(stock.Id, statements, metric, stock.Sector, stock.MarketCap, existingQuality);
                    await repo.UpsertQualityMetricAsync(quality);
                }

                // 7. Calculate technical indicators from stored price history (limit to last 250 days to save memory)
                var allPrices = await repo.GetPricesAsync(stock.Id, DateTime.UtcNow.AddDays(-250), null);
                if (allPrices.Count >= 30)
                {
                    var indicators = techService.CalculateIndicators(stock.Id, allPrices);
                    // Save last 5 days of indicators to keep DB size manageable
                    var recentIndicators = indicators.TakeLast(5).ToList();
                    await repo.AddTechnicalIndicatorsAsync(recentIndicators);
                }

                // 8. Analyze sentiment (skip if recent data exists to preserve API quota)
                var existingSentiment = await repo.GetLatestSentimentAsync(stock.Id);
                var sentimentStaleHours = _config.GetValue<double>("DataRefresh:SentimentStaleHours", 12.0);
                if (existingSentiment == null || DateTime.UtcNow - existingSentiment.AnalyzedAt > TimeSpan.FromHours(sentimentStaleHours))
                {
                    var sentiment = await sentimentService.AnalyzeSentimentAsync(name, symbol);
                    sentiment.StockId = stock.Id;
                    await repo.AddSentimentAsync(sentiment);
                }
                else
                {
                    _logger.LogDebug("Skipping sentiment for {Symbol} — last analyzed {Ago:F1}h ago",
                        symbol, (DateTime.UtcNow - existingSentiment.AnalyzedAt).TotalHours);
                }

                // Small delay between stocks to respect Yahoo Finance rate limits
                await Task.Delay(1000, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {Symbol}", symbol);
            }
        }

        // 8b. ScoreHistory Compaction
        try
        {
            _logger.LogInformation("Compacting ScoreHistory...");
            var db = scope.ServiceProvider.GetRequiredService<Nifty50.Infrastructure.Data.AppDbContext>();
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
            var oneYearAgo = DateTime.UtcNow.AddYears(-1);

            // Fetch old records to compact
            var oldRecords = await db.ScoreHistories
                .Where(sh => sh.RecordedAt < ninetyDaysAgo)
                .ToListAsync(ct);

            var toDelete = new List<ScoreHistory>();
            var groupedByStock = oldRecords.GroupBy(sh => sh.StockId);
            foreach (var stockGroup in groupedByStock)
            {
                var between90AndYear = stockGroup.Where(sh => sh.RecordedAt >= oneYearAgo);
                var olderThanYear = stockGroup.Where(sh => sh.RecordedAt < oneYearAgo);
                
                // Keep 1 per week (latest in that week)
                var weeklyGroups = between90AndYear.GroupBy(sh => sh.RecordedAt.Date.AddDays(-(int)sh.RecordedAt.DayOfWeek));
                foreach (var week in weeklyGroups)
                {
                    var keep = week.OrderByDescending(sh => sh.RecordedAt).First();
                    toDelete.AddRange(week.Where(sh => sh.Id != keep.Id));
                }
                
                // Keep 1 per month (latest in that month)
                var monthlyGroups = olderThanYear.GroupBy(sh => new DateTime(sh.RecordedAt.Year, sh.RecordedAt.Month, 1));
                foreach (var month in monthlyGroups)
                {
                    var keep = month.OrderByDescending(sh => sh.RecordedAt).First();
                    toDelete.AddRange(month.Where(sh => sh.Id != keep.Id));
                }
            }

            if (toDelete.Any())
            {
                db.ScoreHistories.RemoveRange(toDelete);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Deleted {Count} compacted ScoreHistory records.", toDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compacting ScoreHistory");
        }

        // 9. Run analysis engine to generate Buy/Sell/Hold signals for all stocks
        try
        {
            _logger.LogInformation("Running analysis engine for all stocks...");
            var newAlerts = await analysisEngine.RecalculateAllAsync();
            
            // 10. Send Push Notifications to mobile devices
            var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
            await pushService.SendAlertNotificationsAsync(newAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running analysis engine or sending notifications");
        }
    }
}
