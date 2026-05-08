using System.ComponentModel.DataAnnotations;

namespace Xss0rSaaS.App.Contracts;

public sealed record RedeemCouponRequest([property: Required] string Code);

public sealed record CreateCouponRequest(
    [property: Required, MinLength(3)] string Code,
    [property: Range(1, 100)] int PercentOff,
    [property: Range(1, int.MaxValue)] int MaxUses,
    DateTime ExpiresAtUtc);
