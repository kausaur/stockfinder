using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;

namespace Nifty50.Infrastructure.Services;

public class SectorRelativeService : ISectorRelativeService
{
    private readonly IStockRepository _repo;

    public SectorRelativeService(IStockRepository repo)
    {
        _repo = repo;
    }

    public async Task<SectorBenchmark?> GetBenchmarkAsync(string? sector)
    {
        if (string.IsNullOrWhiteSpace(sector)) return null;
        return await _repo.GetSectorBenchmarkAsync(sector);
    }

    public bool IsBFSI(string? sector)
    {
        if (string.IsNullOrWhiteSpace(sector)) return false;
        
        var lowerSector = sector.ToLowerInvariant();
        return lowerSector.Contains("bank") || 
               lowerSector.Contains("financial") || 
               lowerSector.Contains("insurance");
    }

    public double ScoreRelativePE(decimal? stockPE, SectorBenchmark? benchmark)
    {
        if (!stockPE.HasValue) return 0;
        if (stockPE.Value <= 0) return 0; // Negative P/E gets 0 score

        if (benchmark?.MedianPE == null || benchmark.MedianPE.Value <= 0)
        {
            // Fallback: absolute scoring if no benchmark
            if (stockPE.Value < 15) return 100;
            if (stockPE.Value < 20) return 80;
            if (stockPE.Value < 25) return 60;
            if (stockPE.Value < 35) return 40;
            if (stockPE.Value < 50) return 20;
            return 0;
        }

        // Relative scoring
        var median = benchmark.MedianPE.Value;
        var ratio = stockPE.Value / median;

        if (ratio < 0.5m) return 100; // Deep discount to sector
        if (ratio < 0.8m) return 80;  // Discount
        if (ratio <= 1.2m) return 50; // Roughly in line
        if (ratio < 1.5m) return 30;  // Premium
        if (ratio < 2.0m) return 10;  // High premium
        return 0;                     // Very expensive compared to sector
    }

    public double ScoreRelativeROE(decimal? stockROE, SectorBenchmark? benchmark)
    {
        if (!stockROE.HasValue) return 0;
        
        if (benchmark?.MedianROE == null || benchmark.MedianROE.Value <= 0)
        {
            // Fallback: absolute scoring
            if (stockROE.Value > 25) return 100;
            if (stockROE.Value > 20) return 80;
            if (stockROE.Value > 15) return 60;
            if (stockROE.Value > 10) return 40;
            if (stockROE.Value > 5) return 20;
            return 0;
        }

        // Relative scoring
        var median = benchmark.MedianROE.Value;
        var diff = stockROE.Value - median;

        if (diff > 10) return 100; // Significantly outperforms sector
        if (diff > 5) return 80;   // Outperforms
        if (diff > -2) return 50;  // Roughly in line
        if (diff > -5) return 30;  // Underperforms
        if (diff > -10) return 10; // Significantly underperforms
        return 0;                  // Poor ROE relative to sector
    }
}
