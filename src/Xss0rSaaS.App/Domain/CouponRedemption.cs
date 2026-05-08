namespace Xss0rSaaS.App.Domain;

public class CouponRedemption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CouponCodeId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RedeemedAtUtc { get; set; } = DateTime.UtcNow;

    public CouponCode CouponCode { get; set; } = default!;
    public AppUser User { get; set; } = default!;
}
