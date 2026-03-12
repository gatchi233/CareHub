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
        bool seededResidents = false;
        bool seededUsers = false;

        // Seed residents if none exist
        if (!await _db.Residents.AnyAsync(ct))
        {
            var r1 = new Resident { ResidentFName = "Amy", ResidentLName = "Chen", RoomNumber = "101" };
            var r2 = new Resident { ResidentFName = "David", ResidentLName = "Wong", RoomNumber = "102" };

            _db.Residents.AddRange(r1, r2);
            _db.Medications.Add(new Medication { ResidentId = r1.Id, MedName = "Amlodipine", Dosage = "5 mg", TimesPerDay = 1 });
            _db.Observations.Add(new Observation { ResidentId = r2.Id, Type = "Temp", Value = "36.9C", RecordedBy = "Demo" });
            seededResidents = true;
        }

        // Seed user accounts if none exist
        if (!await _db.AppUsers.AnyAsync(ct))
        {
            var users = new[]
            {
                new AppUser
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    DisplayName = "Administrator",
                    Role = "Admin"
                },
                new AppUser
                {
                    Username = "nurse1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("nurse123"),
                    DisplayName = "Nurse One",
                    Role = "Nurse"
                },
                new AppUser
                {
                    Username = "carestaff1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("care123"),
                    DisplayName = "Care Staff One",
                    Role = "CareStaff"
                }
            };

            _db.AppUsers.AddRange(users);
            seededUsers = true;
        }

        if (!seededResidents && !seededUsers)
            return Ok(new { seeded = false, reason = "Already has data" });

        await _db.SaveChangesAsync(ct);
        return Ok(new { seeded = true, seededResidents, seededUsers });
    }

    /// <summary>
    /// Removes duplicate medications that share the same (MedName, ResidentId),
    /// keeping the oldest record (lowest Id) per group.
    /// Reassigns MAR entries and inventory ledger records to the kept medication.
    /// </summary>
    [HttpPost("dedup-medications")]
    public async Task<IActionResult> DedupMedications(CancellationToken ct)
    {
        var allMeds = await _db.Medications.ToListAsync(ct);

        var groups = allMeds
            .GroupBy(m => $"{(m.MedName ?? "").ToLowerInvariant()}|{m.ResidentId}")
            .Where(g => g.Count() > 1)
            .ToList();

        if (groups.Count == 0)
            return Ok(new { removed = 0, reassigned = 0, message = "No duplicates found." });

        int totalRemoved = 0;
        int totalReassigned = 0;

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(m => m.Id).ToList();
            var keep = ordered.First();
            var dupes = ordered.Skip(1).ToList();
            var dupeIds = dupes.Select(d => d.Id).ToHashSet();

            // Reassign MAR entries from duplicates to the kept medication
            var marEntries = await _db.MarEntries
                .Where(e => dupeIds.Contains(e.MedicationId))
                .ToListAsync(ct);
            foreach (var entry in marEntries)
                entry.MedicationId = keep.Id;
            totalReassigned += marEntries.Count;

            // Reassign inventory ledger records from duplicates to the kept medication
            var ledgers = await _db.MedicationInventoryLedgers
                .Where(l => dupeIds.Contains(l.MedicationId))
                .ToListAsync(ct);
            foreach (var ledger in ledgers)
                ledger.MedicationId = keep.Id;
            totalReassigned += ledgers.Count;

            _db.Medications.RemoveRange(dupes);
            totalRemoved += dupes.Count;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { removed = totalRemoved, reassigned = totalReassigned, message = $"Removed {totalRemoved} duplicate medication(s), reassigned {totalReassigned} related record(s)." });
    }
}
