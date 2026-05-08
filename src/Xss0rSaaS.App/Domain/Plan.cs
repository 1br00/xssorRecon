namespace Xss0rSaaS.App.Domain;

public class Plan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public int DurationDays { get; set; }
    public int MaxActivations { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<License> Licenses { get; set; } = [];
}
