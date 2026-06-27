namespace Nifty50.Core.Entities;

public class DeviceRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExpoPushToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
}
