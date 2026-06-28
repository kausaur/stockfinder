using Microsoft.AspNetCore.Mvc;
using Nifty50.Core.DTOs;
using Nifty50.Core.Interfaces;

namespace Nifty50.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockRepository _repo;
    public StocksController(IStockRepository repo) => _repo = repo;

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<List<StockListDto>>> GetAll([FromQuery] string? search, [FromQuery] string? sector)
    {
        var stocksWithAnalysis = await _repo.GetAllWithAnalysisAsync(search, sector);
        var results = new List<StockListDto>();
        foreach (var (s, analysis) in stocksWithAnalysis)
        {
            results.Add(new StockListDto(s.Id, s.Symbol, s.CompanyName, s.Sector, s.CurrentPrice,
                s.DayChangePercent, s.MarketCap, analysis?.OverallSignal.ToString(), analysis?.OverallScore));
        }
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StockDto>> GetById(Guid id)
    {
        var s = await _repo.GetByIdAsync(id);
        if (s == null) return NotFound();
        return Ok(new StockDto(s.Id, s.Symbol, s.CompanyName, s.Sector, s.Industry, s.MarketCap,
            s.CurrentPrice, s.DayChange, s.DayChangePercent, s.Week52High, s.Week52Low, s.IsActive));
    }
}

[ApiController]
[Route("api/stocks/{stockId}")]
public class PricesController : ControllerBase
{
    private readonly IStockRepository _repo;
    public PricesController(IStockRepository repo) => _repo = repo;

    [HttpGet("prices")]
    [ResponseCache(Duration = 300)]
    public async Task<ActionResult<List<StockPriceDto>>> GetPrices(Guid stockId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var prices = await _repo.GetPricesAsync(stockId, from, to);
        return Ok(prices.Select(p => new StockPriceDto(p.Date, p.Open, p.High, p.Low, p.Close, p.AdjClose, p.Volume)));
    }

    [HttpGet("dividends")]
    public async Task<ActionResult<List<DividendDto>>> GetDividends(Guid stockId)
    {
        var divs = await _repo.GetDividendsAsync(stockId);
        return Ok(divs.Select(d => new DividendDto(d.ExDate, d.Amount)));
    }
}

[ApiController]
[Route("api/stocks/{stockId}")]
public class FundamentalsController : ControllerBase
{
    private readonly IStockRepository _repo;
    public FundamentalsController(IStockRepository repo) => _repo = repo;

    [HttpGet("fundamentals")]
    public async Task<ActionResult<FundamentalMetricDto>> GetLatest(Guid stockId)
    {
        var f = await _repo.GetLatestFundamentalAsync(stockId);
        if (f == null) return NotFound();
        return Ok(MapFundamental(f));
    }

    [HttpGet("fundamentals/history")]
    public async Task<ActionResult<List<FundamentalMetricDto>>> GetHistory(Guid stockId)
    {
        var list = await _repo.GetFundamentalHistoryAsync(stockId);
        return Ok(list.Select(MapFundamental));
    }

    [HttpGet("financials")]
    public async Task<ActionResult<List<FinancialStatementDto>>> GetFinancials(Guid stockId, [FromQuery] string? type, [FromQuery] string? period)
    {
        var stmts = await _repo.GetFinancialStatementsAsync(stockId, type, period);
        return Ok(stmts.Select(s => new FinancialStatementDto(
            s.StatementType.ToString(), s.Period.ToString(), s.PeriodEndDate,
            s.TotalAssets, s.TotalLiabilities, s.TotalEquity, s.CurrentAssets, s.CurrentLiabilities,
            s.CashAndEquivalents, s.TotalDebt, s.NetDebt, s.TotalRevenue, s.GrossProfit, s.OperatingIncome,
            s.NetIncome, s.EBITDA, s.EarningsPerShare, s.CostOfRevenue, s.OperatingCashFlow,
            s.CapitalExpenditures, s.FreeCashFlow, s.DividendsPaid)));
    }

    private static FundamentalMetricDto MapFundamental(Nifty50.Core.Entities.FundamentalMetric f) =>
        new(f.PeriodEndDate, f.ComputedAt, f.PERatio, f.PBRatio, f.PSRatio, f.EVToEBITDA,
            f.ROE, f.ROA, f.GrossProfitMargin, f.OperatingMargin, f.NetProfitMargin,
            f.CurrentRatio, f.QuickRatio, f.DebtToEquity, f.DebtToAssets, f.InterestCoverageRatio,
            f.EPS, f.BookValuePerShare, f.DividendYield, f.DividendPayoutRatio,
            f.RevenueGrowthYoY, f.EarningsGrowthYoY, f.FCFGrowthYoY);
}

