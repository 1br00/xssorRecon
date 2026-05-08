using Microsoft.EntityFrameworkCore;
using Xss0rSaaS.App.Domain;

namespace Xss0rSaaS.App.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<CouponCode> CouponCodes => Set<CouponCode>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<DownloadRelease> DownloadReleases => Set<DownloadRelease>();
    public DbSet<ScanHistory> ScanHistory => Set<ScanHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .Property(x => x.Email)
            .HasMaxLength(255);

        modelBuilder.Entity<License>()
            .HasIndex(x => x.LicenseKey)
            .IsUnique();

        modelBuilder.Entity<CouponCode>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<ApiKey>()
            .HasIndex(x => x.KeyHash)
            .IsUnique();

        modelBuilder.Entity<CouponRedemption>()
            .HasIndex(x => new { x.CouponCodeId, x.UserId })
            .IsUnique();
    }
}
