using CareHub.Models;

namespace CareHub.Services.Abstractions
{
    public interface IMedicationOrderService
    {
        Task<List<MedicationOrder>> LoadAsync();

        Task<MedicationOrder> CreateAsync(
            Guid medicationId,
            int requestedQuantity,
            string? requestedBy,
            string? notes);

        Task UpdateStatusAsync(Guid orderId, MedicationOrderStatus newStatus, DateTimeOffset? expiryDate = null);

        Task DeleteAsync(Guid orderId);

        Task<List<MedicationOrder>> GetByMedicationIdAsync(Guid medicationId);
    }
}
