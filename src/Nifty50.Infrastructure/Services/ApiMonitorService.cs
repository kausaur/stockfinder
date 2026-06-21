using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Nifty50.Core.DTOs;
using Nifty50.Core.Interfaces;
using Nifty50.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Nifty50.Infrastructure.Services;

public class ApiMonitorService : IApiMonitorService
{
    private static readonly ConcurrentBag<ApiCallRecord> _calls = new();
    private static readonly DateTime _serverStartedAt = DateTime.UtcNow;
    private readonly IServiceProvider _services;

    public ApiMonitorService(IServiceProvider services) => _services = services;

    public void RecordApiCall(ApiCallRecord record) => _calls.Add(record);

    public List<ApiCallRecord> GetRecentCalls(string? apiName, int limit = 50)
    {
        var q = _calls.AsEnumerable();
        if (!string.IsNullOrEmpty(apiName))
            q = q.Where(c => c.ApiName.Equals(apiName, StringComparison.OrdinalIgnoreCase));
        return q.OrderByDescending(c => c.CalledAt).Take(limit).ToList();
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var allCalls = _calls.ToArray();
        var groups = allCalls.GroupBy(c => c.ApiName);
        var health = groups.Select(g => new ApiHealthDto(
            g.Key,
            g.Count(),
            g.Count(c => c.StatusCode >= 200 && c.StatusCode < 300),
            g.Count(c => c.StatusCode >= 400),
            g.Average(c => c.LatencyMs),
            g.Max(c => c.CalledAt),
            g.Where(c => c.StatusCode >= 400).OrderByDescending(c => c.CalledAt).FirstOrDefault()?.CalledAt,
            g.Where(c => c.StatusCode >= 400).OrderByDescending(c => c.CalledAt).FirstOrDefault()?.ErrorMessage,
            g.OrderByDescending(c => c.CalledAt).Take(20).ToList()
        )).ToList();

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var totalStocks = await db.Stocks.CountAsync();
        var lastRefresh = await db.StockPrices.MaxAsync(p => (DateTime?)p.CreatedAt);

        return new AdminDashboardDto(health, _serverStartedAt, totalStocks, lastRefresh);
    }
}
