namespace CareHub.Desktop.Services.Sync;

public class SyncQueueItem
{
    public Guid Id { get; set; } = Guid.NewGuid(); // queue-item id
    public string EntityType { get; set; } = "";   // e.g. "Observation"
    public SyncOperation Operation { get; set; }   // Create only for M2 Option B
    public string PayloadJson { get; set; } = "";  // serialized Observation DTO/model
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Optional: helpful for retry logic
    public int AttemptCount { get; set; } = 0;
    public string LastError { get; set; } = "";
}