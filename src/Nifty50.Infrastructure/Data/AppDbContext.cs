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
                AlertMinOverallScore = 65, AlertMinTechnicalScore = 50,
                AlertMinFundamentalScore = 55, AlertMinSentimentScore = 40,
                StrongBuyThreshold = 75, BuyThreshold = 60, HoldThreshold = 42, SellThreshold = 28,
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
                AlertMinOverallScore = 62, AlertMinTechnicalScore = 45,
                AlertMinFundamentalScore = 60, AlertMinSentimentScore = 40,
                StrongBuyThreshold = 72, BuyThreshold = 58, HoldThreshold = 40, SellThreshold = 25,
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
}
