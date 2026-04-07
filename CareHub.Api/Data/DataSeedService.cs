using System.Text.Json;
using CareHub.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Data;

public static class DataSeedService
{
    public static async Task SeedFromSharedJsonAsync(
        IServiceProvider services,
        IConfiguration config,
        IWebHostEnvironment env,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHubDbContext>();

        var seedDir = ResolveSeedDirectory(config, env);
        if (seedDir is null)
            return;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var residents = await LoadListAsync<Resident>(Path.Combine(seedDir, "Residents.json"), jsonOptions, ct);
        var medications = await LoadListAsync<Medication>(Path.Combine(seedDir, "Medications.json"), jsonOptions, ct);
        var observations = await LoadListAsync<Observation>(Path.Combine(seedDir, "Observations.json"), jsonOptions, ct);
        var auth = config.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();

        if (residents.Count > 0)
        {
            var existingResidentIds = await db.Residents
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(ct);
            var residentIdSet = existingResidentIds.ToHashSet();

            var missingResidents = residents
                .Where(x => x.Id != Guid.Empty && !residentIdSet.Contains(x.Id))
                .ToList();

            if (missingResidents.Count > 0)
                db.Residents.AddRange(missingResidents);
        }

        if (medications.Count > 0)
        {
            var existingMedicationIds = await db.Medications
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(ct);
            var medicationIdSet = existingMedicationIds.ToHashSet();

            var existingMedKeys = await db.Medications
                .AsNoTracking()
                .Select(x => new { x.MedName, x.ResidentId })
                .ToListAsync(ct);
            var medKeySet = new HashSet<string>(
                existingMedKeys.Select(x => $"{(x.MedName ?? "").ToLowerInvariant()}|{x.ResidentId}"),
                StringComparer.Ordinal);

            var missingMedications = medications
                .Where(x => x.Id != Guid.Empty && !medicationIdSet.Contains(x.Id))
                .Where(x => !medKeySet.Contains($"{(x.MedName ?? "").ToLowerInvariant()}|{x.ResidentId}"))
                .ToList();

            if (missingMedications.Count > 0)
                db.Medications.AddRange(missingMedications);
        }

        if (observations.Count > 0)
        {
            var existingObservationIds = await db.Observations
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(ct);
            var observationIdSet = existingObservationIds.ToHashSet();

            var missingObservations = observations
                .Where(x => x.Id != Guid.Empty && !observationIdSet.Contains(x.Id))
                .ToList();

            if (missingObservations.Count > 0)
            {
                foreach (var item in missingObservations)
                    item.RecordedAt = NormalizeToUtc(item.RecordedAt);

                db.Observations.AddRange(missingObservations);
            }
        }

        if (auth.Users.Count > 0)
        {
            var existingUsernames = await db.AppUsers
                .AsNoTracking()
                .Select(x => x.Username)
                .ToListAsync(ct);
            var usernameSet = existingUsernames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingUsers = auth.Users
                .Where(x => !string.IsNullOrWhiteSpace(x.Username))
                .Where(x => !usernameSet.Contains(x.Username))
                .Select(x => new AppUser
                {
                    Username = x.Username,
                    PasswordHash = x.Password,
                    Role = x.Role,
                    DisplayName = x.DisplayName,
                    ResidentId = Guid.TryParse(x.ResidentId, out var rid) ? rid : null
                })
                .ToList();

            if (missingUsers.Count > 0)
                db.AppUsers.AddRange(missingUsers);
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);

        await SeedDemoMarEntriesAsync(db, ct);
    }

    private static async Task SeedDemoMarEntriesAsync(CareHubDbContext db, CancellationToken ct)
    {
        if (await db.MarEntries.AnyAsync(ct))
            return;

        var residentMeds = await db.Medications.AsNoTracking()
            .Where(m => m.ResidentId.HasValue && m.ResidentId != Guid.Empty)
            .ToListAsync(ct);

        if (residentMeds.Count == 0)
            return;

        var residents = await db.Residents.AsNoTracking().ToDictionaryAsync(r => r.Id, ct);

        var rng = new Random(42);
        var statuses = new[] { "Given", "Given", "Given", "Refused", "Missed" };
        var nurses = new[] { "Nurse Sarah", "Nurse James", "Nurse Emily" };
        var todayLocal = DateTime.Now.Date;
        var nowUtc = DateTimeOffset.UtcNow;

        foreach (var med in residentMeds)
        {
            if (med.TimesPerDay <= 0) continue;
            var residentId = med.ResidentId!.Value;
            var residentName = residents.TryGetValue(residentId, out var r)
                ? $"{r.ResidentFName} {r.ResidentLName}".Trim()
                : "Unknown";

            var slots = Math.Min(med.TimesPerDay, 3);
            var baseTimes = new[] { new TimeSpan(8, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(20, 0, 0) };

            for (int i = 0; i < slots; i++)
            {
                var scheduledLocal = todayLocal.Add(baseTimes[i]);

                if (scheduledLocal > DateTime.Now)
                    continue;

                var scheduledOffset = TimeZoneInfo.Local.GetUtcOffset(scheduledLocal);
                var scheduledUtc = new DateTimeOffset(scheduledLocal, scheduledOffset).ToUniversalTime();

                var status = statuses[rng.Next(statuses.Length)];
                var delayMinutes = status == "Given" ? rng.Next(1, 15) : 0;
                var administeredUtc = status == "Given"
                    ? scheduledUtc.AddMinutes(delayMinutes)
                    : scheduledUtc;

                db.MarEntries.Add(new MarEntry
                {
                    Id = Guid.NewGuid(),
                    ClientRequestId = Guid.NewGuid(),
                    ResidentId = residentId,
                    MedicationId = med.Id,
                    Status = status,
                    DoseQuantity = med.Quantity > 0 ? med.Quantity : 1,
                    DoseUnit = med.QuantityUnit ?? "tablet",
                    ScheduledForUtc = scheduledUtc,
                    AdministeredAtUtc = administeredUtc,
                    RecordedBy = nurses[rng.Next(nurses.Length)],
                    Notes = "SEED_DEMO",
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc,
                });
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    private static string? ResolveSeedDirectory(IConfiguration config, IWebHostEnvironment env)
    {
        var configured = config["SeedData:Directory"];
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            return configured;

        var apiSeedData = Path.Combine(env.ContentRootPath, "SeedData");
        if (Directory.Exists(apiSeedData))
            return apiSeedData;

        var sharedData = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "SharedData"));
        if (Directory.Exists(sharedData))
            return sharedData;

        var desktopRaw = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "CareHub.Desktop", "Resources", "Raw"));
        if (Directory.Exists(desktopRaw))
            return desktopRaw;

        return null;
    }

    private static async Task<List<T>> LoadListAsync<T>(
        string path,
        JsonSerializerOptions options,
        CancellationToken ct)
    {
        if (!File.Exists(path))
            return new List<T>();

        await using var stream = File.OpenRead(path);
        var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, options, ct);
        return items ?? new List<T>();
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        if (value == default)
            return DateTime.UtcNow;

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
