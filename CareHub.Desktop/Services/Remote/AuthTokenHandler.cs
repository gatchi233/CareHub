using System.Net.Http.Headers;
using CareHub.Services;

namespace CareHub.Services.Remote;

/// <summary>
/// Attaches the JWT Bearer token from AuthService to every outgoing HTTP request.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly AuthService _auth;

    public AuthTokenHandler(AuthService auth)
    {
        _auth = auth;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _auth.AccessToken;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
