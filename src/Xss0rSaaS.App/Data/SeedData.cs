using Xss0rSaaS.App.Domain;

namespace Xss0rSaaS.App.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db, IConfiguration configuration)
    {
        if (!db.Plans.Any())
        {
            db.Plans.AddRange(
                new Plan
                {
                    Name = "Starter",
                    PriceMonthly = 19,
                    DurationDays = 30,
                    MaxActivations = 1
                },
                new Plan
                {
                    Name = "Pro",
                    PriceMonthly = 59,
                    DurationDays = 30,
                    MaxActivations = 5
                });
        }

        if (!db.DownloadReleases.Any())
        {
            db.DownloadReleases.Add(new DownloadRelease
            {
                Version = "1.0.0",
                Changelog = "Initial SaaS-ready distribution channel for xss0r clients.",
                WindowsFileUrl = "https://example.invalid/releases/xss0r-1.0.0-win.zip",
                LinuxFileUrl = "https://example.invalid/releases/xss0r-1.0.0-linux.tar.gz",
                IsActive = true
            });
        }

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@xss0r.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "ChangeMe123!";
        if (!db.Users.Any(x => x.Email == adminEmail))
        {
            db.Users.Add(new AppUser
            {
                Email = adminEmail.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = "Admin"
            });
        }

        await db.SaveChangesAsync();
    }
}
