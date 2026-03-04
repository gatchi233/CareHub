using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareHub.Api.Entities
{
    public class MedicationInventoryLedger
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid MedicationId { get; set; }

        [Required]
        public Guid MarEntryId { get; set; }

        public int ChangeQty { get; set; }  // Negative for deduction

        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Reason { get; set; } = "MAR_GIVEN";

        public DateTimeOffset CreatedAtUtc { get; set; }

        public Medication? Medication { get; set; }
        public MarEntry? MarEntry { get; set; }
    }
}
