using Microsoft.EntityFrameworkCore;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IStockRepository _repo;

    public RecommendationService(IStockRepository repo)
    {
        _repo = repo;
    }

    public async Task<RecommendationDashboardDto> GetDashboardAsync()
    {
        var stocksData = await _repo.GetStocksWithDataAsync();

        var recommendations = stocksData
            .Where(d => d.Analysis != null)
            .Select(d =>
            {
                var a = d.Analysis!;
                a.Stock = d.Stock; // Link for MapToRecommendationDto
                return MapToRecommendationDto(a, d.Valuation, d.Quality, d.Fundamental);
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

        var sectors = stocksData
            .Select(d => d.Stock)
            .Where(s => s.Sector != null)
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
        var stocksData = await _repo.GetStocksWithDataAsync();
        
        var recommendations = stocksData
            .Where(d => d.Analysis != null)
            .Select(d =>
            {
                var a = d.Analysis!;
                a.Stock = d.Stock; // Link for MapToRecommendationDto
                return new { Analysis = a, Val = d.Valuation, Qual = d.Quality, Fund = d.Fundamental, Dto = MapToRecommendationDto(a, d.Valuation, d.Quality, d.Fundamental) };
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
        var allStocks = await _repo.GetAllAsync();
        var targetStock = allStocks.FirstOrDefault(s => s.Id == stockId);
        if (targetStock == null || string.IsNullOrEmpty(targetStock.Sector)) return new List<PeerComparisonDto>();

        var peersData = await _repo.GetStocksWithDataAsync(targetStock.Sector);
            
        var dtos = peersData.Select(p =>
        {
            return new PeerComparisonDto(
                p.Stock.Id, p.Stock.Symbol, p.Stock.CompanyName, p.Stock.Sector,
                p.Analysis?.OverallScore ?? 0,
                p.Analysis?.TechnicalScore ?? 0,
                p.Analysis?.FundamentalScore ?? 0,
                p.Analysis?.ValuationScore ?? 0,
                p.Analysis?.QualityScore ?? 0,
                p.Fundamental?.PERatio, p.Fundamental?.PBRatio, p.Fundamental?.ROE, p.Fundamental?.DebtToEquity, p.Fundamental?.DividendYield,
                p.Stock.MarketCap,
                p.Valuation?.UpsidePercent,
                p.Quality?.PiotroskiFScore
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
