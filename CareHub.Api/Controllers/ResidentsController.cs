using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ResidentsController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public ResidentsController(CareHubDbContext db) => _db = db;

    // GET api/residents
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Observer},{Roles.Resident}")]
    public async Task<ActionResult<List<Resident>>> GetAll(CancellationToken ct)
    {
        var query = _db.Residents
            .AsNoTracking()
            .OrderBy(r => r.ResidentLName)
            .ThenBy(r => r.ResidentFName)
            .AsQueryable();

        if (User.IsInRole(Roles.Resident))
        {
            var residentIdText = User.FindFirstValue("resident_id");
            if (!Guid.TryParse(residentIdText, out var residentId))
                return Forbid();
            query = query.Where(r => r.Id == residentId);
        }

        var items = await query.ToListAsync(ct);

        return Ok(items);
    }

    // POST api/residents
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
    public async Task<ActionResult<Resident>> Create([FromBody] Resident resident, CancellationToken ct)
    {
        if (resident.Id == Guid.Empty)
            resident.Id = Guid.NewGuid();

        _db.Residents.Add(resident);
        await _db.SaveChangesAsync(ct);

        return Ok(resident);
    }

    // PUT api/residents/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Resident resident, CancellationToken ct)
    {
        if (id != resident.Id)
            return BadRequest("Route id does not match resident.Id");

        var exists = await _db.Residents.AnyAsync(r => r.Id == id, ct);
        if (exists)
            _db.Entry(resident).State = EntityState.Modified;
        else
            _db.Residents.Add(resident);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE api/residents/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Residents.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity == null) return NotFound();

        _db.Residents.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