[ApiController]
[Route("api/stocks/{stockId}")]
public class TechnicalController : ControllerBase
{
    private readonly IStockRepository _repo;
    public TechnicalController(IStockRepository repo) => _repo = repo;

    [HttpGet("technicals")]
    public async Task<ActionResult<TechnicalIndicatorDto>> GetLatest(Guid stockId)
    {
        var t = await _repo.GetLatestTechnicalAsync(stockId);
        if (t == null) return NotFound();
        return Ok(MapTech(t));
    }

    [HttpGet("technicals/history")]
    public async Task<ActionResult<List<TechnicalIndicatorDto>>> GetHistory(Guid stockId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var list = await _repo.GetTechnicalHistoryAsync(stockId, from, to);
        return Ok(list.Select(MapTech));
    }

    private static TechnicalIndicatorDto MapTech(Nifty50.Core.Entities.TechnicalIndicator t) =>
        new(t.Date, t.SMA20, t.SMA50, t.SMA200, t.EMA12, t.EMA26, t.RSI14,
            t.MACD, t.MACDSignal, t.MACDHistogram, t.BollingerUpper, t.BollingerMiddle, t.BollingerLower,
            t.ATR14, t.ADX14, t.StochK, t.StochD, t.OBV, t.VWAP);
}

[ApiController]
[Route("api/stocks/{stockId}")]
public class SentimentController : ControllerBase
{
    private readonly IStockRepository _repo;
    public SentimentController(IStockRepository repo) => _repo = repo;

    [HttpGet("sentiment")]
    public async Task<ActionResult<SentimentDto>> GetLatest(Guid stockId)
    {
        var s = await _repo.GetLatestSentimentAsync(stockId);
        if (s == null) return NotFound();
        var headlines = string.IsNullOrEmpty(s.TopHeadlines) ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.TopHeadlines);
        return Ok(new SentimentDto(s.AnalyzedAt, s.OverallSentiment.ToString(), s.SentimentScore, s.PositiveCount, s.NegativeCount, s.NeutralCount, headlines));
    }
}

[ApiController]
[Route("api")]
public class AnalysisController : ControllerBase
{
    private readonly IStockRepository _repo;
    private readonly IStockAnalysisEngine _engine;
    public AnalysisController(IStockRepository repo, IStockAnalysisEngine engine) { _repo = repo; _engine = engine; }

    [HttpGet("stocks/{stockId}/analysis")]
    public async Task<ActionResult<AnalysisDto>> GetAnalysis(Guid stockId)
    {
        var a = await _repo.GetLatestAnalysisAsync(stockId);
        if (a == null) return NotFound();
        var stock = await _repo.GetByIdAsync(stockId);
        return Ok(new AnalysisDto(a.StockId, stock?.Symbol ?? "", stock?.CompanyName ?? "", a.AnalyzedAt,
            a.TechnicalSignal.ToString(), a.FundamentalSignal.ToString(), a.SentimentSignal.ToString(), a.OverallSignal.ToString(),
            a.TechnicalScore, a.FundamentalScore, a.SentimentScore, a.DividendScore, a.OverallScore,
            a.Reasoning, a.IsAlert, a.AlertMessage, a.ScoringProfile?.Name));
    }

    [HttpGet("alerts")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<List<AnalysisDto>>> GetAlerts()
    {
        var alerts = await _repo.GetAlertsAsync();
        return Ok(alerts.Select(a => new AnalysisDto(a.StockId, a.Stock?.Symbol ?? "", a.Stock?.CompanyName ?? "", a.AnalyzedAt,
            a.TechnicalSignal.ToString(), a.FundamentalSignal.ToString(), a.SentimentSignal.ToString(), a.OverallSignal.ToString(),
            a.TechnicalScore, a.FundamentalScore, a.SentimentScore, a.DividendScore, a.OverallScore,
            a.Reasoning, a.IsAlert, a.AlertMessage, a.ScoringProfile?.Name)));
    }

    [HttpPost("analysis/recalculate")]
    public async Task<ActionResult> Recalculate() { await _engine.RecalculateAllAsync(); return Ok(new { message = "Recalculation complete" }); }
}

[ApiController]
[Route("api/scoring-profiles")]
public class ScoringProfileController : ControllerBase
{
    private readonly IScoringProfileService _service;
    private readonly IStockAnalysisEngine _engine;
    public ScoringProfileController(IScoringProfileService service, IStockAnalysisEngine engine) { _service = service; _engine = engine; }

