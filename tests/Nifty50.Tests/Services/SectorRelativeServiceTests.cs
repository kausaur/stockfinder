using Nifty50.Core.Entities;
using Nifty50.Infrastructure.Services;
using Moq;
using Nifty50.Core.Interfaces;
using Xunit;

namespace Nifty50.Tests.Services;

public class SectorRelativeServiceTests
{
    private readonly SectorRelativeService _service;
    private readonly Mock<IStockRepository> _repoMock;

    public SectorRelativeServiceTests()
    {
        _repoMock = new Mock<IStockRepository>();
        _service = new SectorRelativeService(_repoMock.Object);
    }

    [Theory]
    [InlineData("Bank", true)]
    [InlineData("Banking", true)]
    [InlineData("Financial Services", true)]
    [InlineData("Financial Technology", false)] // 'technology' should exclude it
    [InlineData("IT Services", false)]
    [InlineData("Insurance", true)]
    [InlineData("NBFC", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsBFSI_ShouldIdentifyCorrectly(string? sector, bool expected)
    {
        // Act
        var result = _service.IsBFSI(sector);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ScoreRelativePE_WhenStockPENegativeOrZero_ReturnsZero()
    {
        Assert.Equal(0, _service.ScoreRelativePE(-5m, null));
        Assert.Equal(0, _service.ScoreRelativePE(0m, null));
    }

    [Fact]
    public void ScoreRelativePE_WithoutBenchmark_UsesAbsoluteScoring()
    {
        Assert.Equal(100, _service.ScoreRelativePE(14m, null));
        Assert.Equal(80, _service.ScoreRelativePE(18m, null));
        Assert.Equal(60, _service.ScoreRelativePE(22m, null));
        Assert.Equal(40, _service.ScoreRelativePE(30m, null));
        Assert.Equal(20, _service.ScoreRelativePE(45m, null));
        Assert.Equal(0, _service.ScoreRelativePE(55m, null));
    }

    [Fact]
    public void ScoreRelativePE_WithBenchmark_CalculatesCorrectly()
    {
        var benchmark = new SectorBenchmark { MedianPE = 20m };
        Assert.Equal(100, _service.ScoreRelativePE(8m, benchmark));
        Assert.Equal(80, _service.ScoreRelativePE(14m, benchmark));
        Assert.Equal(50, _service.ScoreRelativePE(20m, benchmark));
        Assert.Equal(30, _service.ScoreRelativePE(28m, benchmark));
        Assert.Equal(10, _service.ScoreRelativePE(38m, benchmark));
        Assert.Equal(0, _service.ScoreRelativePE(50m, benchmark));
    }
    
    [Fact]
    public void ScoreRelativeROE_WhenStockROENull_ReturnsZero()
    {
        Assert.Equal(0, _service.ScoreRelativeROE(null, null));
    }

    [Fact]
    public void ScoreRelativeROE_WithBenchmark_CalculatesCorrectly()
    {
        var benchmark = new SectorBenchmark { MedianROE = 15m };
        Assert.Equal(100, _service.ScoreRelativeROE(26m, benchmark));
        Assert.Equal(80, _service.ScoreRelativeROE(21m, benchmark));
        Assert.Equal(50, _service.ScoreRelativeROE(14m, benchmark));
        Assert.Equal(30, _service.ScoreRelativeROE(11m, benchmark));
        Assert.Equal(10, _service.ScoreRelativeROE(6m, benchmark));
        Assert.Equal(0, _service.ScoreRelativeROE(2m, benchmark));
    }
}
