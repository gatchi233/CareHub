using CareHub.Desktop.Models;
using CareHub.Desktop.Services.Sync;
using CareHub.Services.Abstractions;
using System.Text.Json;

namespace CareHub.Desktop.Services;

public class MarService : IMarService
{
    private readonly IMarService _api;
    private readonly IMarService _local;
    private readonly ISyncQueue _queue;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public MarService(IMarService api, IMarService local, ISyncQueue queue)
    {
        _api = api;
        _local = local;
        _queue = queue;
    }

    public async Task<List<MarEntry>> LoadAsync(Guid? residentId, DateTime fromUtc, DateTime toUtc)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var items = await _api.LoadAsync(residentId, fromUtc, toUtc);
                ConnectivityHelper.MarkOnline();

                // Check for pending MAR sync items
                var pending = await _queue.GetAllAsync();
                if (!pending.Any(x => x.EntityType == "MarEntry"))
                {
                    return items;
                }

                // Pending changes exist — show local data
                return await _local.LoadAsync(residentId, fromUtc, toUtc);
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
                return await _local.LoadAsync(residentId, fromUtc, toUtc);
            }
        }

        return await _local.LoadAsync(residentId, fromUtc, toUtc);
    }

    public async Task CreateAsync(MarEntry entry)
    {
        // Ensure ClientRequestId is set for idempotency
        if (entry.ClientRequestId == Guid.Empty)
            entry.ClientRequestId = Guid.NewGuid();

        entry.CreatedAtUtc = DateTimeOffset.UtcNow;
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.CreateAsync(entry);
                ConnectivityHelper.MarkOnline();
                await _local.CreateAsync(entry);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        // Offline: save local + enqueue
        await _local.CreateAsync(entry);

        var payloadJson = JsonSerializer.Serialize(entry, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "MarEntry",
            Operation = SyncOperation.Create,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<int> SyncAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SYNC] MarService.SyncAsync called");

        if (!ConnectivityHelper.IsOnline()) return 0;

        var items = await _queue.GetAllAsync();
        if (items.Count == 0) return 0;

        int success = 0;

        foreach (var item in items.Where(x => x.EntityType == "MarEntry").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<MarEntry>(item.PayloadJson, _jsonOptions);
                if (entry is null)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                // ClientRequestId ensures idempotent replay on the server
                await _api.CreateAsync(entry);

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
