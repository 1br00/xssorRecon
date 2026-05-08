namespace Xss0rSaaS.App.Domain;

public class License
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int ResetHwidCount { get; set; }
    public DateTime? LastHwidResetUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = default!;
    public Plan Plan { get; set; } = default!;
}
