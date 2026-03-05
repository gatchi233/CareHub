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

    // ──────────────────── REPORTING ────────────────────

    // GET api/mar/report?fromUtc=&toUtc=&residentId=
    [HttpGet("report")]
    public async Task<ActionResult<MarReport>> GetReport(
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset toUtc,
        [FromQuery] Guid? residentId,
        CancellationToken ct)
    {
        var query = _db.MarEntries.AsNoTracking()
            .Where(m => !m.IsVoided)
            .Where(m => m.AdministeredAtUtc >= fromUtc && m.AdministeredAtUtc <= toUtc);

        if (residentId.HasValue)
            query = query.Where(m => m.ResidentId == residentId.Value);

        var entries = await query.ToListAsync(ct);

        // Lookup resident & medication names
        var residentIds = entries.Select(e => e.ResidentId).Distinct().ToList();
        var medIds = entries.Select(e => e.MedicationId).Distinct().ToList();

        var residentNames = await _db.Residents.AsNoTracking()
            .Where(r => residentIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => (r.ResidentFName + " " + r.ResidentLName).Trim(), ct);

        var medNames = await _db.Medications.AsNoTracking()
            .Where(m => medIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.MedName ?? "Unknown", ct);

        var lines = entries.Select(e => new MarReportLine
        {
            Id = e.Id,
            ResidentId = e.ResidentId,
            ResidentName = residentNames.GetValueOrDefault(e.ResidentId, "Unknown"),
            MedicationId = e.MedicationId,
            MedicationName = medNames.GetValueOrDefault(e.MedicationId, "Unknown"),
            Status = e.Status,
            DoseQuantity = e.DoseQuantity,
            DoseUnit = e.DoseUnit,
            ScheduledForUtc = e.ScheduledForUtc,
            AdministeredAtUtc = e.AdministeredAtUtc,
            RecordedBy = e.RecordedBy,
            Notes = e.Notes,
        }).OrderBy(l => l.ResidentName).ThenBy(l => l.AdministeredAtUtc).ToList();

        var summary = new MarReportSummary
        {
            TotalEntries = lines.Count,
            GivenCount = lines.Count(l => l.Status == "Given"),
            RefusedCount = lines.Count(l => l.Status == "Refused"),
            MissedCount = lines.Count(l => l.Status == "Missed"),
            HeldCount = lines.Count(l => l.Status == "Held"),
            NotAvailableCount = lines.Count(l => l.Status == "NotAvailable"),
        };

        return Ok(new MarReport
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Summary = summary,
            Lines = lines,
        });
    }

    // ──────────────────── SEED DEMO DATA ────────────────────

    // POST api/mar/seed-demo
    [HttpPost("seed-demo")]
    public async Task<ActionResult> SeedDemo(CancellationToken ct)
    {
        // Use server local time so scheduled times match what the desktop computes
        var todayLocal = DateTime.Now.Date;
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(todayLocal);
        var todayStartUtc = new DateTimeOffset(todayLocal, localOffset).ToUniversalTime();
        var dayOfWeek = todayLocal.DayOfWeek;

        var meds = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId != null && m.ResidentId != Guid.Empty)
            .ToListAsync(ct);

        if (meds.Count == 0)
            return BadRequest("No resident-assigned medications found to seed.");

        // Delete existing demo entries for today (clean re-seed)
        var existingToday = await _db.MarEntries
            .Where(m => m.AdministeredAtUtc >= todayStartUtc && m.AdministeredAtUtc < todayStartUtc.AddDays(1))
            .Where(m => m.Notes == "SEED_DEMO")
            .ToListAsync(ct);
        _db.MarEntries.RemoveRange(existingToday);

        var rng = new Random(42);
        var statuses = new[] { "Given", "Given", "Given", "Refused", "Missed" }; // 60% Given, 20% Refused, 20% Missed
        var nurses = new[] { "Nurse Sarah", "Nurse James", "Nurse Emily" };
        var created = new List<object>();

        foreach (var med in meds)
        {
            var times = GetTimesForDay(med, dayOfWeek);
            int slotsToUse = Math.Max(1, Math.Min(3, med.TimesPerDay));

            for (int i = 0; i < slotsToUse && i < times.Count; i++)
            {
                var scheduledTime = times[i];
                if (scheduledTime == TimeSpan.Zero) continue;

                // Convert local scheduled time to UTC (same as desktop does)
                var scheduledLocal = todayLocal.Add(scheduledTime);
                var scheduledOffset = TimeZoneInfo.Local.GetUtcOffset(scheduledLocal);
                var scheduledUtc = new DateTimeOffset(scheduledLocal, scheduledOffset).ToUniversalTime();

                // Only seed past slots (compare local time)
                if (scheduledLocal > DateTime.Now)
                    continue;

                var status = statuses[rng.Next(statuses.Length)];
                var delayMinutes = status == "Given" ? rng.Next(1, 15) : 0;
                var administeredUtc = status == "Given"
                    ? scheduledUtc.AddMinutes(delayMinutes)
                    : scheduledUtc;

                var entry = new MarEntry
                {
                    ClientRequestId = Guid.NewGuid(),
                    ResidentId = med.ResidentId!.Value,
                    MedicationId = med.Id,
                    Status = status,
                    DoseQuantity = med.Quantity > 0 ? med.Quantity : 1,
                    DoseUnit = med.QuantityUnit ?? "tablet",
                    ScheduledForUtc = scheduledUtc,
                    AdministeredAtUtc = administeredUtc,
                    RecordedBy = nurses[rng.Next(nurses.Length)],
                    Notes = "SEED_DEMO",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                };

                _db.MarEntries.Add(entry);
                created.Add(new
                {
                    med.MedName,
                    Slot = scheduledTime.ToString(@"hh\:mm"),
                    status,
                    ScheduledUtc = scheduledUtc.ToString("yyyy-MM-dd HH:mm"),
                    AdminUtc = administeredUtc.ToString("yyyy-MM-dd HH:mm"),
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = $"Seeded {created.Count} MAR entries for today.", Entries = created });
    }

    private static List<TimeSpan> GetTimesForDay(Medication med, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => new() { med.MonTime1, med.MonTime2, med.MonTime3 },
        DayOfWeek.Tuesday => new() { med.TueTime1, med.TueTime2, med.TueTime3 },
        DayOfWeek.Wednesday => new() { med.WedTime1, med.WedTime2, med.WedTime3 },
        DayOfWeek.Thursday => new() { med.ThuTime1, med.ThuTime2, med.ThuTime3 },
        DayOfWeek.Friday => new() { med.FriTime1, med.FriTime2, med.FriTime3 },
        DayOfWeek.Saturday => new() { med.SatTime1, med.SatTime2, med.SatTime3 },
        DayOfWeek.Sunday => new() { med.SunTime1, med.SunTime2, med.SunTime3 },
        _ => new()
    };
}
