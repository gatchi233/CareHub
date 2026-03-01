using CareHub.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class StaffController : ControllerBase
{
    private readonly AuthOptions _auth;

    public StaffController(IOptions<AuthOptions> auth)
    {
        _auth = auth.Value;
    }

    // GET api/staff
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Observer}")]
    public ActionResult<List<object>> GetAll()
    {
        var list = _auth.Users
            .Where(u => !string.Equals(u.Role, Roles.Resident, StringComparison.OrdinalIgnoreCase))
            .Select(u => new
            {
                username = u.Username,
                displayName = u.DisplayName,
                role = u.Role
            })
            .ToList<object>();

        return Ok(list);
    }
}
