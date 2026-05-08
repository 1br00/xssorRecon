using System.ComponentModel.DataAnnotations;

namespace Xss0rSaaS.App.Contracts;

public sealed record CreatePlanRequest(
    [property: Required, MinLength(2)] string Name,
    decimal PriceMonthly,
    [property: Range(1, 3650)] int DurationDays,
    [property: Range(1, int.MaxValue)] int MaxActivations);
