using MedReminder.Api.Data;
using MedReminder.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedReminder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ObservationsController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public ObservationsController(CareHubDbContext db) => _db = db;

    // GET api/observations
    [HttpGet]
    public async Task<ActionResult<List<Observation>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Observations
            .AsNoTracking()
            .OrderByDescending(o => o.RecordedAt)
            .ToListAsync(ct);

        return Ok(list);
    }

    // GET api/observations/byResident/{residentId}
    [HttpGet("byResident/{residentId:guid}")]
    public async Task<ActionResult<List<Observation>>> GetByResident(Guid residentId, CancellationToken ct)
    {
        var list = await _db.Observations
            .AsNoTracking()
            .Where(o => o.ResidentId == residentId)
            .OrderByDescending(o => o.RecordedAt)
            .ToListAsync(ct);

        return Ok(list);
    }

    // POST api/observations
    [HttpPost]
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
    public async Task<IActionResult> Update(Guid id, Observation updated)
    {
        if (id != updated.Id)
            return BadRequest();

        var existing = await _db.Observations.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.Type = updated.Type;
        existing.Value = updated.Value;
        existing.RecordedBy = updated.RecordedBy;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/observations/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Observations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity == null) return NotFound();

        _db.Observations.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}