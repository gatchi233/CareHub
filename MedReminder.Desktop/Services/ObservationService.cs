using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;
using System.Text.Json;

namespace MedReminder.Desktop.Services;

public class ObservationService : IObservationService
{
    private readonly IObservationService _apiObs;
    private readonly IObservationService _localObs;
    private readonly ISyncQueue _queue;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly string _cachePath =
        Path.Combine(FileSystem.AppDataDirectory, "Observations.json");

    public ObservationService(
        IObservationService apiObservationService,
        IObservationService localObservationService,
        ISyncQueue queue)
    {
        _apiObs = apiObservationService;
        _localObs = localObservationService;
        _queue = queue;
    }

    public async Task<List<Observation>> LoadAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var apiItems = await _apiObs.LoadAsync();
                ConnectivityHelper.MarkOnline();

                var pending = await _queue.GetAllAsync();
                bool hasPending = pending.Any(x => x.EntityType == "Observation");

                if (!hasPending)
                {
                    // Merge: keep local-only items that aren't in the API yet
                    var localItems = await _localObs.LoadAsync();
                    var apiIds = new HashSet<Guid>(apiItems.Select(o => o.Id));
                    var localOnly = localItems.Where(o => !apiIds.Contains(o.Id)).ToList();

                    if (localOnly.Count > 0)
                    {
                        // Build lookup by (ResidentId, RecordedAt) to avoid duplicates
                        var apiKeys = new HashSet<string>(
                            apiItems.Select(o => $"{o.ResidentId}|{o.RecordedAt:O}"),
                            StringComparer.Ordinal);

                        foreach (var obs in localOnly)
                        {
                            var key = $"{obs.ResidentId}|{obs.RecordedAt:O}";
                            if (apiKeys.Contains(key))
                                continue; // API already has this observation — skip to avoid duplicate

                            try
                            {
                                await _apiObs.AddAsync(obs);
                                apiItems.Add(obs);
                            }
                            catch { /* will retry next load */ }
                        }
                    }

                    _ = CacheLocallyAsync(apiItems);
                    return apiItems;
                }

                // Pending changes exist — show local data (which includes unsync'd items)
                return await _localObs.LoadAsync();
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
                return await _localObs.LoadAsync();
            }
        }

        return await _localObs.LoadAsync();
    }

    public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var items = await _apiObs.GetByResidentIdAsync(residentId);
                ConnectivityHelper.MarkOnline();
                return items;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
                return await _localObs.GetByResidentIdAsync(residentId);
            }
        }

        return await _localObs.GetByResidentIdAsync(residentId);
    }

    private async Task CacheLocallyAsync(List<Observation> items)
    {
        try
        {
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_cachePath, json);
        }
        catch { /* best-effort cache */ }
    }

    public Task ReplaceAllAsync(List<Observation> items) => _localObs.ReplaceAllAsync(items);

    public async Task UpsertAsync(Observation item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (item.Id == Guid.Empty)
        {
            await AddAsync(item);
            return;
        }

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiObs.UpsertAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localObs.UpsertAsync(item);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localObs.UpsertAsync(item);
    }

    public async Task DeleteAsync(Observation item)
    {
        if (item is null)
            return;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiObs.DeleteAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localObs.DeleteAsync(item);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localObs.DeleteAsync(item);
    }

    public async Task AddAsync(Observation observation)
    {
        // Always use UTC for recorded/observed timestamp
        observation.RecordedAt = DateTime.UtcNow;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiObs.AddAsync(observation);
                ConnectivityHelper.MarkOnline();
                await _localObs.AddAsync(observation);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        // Offline (or API error): save local + enqueue
        await _localObs.AddAsync(observation);

        var payloadJson = JsonSerializer.Serialize(observation, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Observation",
            Operation = SyncOperation.Create,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<int> SyncAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SYNC] ObservationService.SyncAsync called");

        if (!ConnectivityHelper.IsOnline()) return 0;

        var items = await _queue.GetAllAsync();
        if (items.Count == 0) return 0;

        int success = 0;

        foreach (var item in items.Where(x => x.EntityType == "Observation").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var obs = JsonSerializer.Deserialize<Observation>(item.PayloadJson, _jsonOptions);
                if (obs is null)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                // Ensure UTC (safe)
                obs.RecordedAt = DateTime.SpecifyKind(obs.RecordedAt, DateTimeKind.Utc);

                switch (item.Operation)
                {
                    case SyncOperation.Create:
                        var tempId = obs.Id;
                        obs.Id = Guid.Empty;
                        await _apiObs.AddAsync(obs);
                        if (obs.Id != Guid.Empty && obs.Id != tempId)
                        {
                            await _localObs.DeleteAsync(new Observation { Id = tempId });
                            await _localObs.UpsertAsync(obs);
                        }
                        break;
                    case SyncOperation.Update:
                        await _apiObs.UpsertAsync(obs);
                        break;
                    case SyncOperation.Delete:
                        await _apiObs.DeleteAsync(obs);
                        break;
                }

                await _queue.RemoveAsync(item.Id);
                success++;
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.LastError = ex.Message;
                break;
            }
        }

        return success;
    }
}
