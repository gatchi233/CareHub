using MedReminder.Models;

namespace MedReminder.Services.Abstractions
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
    }
}
