using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MedReminder.Desktop.Services;

public class MedicationService : IMedicationService
{
    private readonly IMedicationService _apiMed;
    private readonly IMedicationService _localMed;
    private readonly ISyncQueue _queue;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public MedicationService(
        IMedicationService apiService,
        IMedicationService localService,
        ISyncQueue queue)
    {
        _apiMed = apiService;
        _localMed = localService;
        _queue = queue;
    }

    public async Task<List<Medication>> LoadAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var apiItems = await _apiMed.LoadAsync();
                ConnectivityHelper.MarkOnline();

                var pending = await _queue.GetAllAsync();
                bool hasPending = pending.Any(x => x.EntityType == "Medication");

                if (!hasPending)
                {
                    // Merge: keep local-only items that aren't in the API yet
                    var localItems = await _localMed.LoadAsync();
                    var apiIds = new HashSet<Guid>(apiItems.Select(m => m.Id));
                    var localOnly = localItems.Where(m => !apiIds.Contains(m.Id)).ToList();

                    if (localOnly.Count > 0)
                    {
                        // Build a lookup of API items by (MedName, ResidentId) to avoid duplicates
                        var apiKeys = new HashSet<string>(
                            apiItems.Select(m => $"{(m.MedName ?? "").ToLowerInvariant()}|{m.ResidentId}"),
                            StringComparer.Ordinal);

                        foreach (var med in localOnly)
                        {
                            var key = $"{(med.MedName ?? "").ToLowerInvariant()}|{med.ResidentId}";
                            if (apiKeys.Contains(key))
                                continue; // API already has this med for this resident — skip to avoid duplicate

                            try
                            {
                                await _apiMed.UpsertAsync(med);
                                apiItems.Add(med);
                            }
                            catch { /* will retry next load */ }
                        }
                    }

                    await _localMed.ReplaceAllAsync(apiItems);
                    return apiItems;
                }

                // Pending changes exist — show local data (which includes unsync'd items)
                return await _localMed.LoadAsync();
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
                return await _localMed.LoadAsync();
            }
        }

        return await _localMed.LoadAsync();
    }

    public async Task UpsertAsync(Medication item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        // Medication.Id defaults to Guid.NewGuid(), so it's never empty.
        // Detect "create" by checking if the item already exists locally.
        var localList = await _localMed.LoadAsync();
        var isCreate = !localList.Any(m => m.Id == item.Id);

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiMed.UpsertAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localMed.UpsertAsync(item);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localMed.UpsertAsync(item);

        var operation = isCreate ? SyncOperation.Create : SyncOperation.Update;
        var payloadJson = JsonSerializer.Serialize(item, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Medication",
            Operation = operation,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task DeleteAsync(Medication item)
    {
        if (item is null)
            return;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiMed.DeleteAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localMed.DeleteAsync(item);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localMed.DeleteAsync(item);

        var payloadJson = JsonSerializer.Serialize(item, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Medication",
            Operation = SyncOperation.Delete,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public Task ReplaceAllAsync(List<Medication> items) => _localMed.ReplaceAllAsync(items);

    public async Task<List<Medication>> GetLowStockAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var items = await _apiMed.GetLowStockAsync();
                ConnectivityHelper.MarkOnline();
                return items;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
                return await _localMed.GetLowStockAsync();
            }
        }

        return await _localMed.GetLowStockAsync();
    }

    public async Task AdjustStockAsync(Guid medicationId, int delta)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiMed.AdjustStockAsync(medicationId, delta);
                ConnectivityHelper.MarkOnline();
                await _localMed.AdjustStockAsync(medicationId, delta);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localMed.AdjustStockAsync(medicationId, delta);

        var payload = new { MedicationId = medicationId, Delta = delta };
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "StockAdjustment",
            Operation = SyncOperation.Update,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<int> SyncAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SYNC] MedicationService.SyncAsync called");

        if (!ConnectivityHelper.IsOnline()) return 0;

        var items = await _queue.GetAllAsync();
        if (items.Count == 0) return 0;

        int success = 0;

        // Process Medication CRUD operations
        foreach (var item in items.Where(x => x.EntityType == "Medication").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var med = JsonSerializer.Deserialize<Medication>(item.PayloadJson, _jsonOptions);
                if (med is null)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                switch (item.Operation)
                {
                    case SyncOperation.Create:
                    case SyncOperation.Update:
                        // Try upsert as-is (PUT). The API will handle
                        // create-or-update via the controller.
                        await _apiMed.UpsertAsync(med);
                        break;
                    case SyncOperation.Delete:
                        await _apiMed.DeleteAsync(med);
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

        // Process StockAdjustment operations separately
        foreach (var item in items.Where(x => x.EntityType == "StockAdjustment").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var node = JsonNode.Parse(item.PayloadJson);
                var medicationId = node?["medicationId"]?.GetValue<Guid>() ?? Guid.Empty;
                var delta = node?["delta"]?.GetValue<int>() ?? 0;

                if (medicationId == Guid.Empty)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                await _apiMed.AdjustStockAsync(medicationId, delta);
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
    /// After a medication's temp ID is replaced by the server-assigned ID,
    /// update any pending StockAdjustment queue items that still reference
    /// the old MedicationId so they can sync successfully.
    /// </summary>
    private async Task RemapMedicationIdInQueue(Guid oldId, Guid newId)
    {
        var all = await _queue.GetAllAsync();
        var oldIdStr = oldId.ToString();
        var newIdStr = newId.ToString();

        foreach (var qi in all.Where(q => q.EntityType == "StockAdjustment" && q.PayloadJson.Contains(oldIdStr)))
        {
            try
            {
                var node = JsonNode.Parse(qi.PayloadJson);
                if (node is JsonObject obj && obj["medicationId"]?.ToString() == oldIdStr)
                {
                    obj["medicationId"] = newIdStr;
                    qi.PayloadJson = obj.ToJsonString(_jsonOptions);

                    await _queue.RemoveAsync(qi.Id);
                    await _queue.EnqueueAsync(qi);

                    System.Diagnostics.Debug.WriteLine(
                        $"[SYNC] Remapped MedicationId {oldId} -> {newId} in queued StockAdjustment");
                }
            }
            catch { }
        }
    }
}
