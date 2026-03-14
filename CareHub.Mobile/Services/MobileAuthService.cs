using System.Net.Http.Json;
using System.Text.Json;

namespace CareHub.Mobile.Services;

/// <summary>
/// Lightweight auth service for Mobile app. Logs in to the API and stores the JWT token.
/// </summary>
public sealed class MobileAuthService
{
    private readonly HttpClient _http;

    public string? AccessToken { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

    public MobileAuthService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var payload = new { username, password };
            var resp = await _http.PostAsJsonAsync("api/auth/login", payload);

            if (!resp.IsSuccessStatusCode)
                return false;

            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            AccessToken = body.GetProperty("accessToken").GetString();
            return !string.IsNullOrEmpty(AccessToken);
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        AccessToken = null;
    }
}
