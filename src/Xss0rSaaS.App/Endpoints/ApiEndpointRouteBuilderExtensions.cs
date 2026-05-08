using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xss0rSaaS.App.Contracts;
using Xss0rSaaS.App.Data;
using Xss0rSaaS.App.Domain;
using Xss0rSaaS.App.Services;

namespace Xss0rSaaS.App.Endpoints;

public static class ApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSaasApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        var auth = api.MapGroup("/auth");
        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/login", LoginAsync);

        api.MapGet("/dashboard/me", GetDashboardAsync).RequireAuthorization();
        api.MapPost("/apikeys/rotate", RotateApiKeyAsync).RequireAuthorization();
        api.MapPost("/coupons/redeem", RedeemCouponAsync).RequireAuthorization();
        api.MapGet("/downloads", GetDownloadsAsync).RequireAuthorization();
        api.MapGet("/downloads/{id:guid}/signed", GetSignedDownloadLinkAsync).RequireAuthorization();
        api.MapGet("/downloads/file", ResolveSignedDownloadAsync);

        var admin = api.MapGroup("/admin")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
        admin.MapGet("/users", ListUsersAsync);
        admin.MapPost("/coupons", CreateCouponAsync);
        admin.MapPost("/plans", CreatePlanAsync);
        admin.MapPost("/releases", CreateReleaseAsync);
        admin.MapPatch("/users/{id:guid}/ban", ToggleBanAsync);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        ApplicationDbContext db,
        JwtTokenService tokenService)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email))
        {
            return Results.BadRequest("User with this email already exists.");
        }

        var user = new AppUser
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        var starterPlan = await db.Plans
            .OrderBy(x => x.PriceMonthly)
            .FirstOrDefaultAsync(x => x.IsActive);

        if (starterPlan is not null)
        {
            db.Licenses.Add(new License
            {
                UserId = user.Id,
                PlanId = starterPlan.Id,
                LicenseKey = $"xss0r-{Guid.NewGuid():N}",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(starterPlan.DurationDays),
                IsActive = true
            });
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, expiresAtUtc) = tokenService.Generate(user);
        return Results.Ok(new AuthResponse(token, expiresAtUtc, user.Email, user.Role));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ApplicationDbContext db,
        JwtTokenService tokenService)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (user is null || user.IsBanned)
        {
            return Results.Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var (token, expiresAtUtc) = tokenService.Generate(user);
        return Results.Ok(new AuthResponse(token, expiresAtUtc, user.Email, user.Role));
    }

    private static async Task<IResult> GetDashboardAsync(
        ClaimsPrincipal principal,
        ApplicationDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var user = await db.Users
            .Include(x => x.Licenses)
            .Include(x => x.ApiKeys)
            .Include(x => x.ScanHistory)
            .SingleOrDefaultAsync(x => x.Id == userId.Value);

        if (user is null)
        {
            return Results.NotFound();
        }

        var activeLicense = user.Licenses
            .Where(x => x.IsActive && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.ExpiresAtUtc)
            .FirstOrDefault();

        return Results.Ok(new DashboardResponse(
            Email: user.Email,
            Role: user.Role,
            ActiveLicenseKey: activeLicense?.LicenseKey,
            LicenseExpiresAtUtc: activeLicense?.ExpiresAtUtc,
            ActiveApiKeys: user.ApiKeys.Count(x => !x.IsRevoked),
            ScanHistoryEntries: user.ScanHistory.Count));
    }

    private static async Task<IResult> RotateApiKeyAsync(
        ClaimsPrincipal principal,
        ApplicationDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var activeKeys = await db.ApiKeys
            .Where(x => x.UserId == userId.Value && !x.IsRevoked)
            .ToListAsync();

        foreach (var key in activeKeys)
        {
            key.IsRevoked = true;
            key.RevokedAtUtc = DateTime.UtcNow;
        }

        var rawToken = $"xss0r_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
        var prefix = rawToken[..12];
        var keyHash = Sha256(rawToken);

        db.ApiKeys.Add(new ApiKey
        {
            UserId = userId.Value,
            Prefix = prefix,
            KeyHash = keyHash
        });

        await db.SaveChangesAsync();
        return Results.Ok(new { apiKey = rawToken });
    }

    private static async Task<IResult> RedeemCouponAsync(
        RedeemCouponRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var coupon = await db.CouponCodes.SingleOrDefaultAsync(x => x.Code == code && x.IsActive);
        if (coupon is null)
        {
            return Results.BadRequest("Coupon code is invalid.");
        }

        if (coupon.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Results.BadRequest("Coupon code has expired.");
        }

        if (coupon.UsedCount >= coupon.MaxUses)
        {
            return Results.BadRequest("Coupon maximum usage reached.");
        }

        var alreadyRedeemed = await db.CouponRedemptions
            .AnyAsync(x => x.CouponCodeId == coupon.Id && x.UserId == userId.Value);
        if (alreadyRedeemed)
        {
            return Results.BadRequest("This user already redeemed this coupon.");
        }

        coupon.UsedCount++;
        db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponCodeId = coupon.Id,
            UserId = userId.Value
        });

        await db.SaveChangesAsync();
        return Results.Ok(new { message = $"Coupon applied: {coupon.PercentOff}% off." });
    }

    private static async Task<IResult> GetDownloadsAsync(ApplicationDbContext db)
    {
        var releases = await db.DownloadReleases
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ReleaseDto(
                x.Id,
                x.Version,
                x.Changelog,
                x.IsActive,
                x.CreatedAtUtc))
            .ToListAsync();

        return Results.Ok(releases);
    }

    private static async Task<IResult> GetSignedDownloadLinkAsync(
        Guid id,
        string platform,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var hasActiveLicense = await db.Licenses.AnyAsync(x =>
            x.UserId == userId.Value &&
            x.IsActive &&
            x.ExpiresAtUtc > DateTime.UtcNow);
        if (!hasActiveLicense)
        {
            return Results.BadRequest("No active license found.");
        }

        var release = await db.DownloadReleases.SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (release is null)
        {
            return Results.NotFound("Release not found.");
        }

        var normalizedPlatform = platform.Trim().ToLowerInvariant() switch
        {
            "linux" => "linux",
            _ => "windows"
        };

        var expires = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var payload = $"{id}:{userId}:{normalizedPlatform}:{expires}";
        var secret = configuration["DownloadSigning:Secret"] ?? "dev-download-signing-secret";
        var signature = SignPayload(payload, secret);

        var url = $"/api/downloads/file?rid={id}&uid={userId}&platform={normalizedPlatform}&exp={expires}&sig={signature}";
        return Results.Ok(new { url, expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expires).UtcDateTime });
    }

    private static async Task<IResult> ResolveSignedDownloadAsync(
        Guid rid,
        Guid uid,
        string platform,
        long exp,
        string sig,
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (exp < now)
        {
            return Results.BadRequest("Download link expired.");
        }

        var normalizedPlatform = platform.Trim().ToLowerInvariant() switch
        {
            "linux" => "linux",
            _ => "windows"
        };

        var payload = $"{rid}:{uid}:{normalizedPlatform}:{exp}";
        var secret = configuration["DownloadSigning:Secret"] ?? "dev-download-signing-secret";
        var expected = SignPayload(payload, secret);

        if (!SlowEquals(expected, sig))
        {
            return Results.Unauthorized();
        }

        var release = await db.DownloadReleases.SingleOrDefaultAsync(x => x.Id == rid && x.IsActive);
        if (release is null)
        {
            return Results.NotFound("Release no longer available.");
        }

        var target = normalizedPlatform == "linux" ? release.LinuxFileUrl : release.WindowsFileUrl;
        return Results.Redirect(target);
    }

    private static async Task<IResult> ListUsersAsync(ApplicationDbContext db)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new UserDto(
                x.Id,
                x.Email,
                x.Role,
                x.IsBanned,
                x.CreatedAtUtc))
            .ToListAsync();
        return Results.Ok(users);
    }

    private static async Task<IResult> CreateCouponAsync(CreateCouponRequest request, ApplicationDbContext db)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var exists = await db.CouponCodes.AnyAsync(x => x.Code == normalizedCode);
        if (exists)
        {
            return Results.BadRequest("Coupon already exists.");
        }

        var coupon = new CouponCode
        {
            Code = normalizedCode,
            PercentOff = request.PercentOff,
            MaxUses = request.MaxUses,
            ExpiresAtUtc = request.ExpiresAtUtc.ToUniversalTime(),
            IsActive = true
        };

        db.CouponCodes.Add(coupon);
        await db.SaveChangesAsync();
        return Results.Ok(coupon.Id);
    }

    private static async Task<IResult> CreatePlanAsync(CreatePlanRequest request, ApplicationDbContext db)
    {
        var plan = new Plan
        {
            Name = request.Name.Trim(),
            PriceMonthly = request.PriceMonthly,
            DurationDays = request.DurationDays,
            MaxActivations = request.MaxActivations
        };

        db.Plans.Add(plan);
        await db.SaveChangesAsync();
        return Results.Ok(plan.Id);
    }

    private static async Task<IResult> CreateReleaseAsync(CreateReleaseRequest request, ApplicationDbContext db)
    {
        var release = new DownloadRelease
        {
            Version = request.Version.Trim(),
            Changelog = request.Changelog.Trim(),
            WindowsFileUrl = request.WindowsFileUrl.Trim(),
            LinuxFileUrl = request.LinuxFileUrl.Trim(),
            IsActive = request.IsActive
        };

        db.DownloadReleases.Add(release);
        await db.SaveChangesAsync();
        return Results.Ok(release.Id);
    }

    private static async Task<IResult> ToggleBanAsync(Guid id, ApplicationDbContext db)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return Results.NotFound();
        }

        user.IsBanned = !user.IsBanned;
        await db.SaveChangesAsync();
        return Results.Ok(new { user.Id, user.Email, user.IsBanned });
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(ClaimTypes.Name);
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }

    private static string SignPayload(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Sha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool SlowEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < leftBytes.Length; i++)
        {
            diff |= leftBytes[i] ^ rightBytes[i];
        }

        return diff == 0;
    }
}
