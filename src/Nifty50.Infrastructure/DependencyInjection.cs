using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Data;
using Nifty50.Infrastructure.Repositories;
using Nifty50.Infrastructure.Services;
using Polly;

namespace Nifty50.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            var connStr = config.GetConnectionString("DefaultConnection");
            // Add connection pooling parameters if they are not present
            if (connStr != null && !connStr.Contains("Maximum Pool Size"))
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connStr)
                {
                    MaxPoolSize = 5,
                    ConnectionIdleLifetime = 60,
                    Timeout = 15,
                    CommandTimeout = 30
                };
                connStr = builder.ToString();
            }
            options.UseNpgsql(connStr, npgsqlOptions => 
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // Repositories
        services.AddScoped<IStockRepository, StockRepository>();

        // Services
        services.AddScoped<IScoringProfileService, ScoringProfileService>();
        services.AddScoped<ITechnicalAnalysisService, TechnicalAnalysisService>();
        services.AddScoped<IFundamentalAnalysisService, FundamentalAnalysisService>();
        services.AddScoped<IStockAnalysisEngine, StockAnalysisEngine>();
        services.AddScoped<PushNotificationService>();
        services.AddSingleton<IApiMonitorService, ApiMonitorService>();

        // HTTP clients with Polly retry
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        services.AddHttpClient<IYahooCookieManager, YahooCookieManager>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
            .AddPolicyHandler(retryPolicy);
        services.AddHttpClient<IStockDataService, YahooFinanceService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
            .AddPolicyHandler(retryPolicy);
        services.AddHttpClient<IFundamentalDataService, YahooFundamentalsService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
            .AddPolicyHandler(retryPolicy);
        services.AddHttpClient<IStockMetadataService, YahooMetadataService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
            .AddPolicyHandler(retryPolicy);
        services.AddHttpClient<ISentimentService, GNewsSentimentService>()
            .AddPolicyHandler(retryPolicy);

        // Background service
        services.AddSingleton<IDataRefreshService, DataRefreshService>();
        services.AddHostedService(sp => (DataRefreshService)sp.GetRequiredService<IDataRefreshService>());

        return services;
    }
}
