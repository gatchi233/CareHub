using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class StaffController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public StaffController(CareHubDbContext db)
    {
        _db = db;
    }

    // GET api/staff
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Observer}")]
    public async Task<ActionResult<List<object>>> GetAll(CancellationToken ct)
    {
        var list = await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Role != Roles.Resident)
            .Select(u => new
            {
                username = u.Username,
                displayName = u.DisplayName,
                role = u.Role
            })
            .ToListAsync(ct);

        return Ok(list.Cast<object>().ToList());
    }

    // PUT api/staff/{username}
    [HttpPut("{username}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(string username, [FromBody] UpdateStaffRequest request, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == username.ToLower(), ct);
        if (user is null)
            return NotFound();

        var role = (request.Role ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(role))
        {
            var allowed = new[] { Roles.Admin, Roles.Staff, Roles.Observer };
            if (!allowed.Contains(role, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Role must be Admin, Staff, or Observer.");

            user.Role = role;
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = request.Password;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
    // POST api/staff
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var allowed = new[] { Roles.Admin, Roles.Staff, Roles.Observer };
        var role = (request.Role ?? "").Trim();
        if (!allowed.Contains(role, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Role must be Admin, Staff, or Observer.");

        var exists = await _db.AppUsers.AnyAsync(
            u => u.Username.ToLower() == request.Username.Trim().ToLower(), ct);
        if (exists)
            return Conflict("A user with that username already exists.");

        var user = new AppUser
        {
            Username = request.Username.Trim(),
            PasswordHash = request.Password,
            DisplayName = (request.DisplayName ?? request.Username).Trim(),
            Role = role,
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), null,
            new { username = user.Username, displayName = user.DisplayName, role = user.Role });
    }

    // DELETE api/staff/{username}
    [HttpDelete("{username}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string username, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(
            u => u.Username.ToLower() == username.ToLower(), ct);
        if (user is null)
            return NotFound();

        // Prevent deleting yourself
        var currentUsername = User.Identity?.Name;
        if (string.Equals(user.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
            return BadRequest("You cannot delete your own account.");

        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

public sealed class CreateStaffRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "";
}

public sealed class UpdateStaffRequest
{
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
}
