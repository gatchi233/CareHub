using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MedReminder.Desktop.Services;

public class ResidentService : IResidentService
{
    private readonly IResidentService _apiRes;
    private readonly IResidentService _localRes;
    private readonly IMedicationService _localMeds;
    private readonly IObservationService _localObs;
    private readonly ISyncQueue _queue;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ResidentService(
        IResidentService apiService,
        IResidentService localService,
        IMedicationService localMedicationService,
        IObservationService localObservationService,
        ISyncQueue queue)
    {
        _apiRes = apiService;
        _localRes = localService;
        _localMeds = localMedicationService;
        _localObs = localObservationService;
        _queue = queue;
    }

    public async Task<List<Resident>> LoadAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var apiItems = await _apiRes.LoadAsync();
                ConnectivityHelper.MarkOnline();

                var pending = await _queue.GetAllAsync();
                bool hasPending = pending.Any(x => x.EntityType == "Resident");

                if (!hasPending)
                {
                    // Merge: keep local-only items that aren't in the API yet
                    var localItems = await _localRes.LoadAsync();
                    var apiIds = new HashSet<Guid>(apiItems.Select(r => r.Id));
                    var localOnly = localItems.Where(r => !apiIds.Contains(r.Id)).ToList();

                    if (localOnly.Count > 0)
                    {
                        // Build lookup by (FirstName, LastName, DOB) to avoid duplicates
                        var apiKeys = new HashSet<string>(
                            apiItems.Select(r => $"{(r.ResidentFName ?? "").ToLowerInvariant()}|{(r.ResidentLName ?? "").ToLowerInvariant()}|{r.DateOfBirth}"),
                            StringComparer.Ordinal);

                        foreach (var resident in localOnly)
                        {
                            var key = $"{(resident.ResidentFName ?? "").ToLowerInvariant()}|{(resident.ResidentLName ?? "").ToLowerInvariant()}|{resident.DateOfBirth}";
                            if (apiKeys.Contains(key))
                                continue; // API already has this resident — skip to avoid duplicate

                            try
                            {
                                await _apiRes.UpsertAsync(resident);
                                apiItems.Add(resident);
                            }
                            catch { /* will retry next load */ }
                        }
                    }

                    await _localRes.ReplaceAllAsync(apiItems);
                    return apiItems;
                }

                // Pending changes exist — show local data (which includes unsync'd items)
                return await _localRes.LoadAsync();
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
                return await _localRes.LoadAsync();
            }
        }

        return await _localRes.LoadAsync();
    }

    public async Task UpsertAsync(Resident item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        var isCreate = item.Id == Guid.Empty;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiRes.UpsertAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localRes.UpsertAsync(item);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localRes.UpsertAsync(item);

        var operation = isCreate ? SyncOperation.Create : SyncOperation.Update;
        var payloadJson = JsonSerializer.Serialize(item, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Resident",
            Operation = operation,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task DeleteAsync(Resident item)
    {
        if (item is null)
            return;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiRes.DeleteAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localRes.DeleteAsync(item);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localRes.DeleteAsync(item);

        var payloadJson = JsonSerializer.Serialize(item, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Resident",
            Operation = SyncOperation.Delete,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public Task ReplaceAllAsync(List<Resident> items) => _localRes.ReplaceAllAsync(items);

    public async Task<int> SyncAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SYNC] ResidentService.SyncAsync called");

        if (!ConnectivityHelper.IsOnline()) return 0;

        var items = await _queue.GetAllAsync();
        if (items.Count == 0) return 0;

        int success = 0;

        foreach (var item in items.Where(x => x.EntityType == "Resident").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var resident = JsonSerializer.Deserialize<Resident>(item.PayloadJson, _jsonOptions);
                if (resident is null)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                switch (item.Operation)
                {
                    case SyncOperation.Create:
                        var tempId = resident.Id;
                        resident.Id = Guid.Empty;
                        await _apiRes.UpsertAsync(resident);
                        if (resident.Id != Guid.Empty && resident.Id != tempId)
                        {
                            await _localRes.DeleteAsync(new Resident { Id = tempId });
                            await _localRes.UpsertAsync(resident);
                            await RemapResidentIdInQueue(tempId, resident.Id);
                        }
                        break;
                    case SyncOperation.Update:
                        await _apiRes.UpsertAsync(resident);
                        break;
                    case SyncOperation.Delete:
                        await _apiRes.DeleteAsync(resident);
                        break;
                }

                await _queue.RemoveAsync(item.Id);
                success++;
            }
            catch
            {
                break;
            }
        }

        return success;
    }

    /// <summary>
    /// After a resident's temp ID is replaced by the server-assigned ID,
    /// update any pending queue items AND locally-saved medications/observations
    /// that still reference the old ResidentId.
    /// </summary>
    private async Task RemapResidentIdInQueue(Guid oldId, Guid newId)
    {
        var all = await _queue.GetAllAsync();
        var oldIdStr = oldId.ToString();
        var newIdStr = newId.ToString();

        // 1. Remap ResidentId in queued items (Medication, Observation, StockAdjustment)
        foreach (var qi in all.Where(q => q.EntityType != "Resident" && q.PayloadJson.Contains(oldIdStr)))
        {
            try
            {
                var node = JsonNode.Parse(qi.PayloadJson);
                if (node is JsonObject obj && obj["residentId"]?.ToString() == oldIdStr)
                {
                    obj["residentId"] = newIdStr;
                    qi.PayloadJson = obj.ToJsonString(_jsonOptions);

                    await _queue.RemoveAsync(qi.Id);
                    await _queue.EnqueueAsync(qi);

                    System.Diagnostics.Debug.WriteLine(
                        $"[SYNC] Remapped ResidentId {oldId} -> {newId} in queued {qi.EntityType}");
                }
            }
            catch { }
        }

        // 2. Remap ResidentId in locally-saved medications
        try
        {
            var meds = await _localMeds.LoadAsync();
            bool medsChanged = false;
            foreach (var med in meds.Where(m => m.ResidentId == oldId))
            {
                med.ResidentId = newId;
                medsChanged = true;
            }
            if (medsChanged)
            {
                await _localMeds.ReplaceAllAsync(meds);
                System.Diagnostics.Debug.WriteLine(
                    $"[SYNC] Remapped ResidentId {oldId} -> {newId} in local Medications");
            }
        }
        catch { }

        // 3. Remap ResidentId in locally-saved observations
        try
        {
            var obs = await _localObs.LoadAsync();
            bool obsChanged = false;
            foreach (var o in obs.Where(o => o.ResidentId == oldId))
            {
                o.ResidentId = newId;
                obsChanged = true;
            }
            if (obsChanged)
            {
                await _localObs.ReplaceAllAsync(obs);
                System.Diagnostics.Debug.WriteLine(
                    $"[SYNC] Remapped ResidentId {oldId} -> {newId} in local Observations");
            }
        }
        catch { }
    }
}
