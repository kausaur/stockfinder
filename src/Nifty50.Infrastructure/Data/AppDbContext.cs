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
            new ScoringProfile { Name = "Balanced", IsDefault = true, IsPreset = true, TechnicalWeight = 40, FundamentalWeight = 25, SentimentWeight = 20, DividendWeight = 15 },
            new ScoringProfile { Name = "Growth", IsPreset = true, TechnicalWeight = 30, FundamentalWeight = 40, SentimentWeight = 25, DividendWeight = 5, FundGrowthWeight = 40, FundValuationWeight = 15 },
            new ScoringProfile { Name = "Value", IsPreset = true, TechnicalWeight = 35, FundamentalWeight = 40, SentimentWeight = 10, DividendWeight = 15, FundValuationWeight = 40, FundProfitabilityWeight = 25 },
            new ScoringProfile { Name = "Income", IsPreset = true, TechnicalWeight = 20, FundamentalWeight = 20, SentimentWeight = 10, DividendWeight = 50 },
        };

        db.ScoringProfiles.AddRange(presets);
        await db.SaveChangesAsync();
    }
}
