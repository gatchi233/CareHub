namespace CareHub.Desktop.Models
{
    public class MarEntry
    {
        public Guid Id { get; set; }

        public Guid ClientRequestId { get; set; }

        public Guid ResidentId { get; set; }
        public Guid MedicationId { get; set; }
        public Guid? MedicationOrderId { get; set; }

        public string Status { get; set; } = string.Empty;

        public int DoseQuantity { get; set; }
        public string DoseUnit { get; set; } = string.Empty;

        public DateTimeOffset? ScheduledForUtc { get; set; }
        public DateTimeOffset AdministeredAtUtc { get; set; }

        public Guid? AdministeredByStaffId { get; set; }

        public string? RecordedBy { get; set; }

        public string? Notes { get; set; }

        public bool IsVoided { get; set; }

        public DateTimeOffset? VoidedAtUtc { get; set; }
        public string? VoidReason { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }

        // Display helpers for UI
        public string MedicationName { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
    }
}
