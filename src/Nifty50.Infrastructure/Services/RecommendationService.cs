using Microsoft.EntityFrameworkCore;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Data;

namespace Nifty50.Infrastructure.Services;

public class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _db;

    public RecommendationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RecommendationDashboardDto> GetDashboardAsync()
    {
        var activeStocks = await _db.Stocks.Where(s => s.IsActive).ToListAsync();
        
        // Fetch latest analysis per active stock using a projection
        var latestAnalyses = await _db.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.StockAnalyses.OrderByDescending(a => a.AnalyzedAt).FirstOrDefault())
            .Where(a => a != null)
            .ToListAsync();
            
        // Link navigation properties manually since Include is lost in projection
        foreach(var a in latestAnalyses) 
        {
            a!.Stock = activeStocks.FirstOrDefault(s => s.Id == a.StockId);
        }
        latestAnalyses.RemoveAll(a => a.Stock == null);

        // Get latest intrinsic valuation and quality for extra details
        var valuations = await _db.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.IntrinsicValuations.OrderByDescending(v => v.ComputedAt).FirstOrDefault())
            .Where(v => v != null)
            .ToDictionaryAsync(v => v!.StockId);
            
        var qualities = await _db.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.QualityMetrics.OrderByDescending(q => q.AsOfDate).FirstOrDefault())
            .Where(q => q != null)
            .ToDictionaryAsync(q => q!.StockId);

        var recommendations = latestAnalyses.Select(a =>
        {
            valuations.TryGetValue(a!.StockId, out var v);
            qualities.TryGetValue(a.StockId, out var q);
            return MapToRecommendationDto(a, v, q);
        }).ToList();

        var topBullish = recommendations
            .Where(r => r.OverallSignal == "Strong Bullish" || r.OverallSignal == "Bullish")
            .OrderByDescending(r => r.OverallScore)
            .Take(10)
            .ToList();

        var bottomBearish = recommendations
            .Where(r => r.OverallSignal == "Strong Bearish" || r.OverallSignal == "Bearish")
            .OrderBy(r => r.OverallScore)
            .Take(5)
            .ToList();

        var valueOps = recommendations
            .Where(r => r.ValuationVerdict == "Significantly Undervalued" || r.ValuationVerdict == "Moderately Undervalued")
            .Where(r => r.QualityScore >= 60)
            .OrderByDescending(r => r.UpsidePercent)
            .Take(5)
            .ToList();

        var sectors = activeStocks.Where(s => s.Sector != null)
            .GroupBy(s => s.Sector!)
            .Select(g => new SectorPerformanceDto(
                g.Key, 
                g.Average(s => s.DayChangePercent ?? 0), 
                g.Count()))
            .OrderByDescending(s => s.AverageChangePercent)
            .ToList();

        var bullishCount = recommendations.Count(r => r.OverallSignal == "Strong Bullish" || r.OverallSignal == "Bullish");
        var bearishCount = recommendations.Count(r => r.OverallSignal == "Strong Bearish" || r.OverallSignal == "Bearish");

        return new RecommendationDashboardDto(
            topBullish,
            bottomBearish,
            sectors,
            valueOps,
            bullishCount,
            bearishCount
        );
    }

    public async Task<List<StockRecommendationDto>> ScreenStocksAsync(ScreenerFilters filters)
    {
        var activeStocks = await _db.Stocks.Where(s => s.IsActive).ToListAsync();
        
        var latestAnalyses = await _db.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.StockAnalyses.OrderByDescending(a => a.AnalyzedAt).FirstOrDefault())
            .Where(a => a != null)
            .ToListAsync();
            
        foreach(var a in latestAnalyses) 
        {
            var stock = activeStocks.FirstOrDefault(s => s.Id == a!.StockId);
            if (stock != null)
                a!.Stock = stock;
        }
            
        var stockIds = latestAnalyses.Select(a => a!.StockId).ToList();
        
        var valuations = await _db.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.IntrinsicValuations.OrderByDescending(v => v.ComputedAt).FirstOrDefault())
            .Where(v => v != null)
            .ToDictionaryAsync(v => v!.StockId);
            
        var qualities = await _db.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.QualityMetrics.OrderByDescending(q => q.AsOfDate).FirstOrDefault())
            .Where(q => q != null)
            .ToDictionaryAsync(q => q!.StockId);
            
        var fundamentals = await _db.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.FundamentalMetrics.OrderByDescending(f => f.PeriodEndDate).FirstOrDefault())
            .Where(f => f != null)
            .ToDictionaryAsync(f => f!.StockId);

        var recommendations = latestAnalyses.Select(a =>
        {
            valuations.TryGetValue(a!.StockId, out var v);
            qualities.TryGetValue(a.StockId, out var q);
            fundamentals.TryGetValue(a.StockId, out var f);
            
            return new { Analysis = a, Val = v, Qual = q, Fund = f, Dto = MapToRecommendationDto(a, v, q, f) };
        });
        
        // Apply Filters
        if (filters.MinScore.HasValue)
            recommendations = recommendations.Where(r => r.Dto.OverallScore >= filters.MinScore.Value);
            
        if (filters.MaxScore.HasValue)
            recommendations = recommendations.Where(r => r.Dto.OverallScore <= filters.MaxScore.Value);
            
        if (filters.Sectors != null && filters.Sectors.Any())
            recommendations = recommendations.Where(r => filters.Sectors.Contains(r.Dto.Sector ?? ""));
            
        if (filters.Signals != null && filters.Signals.Any())
            recommendations = recommendations.Where(r => filters.Signals.Contains(r.Dto.OverallSignal));
            
        if (filters.MaxPE.HasValue)
            recommendations = recommendations.Where(r => r.Dto.PE <= filters.MaxPE.Value || r.Dto.PE == null);
            
        if (filters.MinROE.HasValue)
            recommendations = recommendations.Where(r => r.Dto.ROE >= filters.MinROE.Value);
            
        if (filters.MaxDebtToEquity.HasValue)
            recommendations = recommendations.Where(r => (r.Fund?.DebtToEquity ?? 0) <= filters.MaxDebtToEquity.Value);
            
        if (filters.MinDividendYield.HasValue)
            recommendations = recommendations.Where(r => (r.Fund?.DividendYield ?? 0) >= filters.MinDividendYield.Value);
            
        if (filters.MinPiotroskiScore.HasValue)
            recommendations = recommendations.Where(r => (r.Qual?.PiotroskiFScore ?? 0) >= filters.MinPiotroskiScore.Value);
            
        if (!string.IsNullOrEmpty(filters.ValuationVerdict) && filters.ValuationVerdict != "Any")
            recommendations = recommendations.Where(r => r.Dto.ValuationVerdict == filters.ValuationVerdict);

        // Sorting
        var results = recommendations.Select(r => r.Dto);
        results = filters.SortBy switch
        {
            "Score" => results.OrderByDescending(r => r.OverallScore),
            "PE" => results.OrderBy(r => r.PE ?? 9999),
            "ROE" => results.OrderByDescending(r => r.ROE ?? -9999),
            "DivYield" => results.OrderByDescending(r => r.DividendYield ?? -9999),
            _ => results.OrderByDescending(r => r.OverallScore)
        };

        return results.ToList();
    }

    public async Task<List<PeerComparisonDto>> GetPeersAsync(Guid stockId)
    {
        var targetStock = await _db.Stocks.FindAsync(stockId);
        if (targetStock == null || string.IsNullOrEmpty(targetStock.Sector)) return new List<PeerComparisonDto>();

        var peers = await _db.Stocks
            .Where(s => s.Sector == targetStock.Sector && s.IsActive)
            .ToListAsync();
            
        var peerIds = peers.Select(p => p.Id).ToList();

        var latestAnalyses = await _db.StockAnalyses
            .Where(a => peerIds.Contains(a.StockId))
            .GroupBy(a => a.StockId)
            .Select(g => g.OrderByDescending(a => a.AnalyzedAt).FirstOrDefault())
            .ToDictionaryAsync(a => a!.StockId);

        var valuations = await _db.IntrinsicValuations
            .Where(v => peerIds.Contains(v.StockId))
            .GroupBy(v => v.StockId)
            .Select(g => g.OrderByDescending(v => v.ComputedAt).FirstOrDefault())
            .ToDictionaryAsync(v => v!.StockId);

        var qualities = await _db.QualityMetrics
            .Where(q => peerIds.Contains(q.StockId))
            .GroupBy(q => q.StockId)
            .Select(g => g.OrderByDescending(q => q.AsOfDate).FirstOrDefault())
            .ToDictionaryAsync(q => q!.StockId);

        var fundamentals = await _db.FundamentalMetrics
            .Where(f => peerIds.Contains(f.StockId))
            .GroupBy(f => f.StockId)
            .Select(g => g.OrderByDescending(f => f.PeriodEndDate).FirstOrDefault())
            .ToDictionaryAsync(f => f!.StockId);

        var dtos = peers.Select(p =>
        {
            latestAnalyses.TryGetValue(p.Id, out var a);
            valuations.TryGetValue(p.Id, out var v);
            qualities.TryGetValue(p.Id, out var q);
            fundamentals.TryGetValue(p.Id, out var f);

            return new PeerComparisonDto(
                p.Id, p.Symbol, p.CompanyName, p.Sector,
                a?.OverallScore ?? 0,
                a?.TechnicalScore ?? 0,
                a?.FundamentalScore ?? 0,
                a?.ValuationScore ?? 0,
                a?.QualityScore ?? 0,
                f?.PERatio, f?.PBRatio, f?.ROE, f?.DebtToEquity, f?.DividendYield,
                p.MarketCap,
                v?.UpsidePercent,
                q?.PiotroskiFScore
            );
        }).OrderByDescending(p => p.OverallScore).ToList();

        return dtos;
    }

    private StockRecommendationDto MapToRecommendationDto(StockAnalysis a, IntrinsicValuation? v, QualityMetric? q, FundamentalMetric? f = null)
    {
        var sig = a.OverallSignal.ToString();
        if (sig == "StrongBuy") sig = "Strong Bullish";
        else if (sig == "Buy") sig = "Bullish";
        else if (sig == "Sell") sig = "Bearish";
        else if (sig == "StrongSell") sig = "Strong Bearish";

        return new StockRecommendationDto(
            a.StockId, a.Stock?.Symbol ?? "", a.Stock?.CompanyName ?? "", a.Stock?.Sector,
            a.Stock?.CurrentPrice, a.Stock?.DayChangePercent,
            sig, a.OverallScore,
            a.TechnicalScore, a.FundamentalScore, a.ValuationScore ?? 0, a.QualityScore ?? 0,
            f?.PERatio, f?.ROE ?? q?.ROICLatest, v?.UpsidePercent, v?.ValuationVerdict,
            a.Reasoning,
            f?.DividendYield
        );
    }
}
