using Nifty50.Core.Entities;
using Nifty50.Infrastructure.Services;
using Xunit;

namespace Nifty50.Tests.Services;

public class IntrinsicValueServiceTests
{
    private readonly IntrinsicValueService _service;

    public IntrinsicValueServiceTests()
    {
        _service = new IntrinsicValueService();
    }

    [Fact]
    public void CalculateIntrinsicValue_WithValidInputs_CalculatesUpsideAndVerdict()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        var currentPrice = 1000m;
        var metric = new FundamentalMetric
        {
            StockId = stockId,
            EPS = 50m,
            BookValuePerShare = 300m,
            EarningsGrowthYoY = 15m, // 15% growth
            PERatio = 20m,
            NetProfitMargin = 0.1m,
            OperatingMargin = 0.15m
        };

        // Act
        var result = _service.CalculateIntrinsicValue(stockId, currentPrice, metric);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(stockId, result.StockId);
        
        // Graham Number = sqrt(22.5 * 50 * 300) = sqrt(337500) = 580.95
        Assert.True(result.GrahamNumber > 580m && result.GrahamNumber < 582m);

        // EstimatedFairValue = (Graham + EPV) / 2
        Assert.True(result.EstimatedFairValue.HasValue);

        // Upside is calculated from EstimatedFairValue
        Assert.True(result.UpsidePercent.HasValue);
    }
}
