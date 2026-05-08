namespace Xss0rSaaS.App.Domain;

public class DownloadRelease
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Version { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public string WindowsFileUrl { get; set; } = string.Empty;
    public string LinuxFileUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
