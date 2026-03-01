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

        // If data already exists, do not reseed.
        if (await db.Residents.AnyAsync(ct) || await db.Medications.AnyAsync(ct) || await db.Observations.AnyAsync(ct))
            return;

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

        if (residents.Count > 0)
            db.Residents.AddRange(residents);
        if (medications.Count > 0)
            db.Medications.AddRange(medications);
        if (observations.Count > 0)
            db.Observations.AddRange(observations);

        if (residents.Count > 0 || medications.Count > 0 || observations.Count > 0)
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
}
