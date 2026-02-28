using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MedReminder.Desktop.Services.Sync;

public class JsonSyncQueue : ISyncQueue
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public JsonSyncQueue(string fileName = "sync_queue.json")
    {
        _path = Path.Combine(FileSystem.AppDataDirectory, fileName);
    }

    public async Task<List<SyncQueueItem>> GetAllAsync()
    {
        if (!File.Exists(_path)) return new List<SyncQueueItem>();

        var json = await File.ReadAllTextAsync(_path);
        if (string.IsNullOrWhiteSpace(json)) return new List<SyncQueueItem>();

        return JsonSerializer.Deserialize<List<SyncQueueItem>>(json, _jsonOptions) ?? new List<SyncQueueItem>();
    }

    public async Task EnqueueAsync(SyncQueueItem item)
    {
        var list = await GetAllAsync();
        list.Add(item);
        await SaveAsync(list);
        System.Diagnostics.Debug.WriteLine($"[SYNCQ] Enqueued {item.EntityType} {item.Operation} id={item.Id}");
    }

    public async Task RemoveAsync(Guid queueItemId)
    {
        var list = await GetAllAsync();
        list.RemoveAll(x => x.Id == queueItemId);
        await SaveAsync(list);
    }

    public async Task ClearAsync()
    {
        await SaveAsync(new List<SyncQueueItem>());
    }

    private async Task SaveAsync(List<SyncQueueItem> list)
    {
        var json = JsonSerializer.Serialize(list, _jsonOptions);
        await File.WriteAllTextAsync(_path, json);
    }
}
