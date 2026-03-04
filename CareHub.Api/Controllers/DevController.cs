using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/dev")]
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
}
