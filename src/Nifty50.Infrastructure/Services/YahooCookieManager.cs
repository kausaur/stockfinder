using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Nifty50.Infrastructure.Services;

public interface IYahooCookieManager
{
    Task<(string Cookie, string Crumb)> GetCookieAndCrumbAsync(CancellationToken ct = default);
}

public class YahooCookieManager : IYahooCookieManager
{
    private readonly HttpClient _http;
    private readonly ILogger<YahooCookieManager> _logger;
    private string? _cachedCookie;
    private string? _cachedCrumb;
    private DateTime _lastFetched;

    public YahooCookieManager(HttpClient http, ILogger<YahooCookieManager> logger)
    {
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        _http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
    }

    public async Task<(string Cookie, string Crumb)> GetCookieAndCrumbAsync(CancellationToken ct = default)
    {
        // Cache for 6 hours
        if (_cachedCookie != null && _cachedCrumb != null && DateTime.UtcNow - _lastFetched < TimeSpan.FromHours(6))
        {
            return (_cachedCookie, _cachedCrumb);
        }

        try
        {
            _logger.LogInformation("Fetching new Yahoo Finance Cookie and Crumb...");
            
            // 1. Get Cookie from fc.yahoo.com or finance.yahoo.com
            var cookieUrl = "https://fc.yahoo.com/";
            var cookieReq = new HttpRequestMessage(HttpMethod.Get, cookieUrl);
            var cookieRes = await _http.SendAsync(cookieReq, ct);
            // It might return 404/502 but still set cookies!
            
            var cookie = "";
            if (cookieRes.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var c in cookies)
                {
                    if (c.Contains("A3="))
                    {
                        cookie = c.Split(';')[0]; // Extract the A3=... part
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(cookie))
            {
                // Fallback to finance homepage
                cookieRes = await _http.GetAsync("https://finance.yahoo.com", ct);
                if (cookieRes.Headers.TryGetValues("Set-Cookie", out var cookies2))
                {
                    foreach (var c in cookies2)
                    {
                        if (c.Contains("A3="))
                        {
                            cookie = c.Split(';')[0];
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(cookie))
            {
                _logger.LogWarning("Could not find Yahoo A3 Cookie.");
                return ("", "");
            }

            // 2. Get Crumb using the cookie
            var crumbReq = new HttpRequestMessage(HttpMethod.Get, "https://query1.finance.yahoo.com/v1/test/getcrumb");
            crumbReq.Headers.Add("Cookie", cookie);
            var crumbRes = await _http.SendAsync(crumbReq, ct);
            crumbRes.EnsureSuccessStatusCode();
            
            var crumb = await crumbRes.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrEmpty(crumb))
            {
                _logger.LogWarning("Received empty crumb from Yahoo.");
                return ("", "");
            }

            _cachedCookie = cookie;
            _cachedCrumb = crumb;
            _lastFetched = DateTime.UtcNow;
            
            _logger.LogInformation("Successfully fetched Yahoo Crumb.");
            return (_cachedCookie, _cachedCrumb);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Yahoo Cookie and Crumb.");
            return ("", "");
        }
    }
}
