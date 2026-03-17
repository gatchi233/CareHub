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
public sealed class ObservationsController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public ObservationsController(CareHubDbContext db) => _db = db;

    // GET api/observations
    [HttpGet]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.GeneralCareStaff},{Roles.Observer}")]
    public async Task<ActionResult<List<Observation>>> GetAll()
    {
        var query = _db.Observations.AsNoTracking().AsQueryable();
        if (User.IsInRole(Roles.Observer))
        {
            var residentIdText = User.FindFirstValue("resident_id");
            if (!Guid.TryParse(residentIdText, out var residentId))
                return Forbid();
            query = query.Where(o => o.ResidentId == residentId);
        }
        return await query.ToListAsync();
    }

    // GET api/observations/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.GeneralCareStaff},{Roles.Observer}")]
    public async Task<ActionResult<Observation>> GetById(Guid id)
    {
        var obs = await _db.Observations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (obs is not null && User.IsInRole(Roles.Observer))
        {
            var residentIdText = User.FindFirstValue("resident_id");
            if (!Guid.TryParse(residentIdText, out var residentClaimId))
                return Forbid();
            if (obs.ResidentId != residentClaimId)
                return Forbid();
        }

        return obs is null ? NotFound() : Ok(obs);
    }

    // GET api/observations/by-resident/{residentId}
    [HttpGet("by-resident/{residentId:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.GeneralCareStaff},{Roles.Observer}")]
    public async Task<ActionResult<List<Observation>>> GetByResidentId(Guid residentId)
    {
        if (User.IsInRole(Roles.Observer))
        {
            var residentIdText = User.FindFirstValue("resident_id");
            if (!Guid.TryParse(residentIdText, out var residentClaimId))
                return Forbid();
            if (residentId != residentClaimId)
                return Forbid();
        }

        var list = await _db.Observations.AsNoTracking()
            .Where(x => x.ResidentId == residentId)
            .OrderByDescending(x => x.RecordedAt)
            .ToListAsync();

        return Ok(list);
    }

    // POST api/observations
    [HttpPost]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.GeneralCareStaff}")]
    public async Task<ActionResult<Observation>> Create([FromBody] Observation item, CancellationToken ct)
    {
        if (item.Id == Guid.Empty)
            item.Id = Guid.NewGuid();

        if (item.RecordedAt == default)
            item.RecordedAt = DateTime.UtcNow;

        _db.Observations.Add(item);
        await _db.SaveChangesAsync(ct);

        return Ok(item);
    }

    // PUT api/observations/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.GeneralCareStaff}")]
    public async Task<IActionResult> Update(Guid id, Observation updated)
    {
        if (id != updated.Id)
            return BadRequest();

        var existing = await _db.Observations.FindAsync(id);
        if (existing is null)
        {
            updated.Id = id;
            _db.Observations.Add(updated);
        }
        else
        {
            existing.Type = updated.Type;
            existing.Value = updated.Value;
            existing.RecordedBy = updated.RecordedBy;
            existing.ResidentName = updated.ResidentName;
            existing.RecordedAt = updated.RecordedAt;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/observations/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.GeneralCareStaff}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Observations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity == null) return NotFound();

        _db.Observations.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
