namespace Xss0rSaaS.App.Contracts;

public sealed record DashboardResponse(
    string Email,
    string Role,
    string? ActiveLicenseKey,
    DateTime? LicenseExpiresAtUtc,
    int ActiveApiKeys,
    int ScanHistoryEntries);
