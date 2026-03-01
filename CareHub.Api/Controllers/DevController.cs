using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/dev")]
[Authorize(Roles = Roles.Admin)]
public sealed class DevController : ControllerBase
{
    private readonly CareHubDbContext _db;
    public DevController(CareHubDbContext db) => _db = db;

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        if (await _db.Residents.AnyAsync(ct))
            return Ok(new { seeded = false, reason = "Already has data" });

        var r1 = new Resident { ResidentFName = "Amy", ResidentLName = "Chen", RoomNumber = "101" };
        var r2 = new Resident { ResidentFName = "David", ResidentLName = "Wong", RoomNumber = "102" };

        _db.Residents.AddRange(r1, r2);
        _db.Medications.Add(new Medication { ResidentId = r1.Id, MedName = "Amlodipine", Dosage = "5 mg", TimesPerDay = 1 });
        _db.Observations.Add(new Observation { ResidentId = r2.Id, Type = "Temp", Value = "36.9C", RecordedBy = "Demo" });

        await _db.SaveChangesAsync(ct);
        return Ok(new { seeded = true });
    }
}
