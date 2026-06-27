using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nifty50.Core.Entities;
using Nifty50.Infrastructure.Data;

using Nifty50.Core.Enums;

namespace Nifty50.Infrastructure.Services;

public class PushNotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly HttpClient _httpClient;

    public PushNotificationService(AppDbContext db, ILogger<PushNotificationService> logger)
    {
        _db = db;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task SendAlertNotificationsAsync(List<StockAnalysis> newAlerts)
    {
        if (!newAlerts.Any()) return;

        var tokens = await _db.DeviceRegistrations
            .Select(d => d.ExpoPushToken)
            .Distinct()
            .ToListAsync();

        if (!tokens.Any())
        {
            _logger.LogInformation("New alerts generated, but no devices registered for push notifications.");
            return;
        }

        var strongBuys = newAlerts.Where(a => a.OverallSignal == SignalType.StrongBuy || a.OverallSignal == SignalType.Buy).ToList();
        
        if (!strongBuys.Any()) return; // Only notify for Buy/Strong Buy

        var messages = new List<object>();

        foreach (var token in tokens)
        {
            foreach (var alert in strongBuys)
            {
                var body = alert.OverallSignal == SignalType.StrongBuy 
                    ? $"{alert.Stock.Symbol} scored {alert.OverallScore} — Strong Buy signal detected!" 
                    : $"{alert.Stock.Symbol} is showing a Buy signal (Score: {alert.OverallScore}).";

                messages.Add(new
                {
                    to = token,
                    title = "🚀 New Stock Alert",
                    body = body,
                    data = new { stockId = alert.StockId.ToString(), symbol = alert.Stock.Symbol }
                });
            }
        }

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(messages), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://exp.host/--/api/v2/push/send", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent {Count} push notifications via Expo.", messages.Count);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send Expo push notifications. Status: {Status}, Error: {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending push notifications.");
        }
    }
}
