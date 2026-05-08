using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xss0rSaaS.App.Contracts;

namespace Xss0rSaaS.App.Services;

public class ApiClientService(IHttpClientFactory httpClientFactory, ClientSessionState sessionState)
{
    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        using var client = httpClientFactory.CreateClient("ApiClient");
        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        using var client = httpClientFactory.CreateClient("ApiClient");
        var response = await client.PostAsJsonAsync("/api/auth/login", request);
        if (!response.IsSuccessStatusCode)
        {
            return (false, await response.Content.ReadAsStringAsync());
        }

        var authResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (authResult is null)
        {
            return (false, "Login response was empty.");
        }

        sessionState.AccessToken = authResult.AccessToken;
        sessionState.Email = authResult.Email;
        sessionState.Role = authResult.Role;
        return (true, null);
    }

    public async Task<DashboardResponse?> GetDashboardAsync()
    {
        using var client = CreateAuthorizedClient();
        return await client.GetFromJsonAsync<DashboardResponse>("/api/dashboard/me");
    }

    public async Task<IReadOnlyList<ReleaseDto>> GetReleasesAsync()
    {
        using var client = CreateAuthorizedClient();
        var data = await client.GetFromJsonAsync<IReadOnlyList<ReleaseDto>>("/api/downloads");
        return data ?? [];
    }

    public async Task<(bool Success, string? Error)> RedeemCouponAsync(string code)
    {
        using var client = CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/api/coupons/redeem", new RedeemCouponRequest(code));
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? ApiKey, string? Error)> RotateApiKeyAsync()
    {
        using var client = CreateAuthorizedClient();
        var response = await client.PostAsync("/api/apikeys/rotate", null);
        if (!response.IsSuccessStatusCode)
        {
            return (false, null, await response.Content.ReadAsStringAsync());
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiKeyRotateResponse>();
        return payload is null
            ? (false, null, "API returned empty payload.")
            : (true, payload.ApiKey, null);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync()
    {
        using var client = CreateAuthorizedClient();
        var data = await client.GetFromJsonAsync<IReadOnlyList<UserDto>>("/api/admin/users");
        return data ?? [];
    }

    public async Task<(bool Success, string? Error)> CreateCouponAsync(CreateCouponRequest request)
    {
        using var client = CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/api/admin/coupons", request);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> CreateReleaseAsync(CreateReleaseRequest request)
    {
        using var client = CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/api/admin/releases", request);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (false, await response.Content.ReadAsStringAsync());
    }

    private HttpClient CreateAuthorizedClient()
    {
        var client = httpClientFactory.CreateClient("ApiClient");
        if (!string.IsNullOrWhiteSpace(sessionState.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionState.AccessToken);
        }

        return client;
    }

    private sealed record ApiKeyRotateResponse(string ApiKey);
}
