using Microsoft.EntityFrameworkCore;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Data;

namespace Nifty50.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly AppDbContext _db;
    public StockRepository(AppDbContext db) => _db = db;

    public async Task<List<Stock>> GetAllAsync(string? search = null, string? sector = null)
    {
        var q = _db.Stocks.Where(s => s.IsActive).AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(s => s.Symbol.Contains(search) || s.CompanyName.Contains(search));
        if (!string.IsNullOrEmpty(sector))
            q = q.Where(s => s.Sector == sector);
        return await q.OrderBy(s => s.Symbol).ToListAsync();
    }

    public async Task<Stock?> GetByIdAsync(Guid id) =>
        await _db.Stocks.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Stock?> GetBySymbolAsync(string symbol) =>
        await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol);

    public async Task<Stock> AddAsync(Stock stock) { _db.Stocks.Add(stock); await _db.SaveChangesAsync(); return stock; }

    public async Task UpdateAsync(Stock stock) { _db.Stocks.Update(stock); await _db.SaveChangesAsync(); }

    public async Task SoftDeleteAsync(Guid id)
    {
        var stock = await _db.Stocks.FindAsync(id);
        if (stock != null) { stock.IsActive = false; stock.IsDeleted = true; stock.DeletedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
    }

    public async Task<List<StockPrice>> GetPricesAsync(Guid stockId, DateTime? from, DateTime? to)
    {
        var q = _db.StockPrices.Where(p => p.StockId == stockId).AsNoTracking();
        if (from.HasValue) q = q.Where(p => p.Date >= from.Value);
        if (to.HasValue) q = q.Where(p => p.Date <= to.Value);
        return await q.OrderBy(p => p.Date).ToListAsync();
    }

    public async Task<DateTime?> GetLastPriceDateAsync(Guid stockId) =>
        await _db.StockPrices.Where(p => p.StockId == stockId).MaxAsync(p => (DateTime?)p.Date);

    public async Task AddPricesAsync(IEnumerable<StockPrice> prices)
    {
        foreach (var p in prices)
        {
            var existing = await _db.StockPrices.FirstOrDefaultAsync(x => x.StockId == p.StockId && x.Date == p.Date);
            if (existing != null) 
            {
                existing.Open = p.Open; existing.High = p.High; existing.Low = p.Low; 
                existing.Close = p.Close; existing.AdjClose = p.AdjClose; existing.Volume = p.Volume;
                _db.StockPrices.Update(existing);
            }
            else _db.StockPrices.Add(p);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<Dividend>> GetDividendsAsync(Guid stockId) =>
        await _db.Dividends.Where(d => d.StockId == stockId).OrderByDescending(d => d.ExDate).AsNoTracking().ToListAsync();

    public async Task AddDividendsAsync(IEnumerable<Dividend> dividends)
    {
        foreach (var d in dividends)
        {
            var existing = await _db.Dividends.FirstOrDefaultAsync(x => x.StockId == d.StockId && x.ExDate == d.ExDate);
            if (existing != null) 
            {
                existing.Amount = d.Amount;
                _db.Dividends.Update(existing);
            }
            else _db.Dividends.Add(d);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<FinancialStatement>> GetFinancialStatementsAsync(Guid stockId, string? type = null, string? period = null)
    {
        var q = _db.FinancialStatements.Where(f => f.StockId == stockId).AsNoTracking();
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<Nifty50.Core.Enums.StatementType>(type, true, out var st))
            q = q.Where(f => f.StatementType == st);
        if (!string.IsNullOrEmpty(period) && Enum.TryParse<Nifty50.Core.Enums.PeriodType>(period, true, out var pt))
            q = q.Where(f => f.Period == pt);
        return await q.OrderByDescending(f => f.PeriodEndDate).ToListAsync();
    }

    public async Task AddFinancialStatementsAsync(IEnumerable<FinancialStatement> statements)
    {
        foreach (var stmt in statements)
        {
            var existing = await _db.FinancialStatements.FirstOrDefaultAsync(f => 
                f.StockId == stmt.StockId && 
                f.StatementType == stmt.StatementType && 
                f.Period == stmt.Period && 
                f.PeriodEndDate == stmt.PeriodEndDate);
            if (existing != null)
            {
                stmt.Id = existing.Id;
                _db.Entry(existing).CurrentValues.SetValues(stmt);
                _db.Entry(existing).Property(x => x.Id).IsModified = false;
            }
            else
            {
                _db.FinancialStatements.Add(stmt);
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task<FundamentalMetric?> GetLatestFundamentalAsync(Guid stockId) =>
        await _db.FundamentalMetrics.Where(f => f.StockId == stockId).OrderByDescending(f => f.PeriodEndDate).AsNoTracking().FirstOrDefaultAsync();

    public async Task<List<FundamentalMetric>> GetFundamentalHistoryAsync(Guid stockId) =>
        await _db.FundamentalMetrics.Where(f => f.StockId == stockId).OrderByDescending(f => f.PeriodEndDate).AsNoTracking().ToListAsync();

    public async Task AddFundamentalMetricAsync(FundamentalMetric metric)
    {
        var existing = await _db.FundamentalMetrics.FirstOrDefaultAsync(x => x.StockId == metric.StockId && x.PeriodEndDate == metric.PeriodEndDate);
        if (existing != null) 
        {
            metric.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(metric);
            _db.Entry(existing).Property(x => x.Id).IsModified = false;
        }
        else _db.FundamentalMetrics.Add(metric);
        await _db.SaveChangesAsync();
    }

    public async Task<TechnicalIndicator?> GetLatestTechnicalAsync(Guid stockId) =>
        await _db.TechnicalIndicators.Where(t => t.StockId == stockId).OrderByDescending(t => t.Date).AsNoTracking().FirstOrDefaultAsync();

    public async Task<List<TechnicalIndicator>> GetTechnicalHistoryAsync(Guid stockId, DateTime? from, DateTime? to)
    {
        var q = _db.TechnicalIndicators.Where(t => t.StockId == stockId).AsNoTracking();
        if (from.HasValue) q = q.Where(t => t.Date >= from.Value);
        if (to.HasValue) q = q.Where(t => t.Date <= to.Value);
        return await q.OrderBy(t => t.Date).ToListAsync();
    }

    public async Task AddTechnicalIndicatorsAsync(IEnumerable<TechnicalIndicator> indicators)
    {
        foreach (var i in indicators)
        {
            var existing = await _db.TechnicalIndicators.FirstOrDefaultAsync(x => x.StockId == i.StockId && x.Date == i.Date);
            if (existing != null) 
            {
                i.Id = existing.Id;
                _db.Entry(existing).CurrentValues.SetValues(i);
                _db.Entry(existing).Property(x => x.Id).IsModified = false;
            }
            else _db.TechnicalIndicators.Add(i);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<SentimentAnalysis?> GetLatestSentimentAsync(Guid stockId) =>
        await _db.SentimentAnalyses.Where(s => s.StockId == stockId).OrderByDescending(s => s.AnalyzedAt).AsNoTracking().FirstOrDefaultAsync();

    public async Task AddSentimentAsync(SentimentAnalysis sentiment)
    {
        var existing = await _db.SentimentAnalyses.FirstOrDefaultAsync(x => x.StockId == sentiment.StockId && x.AnalyzedAt.Date == sentiment.AnalyzedAt.Date);
        if (existing != null) 
        {
            sentiment.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(sentiment);
            _db.Entry(existing).Property(x => x.Id).IsModified = false;
        }
        else _db.SentimentAnalyses.Add(sentiment);
        await _db.SaveChangesAsync();
    }

    public async Task<StockAnalysis?> GetLatestAnalysisAsync(Guid stockId) =>
        await _db.StockAnalyses.Include(a => a.ScoringProfile).Where(a => a.StockId == stockId).OrderByDescending(a => a.AnalyzedAt).AsNoTracking().FirstOrDefaultAsync();

    public async Task<List<StockAnalysis>> GetAlertsAsync() =>
        await _db.StockAnalyses.Include(a => a.Stock).Include(a => a.ScoringProfile)
            .Where(a => a.IsAlert).OrderByDescending(a => a.OverallScore).AsNoTracking().ToListAsync();

    public async Task AddAnalysisAsync(StockAnalysis analysis) { _db.StockAnalyses.Add(analysis); await _db.SaveChangesAsync(); }

    public async Task ClearAnalysesAsync() => await _db.StockAnalyses.ExecuteDeleteAsync();

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        var stocks = await _db.Stocks.Where(s => s.IsActive).AsNoTracking().ToListAsync();
        var topGainers = stocks.Where(s => s.DayChangePercent.HasValue).OrderByDescending(s => s.DayChangePercent).Take(5)
            .Select(s => new StockListDto(s.Id, s.Symbol, s.CompanyName, s.Sector, s.CurrentPrice, s.DayChangePercent, s.MarketCap, null, null)).ToList();
        var topLosers = stocks.Where(s => s.DayChangePercent.HasValue).OrderBy(s => s.DayChangePercent).Take(5)
            .Select(s => new StockListDto(s.Id, s.Symbol, s.CompanyName, s.Sector, s.CurrentPrice, s.DayChangePercent, s.MarketCap, null, null)).ToList();
        var alerts = await _db.StockAnalyses.Include(a => a.Stock).Include(a => a.ScoringProfile)
            .Where(a => a.IsAlert).OrderByDescending(a => a.OverallScore).Take(10).AsNoTracking()
            .Select(a => new AnalysisDto(a.StockId, a.Stock.Symbol, a.Stock.CompanyName, a.AnalyzedAt,
                a.TechnicalSignal.ToString(), a.FundamentalSignal.ToString(), a.SentimentSignal.ToString(), a.OverallSignal.ToString(),
                a.TechnicalScore, a.FundamentalScore, a.SentimentScore, a.DividendScore, a.OverallScore,
                a.Reasoning, a.IsAlert, a.AlertMessage, a.ScoringProfile.Name)).ToListAsync();
        var sectors = stocks.Where(s => s.Sector != null).GroupBy(s => s.Sector!)
            .Select(g => new SectorPerformanceDto(g.Key, g.Average(s => s.DayChangePercent ?? 0), g.Count())).ToList();
        return new DashboardDto(topGainers, topLosers, alerts, sectors, stocks.Count, alerts.Count);
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
