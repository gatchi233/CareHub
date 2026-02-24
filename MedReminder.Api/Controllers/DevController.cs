using MedReminder.Api.Data;
using MedReminder.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedReminder.Api.Controllers;

[ApiController]
[Route("api/dev")]
public sealed class DevController : ControllerBase
{
    private readonly CareHubDbContext _db;
    public DevController(CareHubDbContext db) => _db = db;

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        if (await _db.Residents.AnyAsync(ct))
            return Ok(new { seeded = false, reason = "Already has data" });

        var r1 = new Resident { FirstName = "Amy", LastName = "Chen", RoomNumber = "101" };
        var r2 = new Resident { FirstName = "David", LastName = "Wong", RoomNumber = "102" };

        _db.Residents.AddRange(r1, r2);
        _db.Medications.Add(new Medication { ResidentId = r1.Id, MedName = "Amlodipine", Dosage = "5 mg", TimesPerDay = 1 });
        _db.Observations.Add(new Observation { ResidentId = r2.Id, Type = "Temp", Value = "36.9C", RecordedBy = "Demo" });

        await _db.SaveChangesAsync(ct);
        return Ok(new { seeded = true });
    }
}