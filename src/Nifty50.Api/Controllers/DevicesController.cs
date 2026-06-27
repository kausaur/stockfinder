using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nifty50.Core.DTOs;
using Nifty50.Core.Entities;
using Nifty50.Infrastructure.Data;

namespace Nifty50.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DevicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ExpoPushToken))
        {
            return BadRequest(new { message = "ExpoPushToken is required" });
        }

        var existing = await _db.DeviceRegistrations.FirstOrDefaultAsync(d => d.ExpoPushToken == request.ExpoPushToken);

        if (existing != null)
        {
            existing.LastActiveAt = DateTime.UtcNow;
            existing.DeviceName = request.DeviceName;
            existing.Platform = request.Platform;
            _db.DeviceRegistrations.Update(existing);
        }
        else
        {
            var device = new DeviceRegistration
            {
                ExpoPushToken = request.ExpoPushToken,
                DeviceName = request.DeviceName,
                Platform = request.Platform,
                RegisteredAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };
            _db.DeviceRegistrations.Add(device);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Device registered successfully" });
    }
}
