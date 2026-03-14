using System.Net.Http.Headers;

namespace CareHub.Mobile.Services;

/// <summary>
/// Attaches the JWT Bearer token from MobileAuthService to every outgoing HTTP request.
/// </summary>
public sealed class MobileAuthTokenHandler : DelegatingHandler
{
    private readonly MobileAuthService _auth;

    public MobileAuthTokenHandler(MobileAuthService auth)
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
