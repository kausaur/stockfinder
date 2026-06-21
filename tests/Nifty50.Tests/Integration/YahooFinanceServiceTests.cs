using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Nifty50.Core.DTOs;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Services;
using Xunit;

namespace Nifty50.Tests.Integration;

public class YahooFinanceServiceTests
{
    [Fact]
    public async Task FetchHistoricalPrices_ReturnsPrices_OnSuccess()
    {
        var mockMonitor = new Mock<IApiMonitorService>();
        var mockLogger = new Mock<ILogger<YahooFinanceService>>();
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Date,Open,High,Low,Close,Adj Close,Volume\n2025-01-01,100,105,95,102,102,1000\n2025-01-02,102,108,100,105,105,1200")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new YahooFinanceService(httpClient, mockLogger.Object, mockMonitor.Object);

        var result = await service.FetchHistoricalPricesAsync("RELIANCE.NS", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(100m, result[0].Open);
        Assert.Equal(105m, result[1].Close);
        
        mockMonitor.Verify(m => m.RecordApiCall(It.IsAny<ApiCallRecord>()), Times.Once);
    }
}
