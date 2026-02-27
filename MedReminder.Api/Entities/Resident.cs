using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MedReminder.Api.Entities;

public sealed class Resident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("FirstName")]
    [JsonPropertyName("ResidentFName")]
    public string ResidentFName { get; set; } = "";
    [Column("LastName")]
    [JsonPropertyName("ResidentLName")]
    public string ResidentLName { get; set; } = "";

    public DateOnly? DateOfBirth { get; set; }

    // Keep “Admission / Facility record” aligned to your current baseline.
    public DateOnly? AdmissionDate { get; set; }
    public string RoomNumber { get; set; } = "";
}
