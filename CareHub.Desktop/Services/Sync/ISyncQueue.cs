using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareHub.Desktop.Services.Sync;

public interface ISyncQueue
{
    Task<List<SyncQueueItem>> GetAllAsync();
    Task EnqueueAsync(SyncQueueItem item);
    Task RemoveAsync(Guid queueItemId);
    Task ClearAsync();
}
