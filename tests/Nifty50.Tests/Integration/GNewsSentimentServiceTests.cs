using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Nifty50.Core.DTOs;
using Nifty50.Core.Enums;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Services;
using Xunit;

namespace Nifty50.Tests.Integration;

public class GNewsSentimentServiceTests
{
    [Fact]
    public async Task AnalyzeSentiment_CalculatesSentiment_FromHeadlines()
    {
        var mockMonitor = new Mock<IApiMonitorService>();
        var mockLogger = new Mock<ILogger<GNewsSentimentService>>();
        
        var jsonResponse = @"{
            ""articles"": [
                { ""title"": ""Company shows strong profit and great growth"" },
                { ""title"": ""New breakthrough product announced"" },
                { ""title"": ""Stock market sees neutral movement today"" }
            ]
        }";

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
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new GNewsSentimentService(httpClient, mockLogger.Object, mockMonitor.Object);

        var result = await service.AnalyzeSentimentAsync("Test Company", "TEST");

        Assert.NotNull(result);
        Assert.True(result.PositiveCount > 0);
        Assert.True(result.SentimentScore > 0);
        Assert.Equal(SentimentType.Bullish, result.OverallSentiment);
    }
}
