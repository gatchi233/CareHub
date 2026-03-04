namespace CareHub.ViewModels;

/// <summary>
/// Read-only view model representing a single scheduled medication slot
/// (or an unscheduled MAR entry) for display on the Desktop MAR page.
/// </summary>
public class MarSlotViewModel
{
    // Schedule identity
    public Guid ResidentId { get; set; }
    public Guid MedicationId { get; set; }
    public DateTime LocalDate { get; set; }
    public string ScheduledLocalTime { get; set; } = "";
    public DateTimeOffset ScheduledForUtc { get; set; }

    // Medication display
    public string MedicationName { get; set; } = "";
    public int DoseQuantity { get; set; }
    public string DoseUnit { get; set; } = "";

    // MAR overlay (populated when a matching entry exists)
    public string Status { get; set; } = "Pending";
    public string? LastAdministeredLocal { get; set; }
    public string? RecordedBy { get; set; }
    public string? NotesPreview { get; set; }

    // Display helpers
    public bool IsUnscheduled { get; set; }

    public string DoseDisplay => DoseQuantity > 0
        ? $"{DoseQuantity} {DoseUnit}".Trim()
        : DoseUnit;

    public string TimeDisplay => IsUnscheduled
        ? (LastAdministeredLocal ?? "—")
        : ScheduledLocalTime;

    public string StatusColor => Status switch
    {
        "Given" => "Badge_Given",
        "Refused" => "Badge_Refused",
        "Held" => "Badge_Held",
        "Missed" => "Badge_Missed",
        "NotAvailable" => "Badge_NotAvailable",
        _ => "Badge_Pending"
    };
}

/// <summary>
/// Groups schedule slots by resident for display.
/// </summary>
public class ResidentMarGroup
{
    public Guid ResidentId { get; set; }
    public string ResidentName { get; set; } = "";
    public string? RoomNumber { get; set; }

    public string Header => string.IsNullOrWhiteSpace(RoomNumber)
        ? ResidentName
        : $"{ResidentName} (Room {RoomNumber})";

    public List<MarSlotViewModel> ScheduledSlots { get; set; } = new();
    public List<MarSlotViewModel> UnscheduledEntries { get; set; } = new();

    public bool HasUnscheduled => UnscheduledEntries.Count > 0;
}
