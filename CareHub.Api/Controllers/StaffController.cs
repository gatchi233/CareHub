using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize]
public sealed class StaffController : ControllerBase
{
    private readonly CareHubDbContext _db;
    private readonly string _staffDirectoryPath;
    private readonly string _desktopSeedStaffPath;
    private static readonly SemaphoreSlim StaffDirectoryLock = new(1, 1);
    private static readonly JsonSerializerOptions StaffJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public StaffController(CareHubDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _staffDirectoryPath = ResolveStaffDirectoryPath(config, env);
        _desktopSeedStaffPath = ResolveSeedStaffPath(env);
    }

    // GET api/staff
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<object>>> GetAll(CancellationToken ct)
    {
        var list = await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Role != Roles.Observer)
            .Select(u => new
            {
                username = u.Username,
                displayName = u.DisplayName,
                role = u.Role
            })
            .ToListAsync(ct);

        return Ok(list.Cast<object>().ToList());
    }

    // GET api/staff/{username}
    [HttpGet("{username}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<object>> GetByUsername(string username, CancellationToken ct)
    {
        var user = await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Role != Roles.Observer)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), ct);

        if (user is null)
            return NotFound();

        return Ok(new
        {
            username = user.Username,
            displayName = user.DisplayName,
            role = user.Role
        });
    }

    // PUT api/staff/{username}
    [HttpPut("{username}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(string username, [FromBody] UpdateStaffRequest request, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == username.ToLower(), ct);
        if (user is null)
            return NotFound();

        var role = (request.Role ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(role))
        {
            var allowed = new[] { Roles.Admin, Roles.Nurse, Roles.GeneralCareStaff, Roles.Observer };
            if (!allowed.Contains(role, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Role must be Admin, Nurse, General CareStaff, or Observer.");

            user.Role = role;
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = request.Password;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
    // POST api/staff
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var allowed = new[] { Roles.Admin, Roles.Nurse, Roles.GeneralCareStaff, Roles.Observer };
        var role = (request.Role ?? "").Trim();
        if (!allowed.Contains(role, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Role must be Admin, Nurse, General CareStaff, or Observer.");

        var exists = await _db.AppUsers.AnyAsync(
            u => u.Username.ToLower() == request.Username.Trim().ToLower(), ct);
        if (exists)
            return Conflict("A user with that username already exists.");

        var user = new AppUser
        {
            Username = request.Username.Trim(),
            PasswordHash = request.Password,
            DisplayName = (request.DisplayName ?? request.Username).Trim(),
            Role = role,
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), null,
            new { username = user.Username, displayName = user.DisplayName, role = user.Role });
    }

    // DELETE api/staff/{username}
    [HttpDelete("{username}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string username, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(
            u => u.Username.ToLower() == username.ToLower(), ct);
        if (user is null)
            return NotFound();

        // Prevent deleting yourself
        var currentUsername = User.Identity?.Name;
        if (string.Equals(user.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
            return BadRequest("You cannot delete your own account.");

        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // GET api/staff/directory
    [HttpGet("directory")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<StaffDirectoryRecord>>> GetDirectory(CancellationToken ct)
    {
        var list = await LoadDirectoryAsync(ct);
        return Ok(list.OrderBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase).ToList());
    }

    // PUT api/staff/directory/{employeeId}
    [HttpPut("directory/{employeeId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpsertDirectoryRecord(string employeeId, [FromBody] StaffDirectoryRecord request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var normalizedId = (employeeId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedId))
            return BadRequest("EmployeeId is required.");

        request.EmployeeId = normalizedId;
        request.Compliance ??= new StaffComplianceRecord();

        var list = await LoadDirectoryAsync(ct);
        var index = list.FindIndex(x => x.EmployeeId.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
            list[index] = request;
        else
            list.Add(request);

        await SaveDirectoryAsync(list, ct);
        return NoContent();
    }

    private async Task<List<StaffDirectoryRecord>> LoadDirectoryAsync(CancellationToken ct)
    {
        await StaffDirectoryLock.WaitAsync(ct);
        try
        {
            EnsureDirectoryFileExists();

            if (!System.IO.File.Exists(_staffDirectoryPath))
                return new List<StaffDirectoryRecord>();

            await using var stream = System.IO.File.OpenRead(_staffDirectoryPath);
            return await JsonSerializer.DeserializeAsync<List<StaffDirectoryRecord>>(stream, StaffJsonOptions, ct)
                   ?? new List<StaffDirectoryRecord>();
        }
        finally
        {
            StaffDirectoryLock.Release();
        }
    }

    private void EnsureDirectoryFileExists()
    {
        if (System.IO.File.Exists(_staffDirectoryPath))
            return;

        var targetDir = Path.GetDirectoryName(_staffDirectoryPath);
        if (!string.IsNullOrWhiteSpace(targetDir))
            Directory.CreateDirectory(targetDir);

        if (System.IO.File.Exists(_desktopSeedStaffPath))
            System.IO.File.Copy(_desktopSeedStaffPath, _staffDirectoryPath, overwrite: false);
    }

    private async Task SaveDirectoryAsync(List<StaffDirectoryRecord> staff, CancellationToken ct)
    {
        await StaffDirectoryLock.WaitAsync(ct);
        try
        {
            var dir = Path.GetDirectoryName(_staffDirectoryPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            await using var stream = System.IO.File.Create(_staffDirectoryPath);
            await JsonSerializer.SerializeAsync(stream, staff, StaffJsonOptions, ct);
        }
        finally
        {
            StaffDirectoryLock.Release();
        }
    }

    private static string ResolveStaffDirectoryPath(IConfiguration config, IWebHostEnvironment env)
    {
        var configured = config["StaffDirectory:Path"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (Directory.Exists(configured))
                return Path.Combine(configured, "Staff.json");

            var parent = Path.GetDirectoryName(configured);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);
            return configured;
        }

        var apiSeedData = Path.Combine(env.ContentRootPath, "SeedData");
        if (Directory.Exists(apiSeedData))
            return Path.Combine(apiSeedData, "Staff.json");

        var sharedData = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "SharedData"));
        if (Directory.Exists(sharedData))
            return Path.Combine(sharedData, "Staff.json");

        var desktopRaw = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "CareHub.Desktop", "Resources", "Raw"));
        return Path.Combine(desktopRaw, "Staff.json");
    }

    private static string ResolveSeedStaffPath(IWebHostEnvironment env)
    {
        var apiSeedStaff = Path.Combine(env.ContentRootPath, "SeedData", "Staff.json");
        if (System.IO.File.Exists(apiSeedStaff))
            return apiSeedStaff;

        var sharedDataStaff = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "SharedData", "Staff.json"));
        if (System.IO.File.Exists(sharedDataStaff))
            return sharedDataStaff;

        return Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "CareHub.Desktop", "Resources", "Raw", "Staff.json"));
    }
}

public sealed class CreateStaffRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "";
}

public sealed class UpdateStaffRequest
{
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
}

public sealed class StaffDirectoryRecord
{
    public string EmployeeId { get; set; } = "";
    public string StaffFName { get; set; } = "";
    public string StaffLName { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Department { get; set; } = "";
    public string EmploymentStatus { get; set; } = "";
    public decimal HourlyWage { get; set; }
    public string ShiftPreference { get; set; } = "";
    public string Role { get; set; } = "General CareStaff";
    public bool IsEnabled { get; set; } = true;
    public StaffComplianceRecord Compliance { get; set; } = new();
}

public sealed class StaffComplianceRecord
{
    public bool HasFirstAid { get; set; }
    public string FirstAidExpiry { get; set; } = "";
    public bool FoodSafeCertified { get; set; }
    public string FoodSafeExpiry { get; set; } = "";
}
