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
public sealed class ResidentsController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public ResidentsController(CareHubDbContext db) => _db = db;

    // GET api/residents
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Observer},{Roles.Resident}")]
    public async Task<ActionResult<List<Resident>>> GetAll(CancellationToken ct)
    {
        var query = _db.Residents
            .AsNoTracking()
            .OrderBy(r => r.ResidentLName)
            .ThenBy(r => r.ResidentFName)
            .AsQueryable();

        if (User.IsInRole(Roles.Resident))
        {
            var residentIdText = User.FindFirstValue("resident_id");
            if (!Guid.TryParse(residentIdText, out var residentId))
                return Forbid();
            query = query.Where(r => r.Id == residentId);
        }

        var items = await query.ToListAsync(ct);

        return Ok(items);
    }

    // POST api/residents
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Resident>> Create([FromBody] Resident resident, CancellationToken ct)
    {
        if (resident.Id == Guid.Empty)
            resident.Id = Guid.NewGuid();

        var roomError = await ValidateRoomAssignment(resident, ct);
        if (roomError is not null)
            return BadRequest(new { message = roomError });

        _db.Residents.Add(resident);
        await _db.SaveChangesAsync(ct);

        return Ok(resident);
    }

    // PUT api/residents/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] Resident resident, CancellationToken ct)
    {
        if (id != resident.Id)
            return BadRequest("Route id does not match resident.Id");

        var roomError = await ValidateRoomAssignment(resident, ct);
        if (roomError is not null)
            return BadRequest(new { message = roomError });

        var exists = await _db.Residents.AnyAsync(r => r.Id == id, ct);
        if (exists)
            _db.Entry(resident).State = EntityState.Modified;
        else
            _db.Residents.Add(resident);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE api/residents/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Residents.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity == null) return NotFound();

        _db.Residents.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Validates that a room assignment respects capacity:
    /// Single rooms allow 1 resident, Double rooms allow 2.
    /// </summary>
    private async Task<string?> ValidateRoomAssignment(Resident resident, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(resident.RoomNumber))
            return null; // no room assigned — nothing to validate

        var roomType = resident.RoomType ?? "Single";
        var maxOccupants = roomType.Equals("Double", StringComparison.OrdinalIgnoreCase)
                        || roomType.Equals("Couple", StringComparison.OrdinalIgnoreCase)
            ? 2 : 1;

        var roommates = await _db.Residents
            .AsNoTracking()
            .Where(r => r.RoomNumber == resident.RoomNumber && r.Id != resident.Id)
            .ToListAsync(ct);

        if (roommates.Count >= maxOccupants)
            return $"Room {resident.RoomNumber} is a {roomType} room and is already full ({roommates.Count} occupant{(roommates.Count == 1 ? "" : "s")}).";

        // Gender check for shared rooms
        if (roommates.Count > 0
            && !string.IsNullOrWhiteSpace(resident.Gender)
            && !string.IsNullOrWhiteSpace(roommates[0].Gender)
            && !string.Equals(resident.Gender, roommates[0].Gender, StringComparison.OrdinalIgnoreCase))
        {
            return $"Room {resident.RoomNumber} already has a {roommates[0].Gender} resident. Cannot assign a {resident.Gender} resident to the same room.";
        }

        return null;
    }
}
