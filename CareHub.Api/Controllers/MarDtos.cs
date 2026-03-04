using System.ComponentModel.DataAnnotations;

namespace CareHub.Api.Controllers;

public record CreateMarEntryRequest
{
    [Required]
    public Guid ClientRequestId { get; init; }

    [Required]
    public Guid ResidentId { get; init; }

    [Required]
    public Guid MedicationId { get; init; }

    [Required]
    [MaxLength(50)]
    public string Status { get; init; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DoseQuantity must be a positive integer.")]
    public int DoseQuantity { get; init; }

    [MaxLength(50)]
    public string DoseUnit { get; init; } = string.Empty;

    public DateTimeOffset AdministeredAtUtc { get; init; }

    public DateTimeOffset? ScheduledForUtc { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    [MaxLength(200)]
    public string? RecordedBy { get; init; }
}

public record VoidMarEntryRequest
{
    [MaxLength(500)]
    public string? Reason { get; init; }
}
