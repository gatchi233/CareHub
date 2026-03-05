using CareHub.Models;

namespace CareHub.Services.Abstractions
{
    public interface IMedicationService
    {
        Task<List<Medication>> LoadAsync();
        Task UpsertAsync(Medication item);
        Task DeleteAsync(Medication item);
        Task ReplaceAllAsync(List<Medication> items);

        // Inventory helpers
        Task<List<Medication>> GetLowStockAsync();
        Task AdjustStockAsync(Guid medicationId, int delta);
        Task AdjustStockFifoAsync(string medName, int delta);
    }
}
