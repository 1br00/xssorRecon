namespace Xss0rSaaS.App.Domain;

public class ScanHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Target { get; set; } = string.Empty;
    public int UrlsScanned { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string Notes { get; set; } = string.Empty;

    public AppUser User { get; set; } = default!;
}
