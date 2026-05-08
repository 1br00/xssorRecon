namespace Xss0rSaaS.App.Domain;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsBanned { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<License> Licenses { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<CouponRedemption> CouponRedemptions { get; set; } = [];
    public ICollection<ScanHistory> ScanHistory { get; set; } = [];
}
