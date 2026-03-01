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
            .Where(u => !string.Equals(u.Role, Roles.Resident, StringComparison.OrdinalIgnoreCase))
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
            user.Password = request.Password;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

public sealed class UpdateStaffRequest
{
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
}
