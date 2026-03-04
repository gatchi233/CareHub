using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareHub.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly CareHubDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(CareHubDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public sealed record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid username or password" });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expMinutes = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "480");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("displayName", user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds);

        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var username = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                    ?? User.FindFirstValue(ClaimTypes.Name);
        var displayName = User.FindFirstValue("displayName");
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new { username, displayName, role });
    }
}
