using System.ComponentModel.DataAnnotations;

namespace Xss0rSaaS.App.Contracts;

public sealed record CreateReleaseRequest(
    [property: Required] string Version,
    [property: Required] string Changelog,
    [property: Required, Url] string WindowsFileUrl,
    [property: Required, Url] string LinuxFileUrl,
    bool IsActive);
