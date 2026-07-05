using Microsoft.EntityFrameworkCore;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Data;

namespace Nifty50.Infrastructure.Services;

public class ScoringProfileService : IScoringProfileService
{
    private readonly AppDbContext _db;
    public ScoringProfileService(AppDbContext db) => _db = db;

    public async Task<List<ScoringProfile>> GetAllProfilesAsync() =>
        await _db.ScoringProfiles.OrderBy(p => p.Name).AsNoTracking().ToListAsync();

    public async Task<ScoringProfile> GetActiveProfileAsync() =>
        await _db.ScoringProfiles.FirstOrDefaultAsync(p => p.IsDefault)
        ?? await _db.ScoringProfiles.FirstAsync(); // Fallback

    public async Task<ScoringProfile?> GetByIdAsync(Guid id) =>
        await _db.ScoringProfiles.FindAsync(id);

    public async Task<ScoringProfile> CreateProfileAsync(ScoringProfileDto dto)
    {
        var profile = new ScoringProfile
        {
            Name = dto.Name, IsPreset = false,
            TechnicalWeight = dto.TechnicalWeight, FundamentalWeight = dto.FundamentalWeight,
            SentimentWeight = dto.SentimentWeight, DividendWeight = dto.DividendWeight,
            ValuationWeight = dto.ValuationWeight, QualityWeight = dto.QualityWeight,
            TechRSIWeight = dto.TechRSIWeight, TechMACDWeight = dto.TechMACDWeight,
            TechMovingAvgWeight = dto.TechMovingAvgWeight, TechBollingerWeight = dto.TechBollingerWeight,
            TechADXWeight = dto.TechADXWeight, TechVolumeWeight = dto.TechVolumeWeight,
            FundValuationWeight = dto.FundValuationWeight, FundProfitabilityWeight = dto.FundProfitabilityWeight,
            FundLiquidityWeight = dto.FundLiquidityWeight, FundLeverageWeight = dto.FundLeverageWeight,
            FundGrowthWeight = dto.FundGrowthWeight, FundROCEWeight = dto.FundROCEWeight, FundPEGWeight = dto.FundPEGWeight,
            QualPiotroskiWeight = dto.QualPiotroskiWeight, QualAltmanWeight = dto.QualAltmanWeight,
            QualPromoterWeight = dto.QualPromoterWeight, QualFIIWeight = dto.QualFIIWeight,
            QualDividendConsistencyWeight = dto.QualDividendConsistencyWeight, QualFCFTrendWeight = dto.QualFCFTrendWeight,
            AlertMinOverallScore = dto.AlertMinOverallScore, AlertMinTechnicalScore = dto.AlertMinTechnicalScore,
            AlertMinFundamentalScore = dto.AlertMinFundamentalScore, AlertMinSentimentScore = dto.AlertMinSentimentScore,
            StrongBuyThreshold = dto.StrongBuyThreshold, BuyThreshold = dto.BuyThreshold,
            HoldThreshold = dto.HoldThreshold, SellThreshold = dto.SellThreshold,
        };
        _db.ScoringProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task UpdateActiveProfileAsync(ScoringProfileUpdateDto dto)
    {
        var profile = await GetActiveProfileAsync();
        if (profile.IsPreset) { /* Clone to custom */ profile.IsDefault = false; profile = new ScoringProfile { Name = "Custom", IsDefault = true }; _db.ScoringProfiles.Add(profile); }
        profile.TechnicalWeight = dto.TechnicalWeight; profile.FundamentalWeight = dto.FundamentalWeight;
        profile.SentimentWeight = dto.SentimentWeight; profile.DividendWeight = dto.DividendWeight;
        profile.ValuationWeight = dto.ValuationWeight; profile.QualityWeight = dto.QualityWeight;
        profile.TechRSIWeight = dto.TechRSIWeight; profile.TechMACDWeight = dto.TechMACDWeight;
        profile.TechMovingAvgWeight = dto.TechMovingAvgWeight; profile.TechBollingerWeight = dto.TechBollingerWeight;
        profile.TechADXWeight = dto.TechADXWeight; profile.TechVolumeWeight = dto.TechVolumeWeight;
        profile.FundValuationWeight = dto.FundValuationWeight; profile.FundProfitabilityWeight = dto.FundProfitabilityWeight;
        profile.FundLiquidityWeight = dto.FundLiquidityWeight; profile.FundLeverageWeight = dto.FundLeverageWeight;
        profile.FundGrowthWeight = dto.FundGrowthWeight; profile.FundROCEWeight = dto.FundROCEWeight; profile.FundPEGWeight = dto.FundPEGWeight;
        profile.QualPiotroskiWeight = dto.QualPiotroskiWeight; profile.QualAltmanWeight = dto.QualAltmanWeight;
        profile.QualPromoterWeight = dto.QualPromoterWeight; profile.QualFIIWeight = dto.QualFIIWeight;
        profile.QualDividendConsistencyWeight = dto.QualDividendConsistencyWeight; profile.QualFCFTrendWeight = dto.QualFCFTrendWeight;
        profile.AlertMinOverallScore = dto.AlertMinOverallScore; profile.AlertMinTechnicalScore = dto.AlertMinTechnicalScore;
        profile.AlertMinFundamentalScore = dto.AlertMinFundamentalScore; profile.AlertMinSentimentScore = dto.AlertMinSentimentScore;
        profile.StrongBuyThreshold = dto.StrongBuyThreshold; profile.BuyThreshold = dto.BuyThreshold;
        profile.HoldThreshold = dto.HoldThreshold; profile.SellThreshold = dto.SellThreshold;
        await _db.SaveChangesAsync();
    }

    public async Task ActivateProfileAsync(Guid id)
    {
        var all = await _db.ScoringProfiles.ToListAsync();
        if (!all.Any(p => p.Id == id)) throw new KeyNotFoundException("Profile not found");
        foreach (var p in all) p.IsDefault = p.Id == id;
        await _db.SaveChangesAsync();
    }

    public async Task ResetToDefaultAsync()
    {
        var balanced = await _db.ScoringProfiles.FirstOrDefaultAsync(p => p.Name == "Balanced" && p.IsPreset);
        if (balanced != null) await ActivateProfileAsync(balanced.Id);
    }
}
