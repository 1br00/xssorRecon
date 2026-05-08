namespace Xss0rSaaS.App.Contracts;

public sealed record ReleaseDto(
    Guid Id,
    string Version,
    string Changelog,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record UserDto(
    Guid Id,
    string Email,
    string Role,
    bool IsBanned,
    DateTime CreatedAtUtc);
