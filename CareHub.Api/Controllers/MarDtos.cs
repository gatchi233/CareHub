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

// ──────────────────── REPORTING DTOs ────────────────────

public class MarReport
{
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public MarReportSummary Summary { get; set; } = new();
    public List<MarReportLine> Lines { get; set; } = new();
}

public class MarReportSummary
{
    public int TotalEntries { get; set; }
    public int GivenCount { get; set; }
    public int RefusedCount { get; set; }
    public int MissedCount { get; set; }
    public int HeldCount { get; set; }
    public int NotAvailableCount { get; set; }
}

public class MarReportLine
{
    public Guid Id { get; set; }
    public Guid ResidentId { get; set; }
    public string ResidentName { get; set; } = "";
    public Guid MedicationId { get; set; }
    public string MedicationName { get; set; } = "";
    public string Status { get; set; } = "";
    public int DoseQuantity { get; set; }
    public string DoseUnit { get; set; } = "";
    public DateTimeOffset? ScheduledForUtc { get; set; }
    public DateTimeOffset AdministeredAtUtc { get; set; }
    public string? RecordedBy { get; set; }
    public string? Notes { get; set; }
}
