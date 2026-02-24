namespace MedReminder.Models
{
    public class MedicationOrder
    {
        public Guid Id { get; set; }
        public Guid MedicationId { get; set; }
        public int RequestedQuantity { get; set; }

        public MedicationOrderStatus Status { get; set; }

        public DateTimeOffset RequestedAt { get; set; }
        public string? RequestedBy { get; set; }

        public DateTimeOffset? OrderedAt { get; set; }
        public string? OrderedBy { get; set; }

        public DateTimeOffset? ReceivedAt { get; set; }
        public string? ReceivedBy { get; set; }

        public DateTimeOffset? CancelledAt { get; set; }
        public string? CancelledBy { get; set; }

        public string? Notes { get; set; }
    }

    public enum MedicationOrderStatus
    {
        Requested,
        Ordered,
        Received,
        Cancelled
    }
}
