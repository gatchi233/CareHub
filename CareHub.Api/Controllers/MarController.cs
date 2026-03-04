using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MarController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public MarController(CareHubDbContext db) => _db = db;

    // GET api/mar?residentId=&fromUtc=&toUtc=&includeVoided=false
    [HttpGet]
    public async Task<ActionResult<List<MarEntry>>> GetAll(
        [FromQuery] Guid? residentId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] bool includeVoided = false,
        CancellationToken ct = default)
    {
        var query = _db.MarEntries.AsNoTracking().AsQueryable();

        if (residentId.HasValue)
            query = query.Where(m => m.ResidentId == residentId.Value);

        if (fromUtc.HasValue)
            query = query.Where(m => m.AdministeredAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(m => m.AdministeredAtUtc <= toUtc.Value);

        if (!includeVoided)
            query = query.Where(m => !m.IsVoided);

        var list = await query
            .OrderByDescending(m => m.AdministeredAtUtc)
            .ToListAsync(ct);

        return Ok(list);
    }

    // GET api/mar/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MarEntry>> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _db.MarEntries.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return entry is null ? NotFound() : Ok(entry);
    }

    // POST api/mar
    [HttpPost]
    public async Task<ActionResult<MarEntry>> Create(
        [FromBody] CreateMarEntryRequest request,
        CancellationToken ct)
    {
        // Idempotency: check if ClientRequestId already exists
        var existing = await _db.MarEntries.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ClientRequestId == request.ClientRequestId, ct);

        if (existing is not null)
            return Ok(existing);

        // Validate FKs up front to return clean errors instead of DB exceptions
        if (!await _db.Residents.AnyAsync(r => r.Id == request.ResidentId, ct))
            return NotFound($"Resident {request.ResidentId} not found.");

        var med = await _db.Medications.FindAsync(new object[] { request.MedicationId }, ct);
        if (med is null)
            return NotFound($"Medication {request.MedicationId} not found.");

        var now = DateTimeOffset.UtcNow;

        var entry = new MarEntry
        {
            ClientRequestId = request.ClientRequestId,
            ResidentId = request.ResidentId,
            MedicationId = request.MedicationId,
            Status = request.Status,
            DoseQuantity = request.DoseQuantity,
            DoseUnit = request.DoseUnit,
            AdministeredAtUtc = request.AdministeredAtUtc,
            ScheduledForUtc = request.ScheduledForUtc,
            Notes = request.Notes,
            RecordedBy = request.RecordedBy,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        if (entry.Status == "Given")
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                if (med.StockQuantity < entry.DoseQuantity)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict($"Insufficient stock. Available: {med.StockQuantity}, requested: {entry.DoseQuantity}.");
                }

                _db.MarEntries.Add(entry);

                _db.MedicationInventoryLedgers.Add(new MedicationInventoryLedger
                {
                    MedicationId = entry.MedicationId,
                    MarEntryId = entry.Id,
                    ChangeQty = -entry.DoseQuantity,
                    Unit = entry.DoseUnit,
                    Reason = "MAR_GIVEN",
                    CreatedAtUtc = now,
                });

                med.StockQuantity -= entry.DoseQuantity;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        else
        {
            _db.MarEntries.Add(entry);
            await _db.SaveChangesAsync(ct);
        }

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    // POST api/mar/{id}/void
    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(
        Guid id,
        [FromBody] VoidMarEntryRequest request,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var entry = await _db.MarEntries.FindAsync(new object[] { id }, ct);
            if (entry is null)
            {
                await tx.RollbackAsync(ct);
                return NotFound();
            }

            if (entry.IsVoided)
            {
                await tx.RollbackAsync(ct);
                return BadRequest("Entry is already voided.");
            }

            var now = DateTimeOffset.UtcNow;

            entry.IsVoided = true;
            entry.VoidedAtUtc = now;
            entry.VoidReason = request.Reason;
            entry.UpdatedAtUtc = now;

            // If it was "Given", restore inventory atomically
            if (entry.Status == "Given")
            {
                var existingLedger = await _db.MedicationInventoryLedgers
                    .FirstOrDefaultAsync(l => l.MarEntryId == entry.Id, ct);

                if (existingLedger is null)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict("Cannot void: no inventory ledger found for this entry.");
                }

                if (existingLedger.ChangeQty >= 0)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict("Cannot void: inventory deduction already reversed.");
                }

                var med = await _db.Medications.FindAsync(new object[] { entry.MedicationId }, ct);
                if (med is null)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict("Cannot void: medication record not found.");
                }

                med.StockQuantity += entry.DoseQuantity;
                existingLedger.ChangeQty = 0;
                existingLedger.Reason = "MAR_VOID_REVERSED";
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(entry);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
