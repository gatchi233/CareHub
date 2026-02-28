using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MedReminder.Models
{
    public class Resident : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        string _residentFName = string.Empty;
        string _residentLName = string.Empty;

        public Guid Id { get; set; }

        public string ResidentFName
        {
            get => _residentFName;
            set
            {
                if (_residentFName == value) return;
                _residentFName = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResidentName));
            }
        }

        public string ResidentLName
        {
            get => _residentLName;
            set
            {
                if (_residentLName == value) return;
                _residentLName = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResidentName));
            }
        }

        [JsonIgnore] // avoid writing it into JSON (it’s derived)
        public string ResidentName
        {
            get
            {
                var fn = (ResidentFName ?? "").Trim();
                var ln = (ResidentLName ?? "").Trim();
                return string.Join(" ", new[] { fn, ln }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string? SIN { get; set; }
        public string DateOfBirth { get; set; } = string.Empty;
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
        bool _allergyNone;
        bool _allergyPeanuts, _allergyTreeNuts, _allergyMilk, _allergyEggs;
        bool _allergyShellfish, _allergyFish, _allergyWheat, _allergySoy;
        bool _allergyLatex, _allergyPenicillin, _allergySulfa, _allergyAspirin;
        string? _allergyOtherItems;

        [JsonIgnore]
        public bool AllergyNone { get => _allergyNone; set { if (_allergyNone == value) return; _allergyNone = value; OnPropertyChanged(); } }

        public bool AllergyPeanuts { get => _allergyPeanuts; set { if (_allergyPeanuts == value) return; _allergyPeanuts = value; OnPropertyChanged(); } }
        public bool AllergyTreeNuts { get => _allergyTreeNuts; set { if (_allergyTreeNuts == value) return; _allergyTreeNuts = value; OnPropertyChanged(); } }
        public bool AllergyMilk { get => _allergyMilk; set { if (_allergyMilk == value) return; _allergyMilk = value; OnPropertyChanged(); } }
        public bool AllergyEggs { get => _allergyEggs; set { if (_allergyEggs == value) return; _allergyEggs = value; OnPropertyChanged(); } }
        public bool AllergyShellfish { get => _allergyShellfish; set { if (_allergyShellfish == value) return; _allergyShellfish = value; OnPropertyChanged(); } }
        public bool AllergyFish { get => _allergyFish; set { if (_allergyFish == value) return; _allergyFish = value; OnPropertyChanged(); } }
        public bool AllergyWheat { get => _allergyWheat; set { if (_allergyWheat == value) return; _allergyWheat = value; OnPropertyChanged(); } }
        public bool AllergySoy { get => _allergySoy; set { if (_allergySoy == value) return; _allergySoy = value; OnPropertyChanged(); } }
        public bool AllergyLatex { get => _allergyLatex; set { if (_allergyLatex == value) return; _allergyLatex = value; OnPropertyChanged(); } }
        public bool AllergyPenicillin { get => _allergyPenicillin; set { if (_allergyPenicillin == value) return; _allergyPenicillin = value; OnPropertyChanged(); } }
        public bool AllergySulfa { get => _allergySulfa; set { if (_allergySulfa == value) return; _allergySulfa = value; OnPropertyChanged(); } }
        public bool AllergyAspirin { get => _allergyAspirin; set { if (_allergyAspirin == value) return; _allergyAspirin = value; OnPropertyChanged(); } }
        public string? AllergyOtherItems { get => _allergyOtherItems; set { if (_allergyOtherItems == value) return; _allergyOtherItems = value; OnPropertyChanged(); } }    // e.g. "Pollen..."
        public string? Remarks { get; set; }

        // --- Room placement ---
        public string? AdmissionDate { get; set; }
        public string? RoomNumber { get; set; }  // e.g. "202"
        public string? RoomType { get; set; }    // "Single" / "Couple" / "MedicalBackup"
        public string? BedLabel { get; set; }    // "A" / "B" (Couple rooms)

    }
}
