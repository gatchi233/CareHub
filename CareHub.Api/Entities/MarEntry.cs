using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareHub.Api.Entities
{
    public class MarEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Idempotency key (client generated)
        [Required]
        public Guid ClientRequestId { get; set; }

        // Foreign Keys
        [Required]
        public Guid ResidentId { get; set; }

        [Required]
        public Guid MedicationId { get; set; }

        public Guid? MedicationOrderId { get; set; }

        // MAR Status
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;
        // Allowed: Given / Refused / Held / Missed / NotAvailable

        public int DoseQuantity { get; set; }

        [MaxLength(50)]
        public string DoseUnit { get; set; } = string.Empty;

        // Always UTC
        public DateTimeOffset? ScheduledForUtc { get; set; }

        [Required]
        public DateTimeOffset AdministeredAtUtc { get; set; }

        public Guid? AdministeredByStaffId { get; set; }

        [MaxLength(200)]
        public string? RecordedBy { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsVoided { get; set; } = false;

        public DateTimeOffset? VoidedAtUtc { get; set; }

        [MaxLength(500)]
        public string? VoidReason { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }

        // Navigation
        public Resident? Resident { get; set; }
        public Medication? Medication { get; set; }
    }
}
