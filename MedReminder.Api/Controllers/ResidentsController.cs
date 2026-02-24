using MedReminder.Api.Data;
using MedReminder.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedReminder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ResidentsController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public ResidentsController(CareHubDbContext db) => _db = db;

    // GET api/residents
    [HttpGet]
    public async Task<ActionResult<List<Resident>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Residents
            .AsNoTracking()
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToListAsync(ct);

        return Ok(items);
    }

    // POST api/residents
    [HttpPost]
    public async Task<ActionResult<Resident>> Create([FromBody] Resident resident, CancellationToken ct)
    {
        // If client sends empty Guid, generate one
        if (resident.Id == Guid.Empty)
            resident.Id = Guid.NewGuid();

        _db.Residents.Add(resident);
        await _db.SaveChangesAsync(ct);

        return Ok(resident);
    }

    // PUT api/residents/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Resident resident, CancellationToken ct)
    {
        if (id != resident.Id)
            return BadRequest("Route id does not match resident.Id");

        var exists = await _db.Residents.AnyAsync(r => r.Id == id, ct);
        if (!exists) return NotFound();

        _db.Entry(resident).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // DELETE api/residents/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Residents.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity == null) return NotFound();

        _db.Residents.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}