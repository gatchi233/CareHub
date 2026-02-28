using System.Text.Json.Serialization;

namespace MedReminder.Api.Entities;

public sealed class Resident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("ResidentFName")]
    public string ResidentFName { get; set; } = "";
    [JsonPropertyName("ResidentLName")]
    public string ResidentLName { get; set; } = "";

    public string? Gender { get; set; }
    public string? SIN { get; set; }
    public string DateOfBirth { get; set; } = "";

    // Address
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }

    // Emergency contacts
    public string EmergencyContactName1 { get; set; } = "";
    public string EmergencyContactPhone1 { get; set; } = "";
    public string EmergencyRelationship1 { get; set; } = "";
    public string? EmergencyContactName2 { get; set; }
    public string? EmergencyContactPhone2 { get; set; }
    public string? EmergencyRelationship2 { get; set; }

    // Doctor
    public string DoctorName { get; set; } = "";
    public string DoctorContact { get; set; } = "";

    // Allergies
    public bool AllergyPeanuts { get; set; }
    public bool AllergyTreeNuts { get; set; }
    public bool AllergyMilk { get; set; }
    public bool AllergyEggs { get; set; }
    public bool AllergyShellfish { get; set; }
    public bool AllergyFish { get; set; }
    public bool AllergyWheat { get; set; }
    public bool AllergySoy { get; set; }
    public bool AllergyLatex { get; set; }
    public bool AllergyPenicillin { get; set; }
    public bool AllergySulfa { get; set; }
    public bool AllergyAspirin { get; set; }
    public string? AllergyOtherItems { get; set; }
    public string? Remarks { get; set; }

    // Room placement
    public string? AdmissionDate { get; set; }
    public string RoomNumber { get; set; } = "";
    public string? RoomType { get; set; }
    public string? BedLabel { get; set; }
}
