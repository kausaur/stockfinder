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
        var analyses = await _db.StockAnalyses.Include(a => a.Stock).ToListAsync();
        
        // Only latest analyses
        var latestAnalyses = analyses
            .GroupBy(a => a.StockId)
            .Select(g => g.OrderByDescending(a => a.AnalyzedAt).First())
            .ToList();

        // Get latest intrinsic valuation and quality for extra details
        var valuations = await _db.IntrinsicValuations
            .GroupBy(v => v.StockId)
            .Select(g => g.OrderByDescending(v => v.ComputedAt).First())
            .ToDictionaryAsync(v => v.StockId);
            
        var qualities = await _db.QualityMetrics
            .GroupBy(q => q.StockId)
            .Select(g => g.OrderByDescending(q => q.AsOfDate).First())
            .ToDictionaryAsync(q => q.StockId);

        var recommendations = latestAnalyses.Select(a =>
        {
            valuations.TryGetValue(a.StockId, out var v);
            qualities.TryGetValue(a.StockId, out var q);
            return MapToRecommendationDto(a, v, q);
        }).ToList();

        var topBullish = recommendations
            .Where(r => r.OverallSignal == "StrongBuy" || r.OverallSignal == "Buy")
            .OrderByDescending(r => r.OverallScore)
            .Take(10)
            .ToList();

        var bottomBearish = recommendations
            .Where(r => r.OverallSignal == "StrongSell" || r.OverallSignal == "Sell")
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

        var bullishCount = recommendations.Count(r => r.OverallSignal == "StrongBuy" || r.OverallSignal == "Buy");
        var bearishCount = recommendations.Count(r => r.OverallSignal == "StrongSell" || r.OverallSignal == "Sell");

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
        var analysesQuery = _db.StockAnalyses.Include(a => a.Stock).AsQueryable();
        
        // In practice we'd just want the latest per stock. Since this is an EF query, we could fetch all latest first.
        var latestAnalyses = await analysesQuery
            .GroupBy(a => a.StockId)
            .Select(g => g.OrderByDescending(a => a.AnalyzedAt).FirstOrDefault())
            .Where(a => a != null)
            .ToListAsync();
            
        var stockIds = latestAnalyses.Select(a => a!.StockId).ToList();
        
        var valuations = await _db.IntrinsicValuations
            .Where(v => stockIds.Contains(v.StockId))
            .GroupBy(v => v.StockId)
            .Select(g => g.OrderByDescending(v => v.ComputedAt).FirstOrDefault())
            .ToDictionaryAsync(v => v!.StockId);
            
        var qualities = await _db.QualityMetrics
            .Where(q => stockIds.Contains(q.StockId))
            .GroupBy(q => q.StockId)
            .Select(g => g.OrderByDescending(q => q.AsOfDate).FirstOrDefault())
            .ToDictionaryAsync(q => q!.StockId);
            
        var fundamentals = await _db.FundamentalMetrics
            .Where(f => stockIds.Contains(f.StockId))
            .GroupBy(f => f.StockId)
            .Select(g => g.OrderByDescending(f => f.PeriodEndDate).FirstOrDefault())
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
            "DivYield" => results.OrderByDescending(r => r.UpsidePercent ?? -9999), // proxy since DivYield isn't on DTO root
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
        return new StockRecommendationDto(
            a.StockId, a.Stock.Symbol, a.Stock.CompanyName, a.Stock.Sector,
            a.Stock.CurrentPrice, a.Stock.DayChangePercent,
            a.OverallSignal.ToString(), a.OverallScore,
            a.TechnicalScore, a.FundamentalScore, a.ValuationScore ?? 0, a.QualityScore ?? 0,
            f?.PERatio, f?.ROE ?? q?.ROCELatest, v?.UpsidePercent, v?.ValuationVerdict,
            a.Reasoning
        );
    }
}
