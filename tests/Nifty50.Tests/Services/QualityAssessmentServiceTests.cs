using Moq;
using Nifty50.Core.Entities;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Services;
using Xunit;

namespace Nifty50.Tests.Services;

public class QualityAssessmentServiceTests
{
    private readonly QualityAssessmentService _service;
    private readonly Mock<ISectorRelativeService> _mockSectorService;

    public QualityAssessmentServiceTests()
    {
        _mockSectorService = new Mock<ISectorRelativeService>();
        _mockSectorService.Setup(s => s.IsBFSI(It.IsAny<string>())).Returns(false);
        _service = new QualityAssessmentService(_mockSectorService.Object);
    }

    [Fact]
    public void AssessQuality_WithPerfectCompany_ReturnsPiotroski9()
    {
        // Arrange
        var stockId = Guid.NewGuid();
        
        var statements = new List<FinancialStatement>
        {
            new FinancialStatement { PeriodEndDate = new DateTime(2023, 3, 31), NetIncome = 100, TotalAssets = 1000, OperatingCashFlow = 150, TotalDebt = 200, CurrentAssets = 150, CurrentLiabilities = 100, GrossProfit = 220, TotalRevenue = 1100 },
            new FinancialStatement { PeriodEndDate = new DateTime(2022, 3, 31), NetIncome = 80, TotalAssets = 900, OperatingCashFlow = 100, TotalDebt = 250, CurrentAssets = 120, CurrentLiabilities = 100, GrossProfit = 162, TotalRevenue = 900 }
        };
        
        var metric = new FundamentalMetric
        {
            StockId = stockId,
            ROA = 10m
        };

        // Act
        var result = _service.AssessQuality(stockId, statements, metric, null, 100000m, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(stockId, result.StockId);
        Assert.Equal(9, result.PiotroskiFScore);
    }
}