    [HttpGet]
    [ResponseCache(Duration = 300)]
    public async Task<ActionResult<List<ScoringProfileDto>>> GetAll()
    {
        var profiles = await _service.GetAllProfilesAsync();
        return Ok(profiles.Select(MapProfile));
    }

    [HttpGet("active")]
    public async Task<ActionResult<ScoringProfileDto>> GetActive()
    {
        var p = await _service.GetActiveProfileAsync();
        return Ok(MapProfile(p));
    }

    [HttpPut("active")]
    public async Task<ActionResult> UpdateActive([FromBody] ScoringProfileUpdateDto dto)
    {
        if (dto.TechnicalWeight + dto.FundamentalWeight + dto.SentimentWeight + dto.DividendWeight + dto.ValuationWeight + dto.QualityWeight != 100)
            return BadRequest("Weights must sum to 100");
        await _service.UpdateActiveProfileAsync(dto);
        return Ok(new { message = "Profile updated" });
    }

    [HttpPost]
    public async Task<ActionResult<ScoringProfileDto>> Create([FromBody] ScoringProfileDto dto)
    {
        if (dto.TechnicalWeight + dto.FundamentalWeight + dto.SentimentWeight + dto.DividendWeight + dto.ValuationWeight + dto.QualityWeight != 100)
            return BadRequest("Weights must sum to 100");
        var profile = await _service.CreateProfileAsync(dto);
        return Ok(MapProfile(profile));
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult> Activate(Guid id) { await _service.ActivateProfileAsync(id); return Ok(new { message = "Profile activated" }); }

    [HttpPost("reset")]
    public async Task<ActionResult> Reset() { await _service.ResetToDefaultAsync(); return Ok(new { message = "Reset to Balanced" }); }

    private static ScoringProfileDto MapProfile(Nifty50.Core.Entities.ScoringProfile p) =>
        new(p.Id, p.Name, p.IsDefault, p.IsPreset, p.TechnicalWeight, p.FundamentalWeight, p.SentimentWeight, p.DividendWeight, p.ValuationWeight, p.QualityWeight,
            p.TechRSIWeight, p.TechMACDWeight, p.TechMovingAvgWeight, p.TechBollingerWeight, p.TechADXWeight, p.TechVolumeWeight,
            p.FundValuationWeight, p.FundProfitabilityWeight, p.FundLiquidityWeight, p.FundLeverageWeight, p.FundGrowthWeight, p.FundROCEWeight, p.FundPEGWeight,
            p.QualPiotroskiWeight, p.QualAltmanWeight, p.QualPromoterWeight, p.QualFIIWeight, p.QualDividendConsistencyWeight, p.QualFCFTrendWeight,
            p.AlertMinOverallScore, p.AlertMinTechnicalScore, p.AlertMinFundamentalScore, p.AlertMinSentimentScore,
            p.StrongBuyThreshold, p.BuyThreshold, p.HoldThreshold, p.SellThreshold);
}

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IStockRepository _repo;
    public DashboardController(IStockRepository repo) => _repo = repo;

    [HttpGet]
    [ResponseCache(Duration = 120)]
    public async Task<ActionResult<DashboardDto>> Get() => Ok(await _repo.GetDashboardDataAsync());
}

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IApiMonitorService _monitor;
    private readonly IDataRefreshService _refresh;
    public AdminController(IApiMonitorService monitor, IDataRefreshService refresh) { _monitor = monitor; _refresh = refresh; }

    [HttpGet("health")]
    public async Task<ActionResult<AdminDashboardDto>> GetHealth() => Ok(await _monitor.GetAdminDashboardAsync());

    [HttpGet("api-calls")]
    public ActionResult<List<ApiCallRecord>> GetApiCalls([FromQuery] string? api, [FromQuery] int limit = 50) =>
        Ok(_monitor.GetRecentCalls(api, limit));

    [HttpPost("/api/refresh")]
    public async Task<ActionResult> Refresh()
    {
        _ = Task.Run(() => _refresh.RefreshAllDataAsync());
        return Ok(new { message = "Refresh started in background" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _service;
    
    public RecommendationsController(IRecommendationService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<RecommendationDashboardDto>> GetDashboard()
    {
        return Ok(await _service.GetDashboardAsync());
    }

    [HttpPost("screener")]
    public async Task<ActionResult<List<StockRecommendationDto>>> Screen([FromBody] ScreenerFilters filters)
    {
        return Ok(await _service.ScreenStocksAsync(filters));
    }

    [HttpGet("{stockId}/peers")]
    public async Task<ActionResult<List<PeerComparisonDto>>> GetPeers(Guid stockId)
    {
        return Ok(await _service.GetPeersAsync(stockId));
    }
}
