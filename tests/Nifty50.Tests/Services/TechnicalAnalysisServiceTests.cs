using System;
using System.Collections.Generic;
using System.Linq;
using Nifty50.Core.Entities;
using Nifty50.Infrastructure.Services;
using Xunit;

namespace Nifty50.Tests.Services;

public class TechnicalAnalysisServiceTests
{
    private readonly TechnicalAnalysisService _service;

    public TechnicalAnalysisServiceTests()
    {
        _service = new TechnicalAnalysisService();
    }

    [Fact]
    public void CalculateIndicators_NotEnoughData_ReturnsEmpty()
    {
        var stockId = Guid.NewGuid();
        var prices = new List<StockPrice>();
        for (int i = 0; i < 10; i++)
        {
            prices.Add(new StockPrice { Date = DateTime.UtcNow.AddDays(-10 + i), Close = 100 });
        }

        var result = _service.CalculateIndicators(stockId, prices);

        Assert.Empty(result);
    }

    [Fact]
    public void CalculateIndicators_ValidData_CalculatesCorrectly()
    {
        var stockId = Guid.NewGuid();
        var prices = new List<StockPrice>();
        var startDate = DateTime.UtcNow.AddDays(-100);
        
        // Generate dummy data (e.g. 50 data points)
        for (int i = 0; i < 50; i++)
        {
            prices.Add(new StockPrice 
            { 
                Date = startDate.AddDays(i), 
                Open = 100 + i, 
                High = 105 + i, 
                Low = 95 + i, 
                Close = 102 + i, 
                Volume = 10000 
            });
        }

        var result = _service.CalculateIndicators(stockId, prices);

        Assert.NotEmpty(result);
        Assert.Equal(50, result.Count);
        
        // Check if SMA20 is calculated for later points
        var lastIndicator = result.Last();
        Assert.NotNull(lastIndicator.SMA20);
        Assert.NotNull(lastIndicator.RSI14);
    }
}
