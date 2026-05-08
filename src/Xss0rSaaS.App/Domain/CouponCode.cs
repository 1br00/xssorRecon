namespace Xss0rSaaS.App.Domain;

public class CouponCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public int PercentOff { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CouponRedemption> Redemptions { get; set; } = [];
}
