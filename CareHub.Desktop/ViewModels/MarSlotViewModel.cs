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

    public string DateTimeDisplay
    {
        get
        {
            if (IsUnscheduled)
                return LastAdministeredLocal ?? "—";

            var dateStr = LocalDate.Date == DateTime.Today
                ? "Today"
                : LocalDate.ToString("ddd dd MMM");

            return $"{dateStr} {ScheduledLocalTime}";
        }
    }

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
/// Groups multiple medication slots at the same date+time into a single card.
/// </summary>
public class MarTimeSlotGroup
{
    public DateTime LocalDate { get; set; }
    public string ScheduledLocalTime { get; set; } = "";
    public DateTimeOffset ScheduledForUtc { get; set; }

    public List<MarSlotViewModel> Slots { get; set; } = new();

    public string DateHeader
    {
        get
        {
            if (LocalDate.Date == DateTime.Today)
                return "Today";
            return LocalDate.ToString("ddd dd/MMM");
        }
    }

    public string TimeHeader => ScheduledLocalTime;

    /// <summary>
    /// Dominant status for the group badge (worst status wins).
    /// </summary>
    public string GroupStatus
    {
        get
        {
            if (Slots.Any(s => s.Status == "Missed")) return "Missed";
            if (Slots.Any(s => s.Status == "Refused")) return "Refused";
            if (Slots.Any(s => s.Status == "Held")) return "Held";
            if (Slots.Any(s => s.Status == "Pending")) return "Pending";
            if (Slots.Any(s => s.Status == "Given")) return "Given";
            return "Pending";
        }
    }

    /// <summary>
    /// Summary line: "Lisinopril 10mg, Aspirin 81mg, Vitamin D3 1000IU"
    /// </summary>
    public string MedicationSummary =>
        string.Join(", ", Slots.Select(s => s.MedicationName));

    /// <summary>
    /// Per-medication detail lines for the card body.
    /// </summary>
    public List<MarSlotViewModel> MedicationDetails => Slots;

    public bool IsUnscheduled { get; set; }
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
