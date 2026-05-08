namespace Xss0rSaaS.App.Services;

public class ClientSessionState
{
    public string? AccessToken { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public void Clear()
    {
        AccessToken = null;
        Email = null;
        Role = null;
    }
}
