using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareHub.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CareHub.Api.Data;

public sealed class JwtTokenService
{
    private readonly AuthOptions _options;
    private readonly CareHubDbContext _db;

    public JwtTokenService(IOptions<AuthOptions> options, CareHubDbContext db)
    {
        _options = options.Value;
        _db = db;
    }

    public async Task<AuthUser?> TryValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        var dbUser = await _db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == username.ToLower() &&
                u.Password == password, ct);

        if (dbUser is not null)
        {
            return new AuthUser
            {
                Username = dbUser.Username,
                Password = dbUser.Password,
                Role = dbUser.Role,
                DisplayName = dbUser.DisplayName,
                ResidentId = dbUser.ResidentId
            };
        }

        return _options.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);
    }

    public (string token, DateTimeOffset expiresAtUtc) CreateToken(AuthUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.TokenMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.ResidentId))
            claims.Add(new("resident_id", user.ResidentId));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
