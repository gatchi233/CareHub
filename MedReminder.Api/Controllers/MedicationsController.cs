using MedReminder.Api.Data;
using MedReminder.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedReminder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MedicationsController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public MedicationsController(CareHubDbContext db) => _db = db;

    // GET api/medications
    [HttpGet]
    public async Task<ActionResult<List<Medication>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Medications
            .AsNoTracking()
            .OrderBy(m => m.MedName)
            .ToListAsync(ct);

        return Ok(list);
    }

    // GET api/medications/lowstock
    [HttpGet("lowstock")]
    public async Task<ActionResult<List<Medication>>> GetLowStock(CancellationToken ct)
    {
        var list = await _db.Medications
            .AsNoTracking()
            // inventory meds: ResidentId is null OR Guid.Empty
            .Where(m => m.ResidentId == null || m.ResidentId == Guid.Empty)
            .Where(m => m.StockQuantity <= m.ReorderLevel)
            .OrderBy(m => m.MedName)
            .ToListAsync(ct);

        return Ok(list);
    }

    // POST api/medications
    [HttpPost]
    public async Task<ActionResult<Medication>> Create([FromBody] Medication med, CancellationToken ct)
    {
        if (med.Id == Guid.Empty)
            med.Id = Guid.NewGuid();

        NormalizeExpiryDate(med);

        _db.Medications.Add(med);
        await _db.SaveChangesAsync(ct);

        return Ok(med);
    }

    // PUT api/medications/{id}  — upsert (create if not found)
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Medication med, CancellationToken ct)
    {
        if (id != med.Id)
            return BadRequest("Route id does not match med.Id");

        NormalizeExpiryDate(med);

        var exists = await _db.Medications.AnyAsync(m => m.Id == id, ct);
        if (exists)
        {
            _db.Entry(med).State = EntityState.Modified;
        }
        else
        {
            _db.Medications.Add(med);
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Npgsql requires UTC offset for timestamp with time zone columns
    private static void NormalizeExpiryDate(Medication med)
    {
        if (med.ExpiryDate.Offset != TimeSpan.Zero)
            med.ExpiryDate = med.ExpiryDate.ToUniversalTime();
    }

    // DELETE api/medications/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity == null) return NotFound();

        _db.Medications.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // POST api/medications/{id}/adjustStock?delta=...
    [HttpPost("{id:guid}/adjustStock")]
    public async Task<IActionResult> AdjustStock(Guid id, [FromQuery] int delta, CancellationToken ct)
    {
        var med = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (med == null) return NotFound();

        med.StockQuantity += delta;
        if (med.StockQuantity < 0)
            med.StockQuantity = 0;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}