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

        // Add only missing rows by id so partially-seeded databases get repaired
        // without duplicating existing records.
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

            var missingMedications = medications
                .Where(x => x.Id != Guid.Empty && !medicationIdSet.Contains(x.Id))
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
                    Password = x.Password,
                    Role = x.Role,
                    DisplayName = x.DisplayName,
                    ResidentId = x.ResidentId
                })
                .ToList();

            if (missingUsers.Count > 0)
                db.AppUsers.AddRange(missingUsers);
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    private static string? ResolveSeedDirectory(IConfiguration config, IWebHostEnvironment env)
    {
        // Optional explicit override.
        var configured = config["SeedData:Directory"];
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            return configured;

        // Preferred shared location at repo root.
        var sharedData = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "SharedData"));
        if (Directory.Exists(sharedData))
            return sharedData;

        // Backward-compatible fallback to desktop raw assets.
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
