using System.Security.Claims;
using CareHub.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtTokenService _tokens;

    public AuthController(JwtTokenService tokens)
    {
        _tokens = tokens;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _tokens.TryValidateCredentialsAsync(request.Username, request.Password, ct);
        if (user is null)
            return Unauthorized(new { message = "Invalid username or password." });

        var (token, expiresAtUtc) = _tokens.CreateToken(user);
        return Ok(new
        {
            accessToken = token,
            tokenType = "Bearer",
            expiresAtUtc,
            role = user.Role,
            displayName = user.DisplayName,
            username = user.Username,
            residentId = user.ResidentId
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            username = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name),
            displayName = User.FindFirstValue(ClaimTypes.Name),
            role = User.FindFirstValue(ClaimTypes.Role),
            residentId = User.FindFirstValue("resident_id")
        });
    }
}

public sealed class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
