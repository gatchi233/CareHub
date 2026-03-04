using CareHub.Desktop.Models;

namespace CareHub.Services.Abstractions;

public interface IMarService
{
    Task<List<MarEntry>> LoadAsync(Guid? residentId, DateTime fromUtc, DateTime toUtc);
    Task CreateAsync(MarEntry entry);
    Task<int> SyncAsync();
}
