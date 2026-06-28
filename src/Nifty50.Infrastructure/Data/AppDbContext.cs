using Microsoft.EntityFrameworkCore;
using Nifty50.Core.Entities;

namespace Nifty50.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockPrice> StockPrices => Set<StockPrice>();
    public DbSet<Dividend> Dividends => Set<Dividend>();
    public DbSet<FinancialStatement> FinancialStatements => Set<FinancialStatement>();
    public DbSet<FundamentalMetric> FundamentalMetrics => Set<FundamentalMetric>();
    public DbSet<TechnicalIndicator> TechnicalIndicators => Set<TechnicalIndicator>();
    public DbSet<SentimentAnalysis> SentimentAnalyses => Set<SentimentAnalysis>();
    public DbSet<StockAnalysis> StockAnalyses => Set<StockAnalysis>();
    public DbSet<ScoringProfile> ScoringProfiles => Set<ScoringProfile>();
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<SectorBenchmark> SectorBenchmarks => Set<SectorBenchmark>();
    public DbSet<IntrinsicValuation> IntrinsicValuations => Set<IntrinsicValuation>();
    public DbSet<QualityMetric> QualityMetrics => Set<QualityMetric>();
    public DbSet<ScoreHistory> ScoreHistories => Set<ScoreHistory>();
    public DbSet<IndexMembership> IndexMemberships => Set<IndexMembership>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global soft-delete query filters
        modelBuilder.Entity<Stock>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StockPrice>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Dividend>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FinancialStatement>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FundamentalMetric>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TechnicalIndicator>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SentimentAnalysis>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StockAnalysis>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ScoringProfile>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SectorBenchmark>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<IntrinsicValuation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<QualityMetric>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ScoreHistory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<IndexMembership>().HasQueryFilter(e => !e.IsDeleted);

        // Stock
        modelBuilder.Entity<Stock>(e =>
        {
            e.HasIndex(s => s.Symbol).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.Property(s => s.Symbol).HasMaxLength(20).IsRequired();
            e.Property(s => s.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(s => s.Sector).HasMaxLength(100);
            e.Property(s => s.Industry).HasMaxLength(100);
        });

        // StockPrice
        modelBuilder.Entity<StockPrice>(e =>
        {
            e.HasIndex(p => new { p.StockId, p.Date }).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasOne(p => p.Stock).WithMany(s => s.Prices).HasForeignKey(p => p.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // Dividend
        modelBuilder.Entity<Dividend>(e =>
        {
            e.HasOne(d => d.Stock).WithMany(s => s.Dividends).HasForeignKey(d => d.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // FinancialStatement
        modelBuilder.Entity<FinancialStatement>(e =>
        {
            e.HasIndex(f => new { f.StockId, f.StatementType, f.Period, f.PeriodEndDate }).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasOne(f => f.Stock).WithMany(s => s.FinancialStatements).HasForeignKey(f => f.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // FundamentalMetric
        modelBuilder.Entity<FundamentalMetric>(e =>
        {
            e.HasIndex(f => new { f.StockId, f.PeriodEndDate }).HasFilter("\"IsDeleted\" = false");
            e.HasOne(f => f.Stock).WithMany(s => s.FundamentalMetrics).HasForeignKey(f => f.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // TechnicalIndicator
        modelBuilder.Entity<TechnicalIndicator>(e =>
        {
            e.HasIndex(t => new { t.StockId, t.Date }).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasOne(t => t.Stock).WithMany(s => s.TechnicalIndicators).HasForeignKey(t => t.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // SentimentAnalysis
        modelBuilder.Entity<SentimentAnalysis>(e =>
        {
            e.HasIndex(s => new { s.StockId, s.AnalyzedAt }).HasFilter("\"IsDeleted\" = false");
            e.HasOne(s => s.Stock).WithMany(s => s.SentimentAnalyses).HasForeignKey(s => s.StockId).OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.TopHeadlines).HasColumnType("jsonb");
        });

        // StockAnalysis
        modelBuilder.Entity<StockAnalysis>(e =>
        {
            e.HasIndex(a => new { a.StockId, a.AnalyzedAt }).HasFilter("\"IsDeleted\" = false");
            e.HasOne(a => a.Stock).WithMany(s => s.StockAnalyses).HasForeignKey(a => a.StockId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.ScoringProfile).WithMany(p => p.StockAnalyses).HasForeignKey(a => a.ScoringProfileId).OnDelete(DeleteBehavior.Restrict);
            e.Property(a => a.WeightsUsed).HasColumnType("jsonb");
        });

        // ScoringProfile
        modelBuilder.Entity<ScoringProfile>(e =>
        {
            e.HasIndex(p => p.Name).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
        });

        // SectorBenchmark
        modelBuilder.Entity<SectorBenchmark>(e =>
        {
            e.HasIndex(s => new { s.Sector, s.AsOfDate }).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        // IntrinsicValuation
        modelBuilder.Entity<IntrinsicValuation>(e =>
        {
            e.HasIndex(i => new { i.StockId, i.ComputedAt }).HasFilter("\"IsDeleted\" = false");
            e.HasOne(i => i.Stock).WithMany(s => s.IntrinsicValuations).HasForeignKey(i => i.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // QualityMetric
        modelBuilder.Entity<QualityMetric>(e =>
        {
            e.HasIndex(q => new { q.StockId, q.AsOfDate }).HasFilter("\"IsDeleted\" = false");
            e.HasOne(q => q.Stock).WithMany(s => s.QualityMetrics).HasForeignKey(q => q.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // ScoreHistory
        modelBuilder.Entity<ScoreHistory>(e =>
        {
            e.HasIndex(s => new { s.StockId, s.RecordedAt }).HasFilter("\"IsDeleted\" = false");
            e.HasOne(s => s.Stock).WithMany(st => st.ScoreHistories).HasForeignKey(s => s.StockId).OnDelete(DeleteBehavior.Cascade);
        });

        // IndexMembership
        modelBuilder.Entity<IndexMembership>(e =>
        {
            e.HasIndex(i => new { i.IndexName, i.StockId }).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasOne(i => i.Stock).WithMany(s => s.IndexMemberships).HasForeignKey(i => i.StockId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}

public static class SeedData
{
    public static async Task SeedPresetsAsync(AppDbContext db)
    {
        if (await db.ScoringProfiles.AnyAsync()) return;

        var presets = new[]
        {
            // Balanced: well-rounded assessment across all factors
            new ScoringProfile
            {
                Name = "Balanced", IsDefault = true, IsPreset = true,
                TechnicalWeight = 30, FundamentalWeight = 35, SentimentWeight = 15, DividendWeight = 20,
                TechRSIWeight = 20, TechMACDWeight = 25, TechMovingAvgWeight = 25,
                TechBollingerWeight = 15, TechADXWeight = 10, TechVolumeWeight = 5,
                FundValuationWeight = 25, FundProfitabilityWeight = 30,
                FundLiquidityWeight = 15, FundLeverageWeight = 15, FundGrowthWeight = 15,
                AlertMinOverallScore = 60, AlertMinTechnicalScore = 0,
                AlertMinFundamentalScore = 0, AlertMinSentimentScore = 0,
                StrongBuyThreshold = 75, BuyThreshold = 60, HoldThreshold = 40, SellThreshold = 20,
            },
            // Growth: chases earnings & revenue momentum, tolerates premium valuations
            new ScoringProfile
            {
                Name = "Growth", IsPreset = true,
                TechnicalWeight = 25, FundamentalWeight = 45, SentimentWeight = 20, DividendWeight = 10,
                TechRSIWeight = 15, TechMACDWeight = 30, TechMovingAvgWeight = 25,
                TechBollingerWeight = 10, TechADXWeight = 15, TechVolumeWeight = 5,
                FundValuationWeight = 10, FundProfitabilityWeight = 25,
                FundLiquidityWeight = 10, FundLeverageWeight = 10, FundGrowthWeight = 45,
                AlertMinOverallScore = 62, AlertMinTechnicalScore = 0,
                AlertMinFundamentalScore = 0, AlertMinSentimentScore = 0,
                StrongBuyThreshold = 75, BuyThreshold = 60, HoldThreshold = 45, SellThreshold = 30,
            },
            // Value: hunts for undervalued stocks with strong balance sheets
            new ScoringProfile
            {
                Name = "Value", IsPreset = true,
                TechnicalWeight = 15, FundamentalWeight = 60, SentimentWeight = 5, DividendWeight = 20,
                TechRSIWeight = 35, TechMACDWeight = 10, TechMovingAvgWeight = 20,
                TechBollingerWeight = 25, TechADXWeight = 5, TechVolumeWeight = 5,
                FundValuationWeight = 40, FundProfitabilityWeight = 25,
                FundLiquidityWeight = 15, FundLeverageWeight = 15, FundGrowthWeight = 5,
                AlertMinOverallScore = 63, AlertMinTechnicalScore = 40,
                AlertMinFundamentalScore = 65, AlertMinSentimentScore = 30,
                StrongBuyThreshold = 73, BuyThreshold = 58, HoldThreshold = 40, SellThreshold = 28,
            },
            // Income: maximises dividend yield with payout sustainability
            new ScoringProfile
            {
                Name = "Income", IsPreset = true,
                TechnicalWeight = 10, FundamentalWeight = 25, SentimentWeight = 5, DividendWeight = 60,
                TechRSIWeight = 20, TechMACDWeight = 15, TechMovingAvgWeight = 30,
                TechBollingerWeight = 15, TechADXWeight = 10, TechVolumeWeight = 10,
                FundValuationWeight = 20, FundProfitabilityWeight = 30,
                FundLiquidityWeight = 20, FundLeverageWeight = 20, FundGrowthWeight = 10,
                AlertMinOverallScore = 60, AlertMinTechnicalScore = 35,
                AlertMinFundamentalScore = 45, AlertMinSentimentScore = 30,
                StrongBuyThreshold = 70, BuyThreshold = 55, HoldThreshold = 38, SellThreshold = 25,
            },
            // Momentum: rides strong price trends with high conviction
            new ScoringProfile
            {
                Name = "Momentum", IsPreset = true,
                TechnicalWeight = 55, FundamentalWeight = 20, SentimentWeight = 20, DividendWeight = 5,
                TechRSIWeight = 15, TechMACDWeight = 30, TechMovingAvgWeight = 20,
                TechBollingerWeight = 10, TechADXWeight = 20, TechVolumeWeight = 5,
                FundValuationWeight = 15, FundProfitabilityWeight = 30,
                FundLiquidityWeight = 10, FundLeverageWeight = 15, FundGrowthWeight = 30,
                AlertMinOverallScore = 62, AlertMinTechnicalScore = 58,
                AlertMinFundamentalScore = 40, AlertMinSentimentScore = 40,
                StrongBuyThreshold = 72, BuyThreshold = 58, HoldThreshold = 40, SellThreshold = 26,
            },
            // Quality: focuses on high-ROE, low-debt, consistently profitable companies
            new ScoringProfile
            {
                Name = "Quality", IsPreset = true,
                TechnicalWeight = 15, FundamentalWeight = 55, SentimentWeight = 10, DividendWeight = 20,
                TechRSIWeight = 20, TechMACDWeight = 20, TechMovingAvgWeight = 25,
                TechBollingerWeight = 15, TechADXWeight = 10, TechVolumeWeight = 10,
                FundValuationWeight = 15, FundProfitabilityWeight = 40,
                FundLiquidityWeight = 15, FundLeverageWeight = 25, FundGrowthWeight = 5,
                AlertMinOverallScore = 63, AlertMinTechnicalScore = 40,
                AlertMinFundamentalScore = 60, AlertMinSentimentScore = 35,
                StrongBuyThreshold = 73, BuyThreshold = 58, HoldThreshold = 40, SellThreshold = 26,
            },
        };

        db.ScoringProfiles.AddRange(presets);
        await db.SaveChangesAsync();
    }
    public static async Task UpdatePresetsForV2Async(AppDbContext db)
    {
        var presets = await db.ScoringProfiles.Where(p => p.IsPreset).ToListAsync();
        foreach (var p in presets)
        {
            switch (p.Name)
            {
                case "Balanced":
                    p.TechnicalWeight = 25; p.FundamentalWeight = 25; p.SentimentWeight = 12; p.DividendWeight = 13; p.ValuationWeight = 13; p.QualityWeight = 12;
                    break;
                case "Growth":
                    p.TechnicalWeight = 20; p.FundamentalWeight = 32; p.SentimentWeight = 15; p.DividendWeight = 5; p.ValuationWeight = 18; p.QualityWeight = 10;
                    break;
                case "Value":
                    p.TechnicalWeight = 10; p.FundamentalWeight = 30; p.SentimentWeight = 3; p.DividendWeight = 12; p.ValuationWeight = 30; p.QualityWeight = 15;
                    break;
                case "Income":
                    p.TechnicalWeight = 8; p.FundamentalWeight = 18; p.SentimentWeight = 4; p.DividendWeight = 45; p.ValuationWeight = 10; p.QualityWeight = 15;
                    break;
                case "Momentum":
                    p.TechnicalWeight = 45; p.FundamentalWeight = 15; p.SentimentWeight = 18; p.DividendWeight = 3; p.ValuationWeight = 10; p.QualityWeight = 9;
                    break;
                case "Quality":
                    p.TechnicalWeight = 10; p.FundamentalWeight = 28; p.SentimentWeight = 5; p.DividendWeight = 12; p.ValuationWeight = 15; p.QualityWeight = 30;
                    break;
            }
        }

        if (!await db.ScoringProfiles.AnyAsync(p => p.Name == "Long-Term Investor" && !p.IsDeleted))
        {
            db.ScoringProfiles.Add(new ScoringProfile
            {
                Name = "Long-Term Investor", IsPreset = true,
                TechnicalWeight = 12, FundamentalWeight = 28, SentimentWeight = 5, DividendWeight = 7, ValuationWeight = 25, QualityWeight = 23,
                TechRSIWeight = 20, TechMACDWeight = 25, TechMovingAvgWeight = 25, TechBollingerWeight = 15, TechADXWeight = 10, TechVolumeWeight = 5,
                FundValuationWeight = 15, FundProfitabilityWeight = 25, FundLiquidityWeight = 15, FundLeverageWeight = 15, FundGrowthWeight = 15, FundROCEWeight = 10, FundPEGWeight = 5,
                QualPiotroskiWeight = 30, QualAltmanWeight = 15, QualPromoterWeight = 20, QualFIIWeight = 15, QualDividendConsistencyWeight = 10, QualFCFTrendWeight = 10,
                AlertMinOverallScore = 65, AlertMinTechnicalScore = 0, AlertMinFundamentalScore = 50, AlertMinSentimentScore = 0,
                StrongBuyThreshold = 75, BuyThreshold = 60, HoldThreshold = 42, SellThreshold = 25
            });
        }

        await db.SaveChangesAsync();
    }
}
