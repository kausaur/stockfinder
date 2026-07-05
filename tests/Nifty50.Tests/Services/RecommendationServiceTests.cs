using Microsoft.EntityFrameworkCore;
using Nifty50.Core.Entities;
using Nifty50.Infrastructure.Data;
using Nifty50.Infrastructure.Services;
using Xunit;

namespace Nifty50.Tests.Services;

public class RecommendationServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetDashboardAsync_FiltersSignalsCorrectly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        var stockId1 = Guid.NewGuid();
        var stockId2 = Guid.NewGuid();
        
        context.Stocks.AddRange(
            new Stock { Id = stockId1, Symbol = "BULLSTOCK", CompanyName = "Bull Inc", IsActive = true, CurrentPrice = 100 },
            new Stock { Id = stockId2, Symbol = "BEARSTOCK", CompanyName = "Bear Inc", IsActive = true, CurrentPrice = 50 }
        );

        context.StockAnalyses.AddRange(
            new StockAnalysis { StockId = stockId1, OverallSignal = Nifty50.Core.Enums.SignalType.StrongBuy, OverallScore = 90, AnalyzedAt = DateTime.UtcNow },
            new StockAnalysis { StockId = stockId2, OverallSignal = Nifty50.Core.Enums.SignalType.StrongSell, OverallScore = 20, AnalyzedAt = DateTime.UtcNow }
        );

        await context.SaveChangesAsync();

        var service = new RecommendationService(context);

        // Act
        var dashboard = await service.GetDashboardAsync();

        // Assert
        Assert.Single(dashboard.TopBullish);
        Assert.Equal("BULLSTOCK", dashboard.TopBullish.First().Symbol);
        Assert.Equal(1, dashboard.BullishCount);
        
        Assert.Single(dashboard.BottomBearish);
        Assert.Equal("BEARSTOCK", dashboard.BottomBearish.First().Symbol);
        Assert.Equal(1, dashboard.BearishCount);
    }
}
