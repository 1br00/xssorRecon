namespace Xss0rSaaS.App.Services;

public class JwtOptions
{
    public string Issuer { get; init; } = "xss0r-saas";
    public string Audience { get; init; } = "xss0r-saas-clients";
    public string SigningKey { get; init; } = "super-secret-key-change-in-production-123456789";
    public int ExpiryMinutes { get; init; } = 60;
}
