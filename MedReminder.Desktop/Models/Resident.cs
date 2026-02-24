using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MedReminder.Models
{
    public class Resident : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        string _firstName = string.Empty;
        string _lastName = string.Empty;

        public Guid Id { get; set; }

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName == value) return;
                _firstName = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullName));
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (_lastName == value) return;
                _lastName = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullName));
            }
        }

        [JsonIgnore] // avoid writing it into JSON (it’s derived)
        public string FullName
        {
            get
            {
                var fn = (FirstName ?? "").Trim();
                var ln = (LastName ?? "").Trim();
                return string.Join(" ", new[] { fn, ln }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string? SIN { get; set; }
        public string DOB { get; set; } = string.Empty;
        public string? Gender { get; set; } // "Male" / "Female" / "Other"

        // Address information
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }

        // Emergency contact
        public string EmergencyContactName1 { get; set; } = string.Empty;
        public string EmergencyContactPhone1 { get; set; } = string.Empty;
        public string EmergencyRelationship1 { get; set; } = string.Empty;
        public string? EmergencyContactName2 { get; set; }
        public string? EmergencyContactPhone2 { get; set; }
        public string? EmergencyRelationship2 { get; set; }

        // Doctor information
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorContact { get; set; } = string.Empty;

        // Allergies & Remarks
        [JsonIgnore]
        public bool AllergyNone { get; set; }

        // Stored allergy flags
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
        public string? AllergyOtherItems { get; set; }    // e.g. "Pollen..."
        public string? Remarks { get; set; }

        // --- Room placement ---
        public string? AdmissionDate { get; set; }
        public string? RoomNumber { get; set; }  // e.g. "202"
        public string? RoomType { get; set; }    // "Single" / "Couple" / "MedicalBackup"
        public string? BedLabel { get; set; }    // "A" / "B" (Couple rooms)

    }
}
