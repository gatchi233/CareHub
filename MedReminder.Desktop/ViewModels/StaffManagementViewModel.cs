using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedReminder.Models;
using MedReminder.Services.Local;
using MedReminder.Services.Abstractions;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.CompilerServices;

namespace MedReminder.ViewModels
{
    public class StaffManagementViewModel : INotifyPropertyChanged
    {
        private readonly IStaffService _staffService;

        public ObservableCollection<StaffRecord> Staff { get; } = new();
        public IList<string> Roles { get; } = new List<string> { "Admin", "Care", "Kitchen", "Facilities" };

        // ── Selection & mode ────────────────────────────────────────────────

        private StaffRecord? _selected;
        public StaffRecord? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    LoadFromSelected();
                    IsEditing = false;   // clicking a card shows view mode
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CannotEdit));
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    OnPropertyChanged(nameof(PanelTitle));
                    OnPropertyChanged(nameof(IsNotEditing));
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CannotEdit));
                    OnPropertyChanged(nameof(CanEditFirstAidExpiry));
                    OnPropertyChanged(nameof(CanEditFoodSafeExpiry));
                }
            }
        }

        public bool IsNotEditing => !IsEditing;
        public bool HasSelection => Selected != null;
        public bool CanEdit     => HasSelection && !IsEditing;
        public bool CannotEdit  => !CanEdit;
        public string PanelTitle => IsEditing ? "EDIT STAFF RECORD" : "VIEW STAFF RECORD";

        // ── Editor fields ────────────────────────────────────────────────────

        private string _employeeId = "";
        public string EmployeeId { get => _employeeId; set => SetProperty(ref _employeeId, value); }

        private string _staffFName = "";
        public string StaffFName { get => _staffFName; set => SetProperty(ref _staffFName, value); }

        private string _staffLName = "";
        public string StaffLName { get => _staffLName; set => SetProperty(ref _staffLName, value); }

        private string _jobTitle = "";
        public string JobTitle { get => _jobTitle; set => SetProperty(ref _jobTitle, value); }

        private string _department = "";
        public string Department { get => _department; set => SetProperty(ref _department, value); }

        private string _employmentStatus = "";
        public string EmploymentStatus { get => _employmentStatus; set => SetProperty(ref _employmentStatus, value); }

        private decimal _hourlyWage;
        public decimal HourlyWage { get => _hourlyWage; set => SetProperty(ref _hourlyWage, value); }

        private string _shiftPreference = "";
        public string ShiftPreference { get => _shiftPreference; set => SetProperty(ref _shiftPreference, value); }

        // ── Compliance ───────────────────────────────────────────────────────

        private bool _hasFirstAid;
        public bool HasFirstAid
        {
            get => _hasFirstAid;
            set
            {
                if (SetProperty(ref _hasFirstAid, value))
                    OnPropertyChanged(nameof(CanEditFirstAidExpiry));
            }
        }

        private string _firstAidExpiry = "";
        public string FirstAidExpiry
        {
            get => _firstAidExpiry;
            set
            {
                if (SetProperty(ref _firstAidExpiry, value) && !string.IsNullOrWhiteSpace(value))
                    HasFirstAid = true;
            }
        }

        // IsEditing AND HasFirstAid both required to type in the expiry date
        public bool CanEditFirstAidExpiry => IsEditing && HasFirstAid;

        private bool _foodSafeCertified;
        public bool FoodSafeCertified
        {
            get => _foodSafeCertified;
            set
            {
                if (SetProperty(ref _foodSafeCertified, value))
                    OnPropertyChanged(nameof(CanEditFoodSafeExpiry));
            }
        }

        private string _foodSafeExpiry = "";
        public string FoodSafeExpiry
        {
            get => _foodSafeExpiry;
            set
            {
                if (SetProperty(ref _foodSafeExpiry, value) && !string.IsNullOrWhiteSpace(value))
                    FoodSafeCertified = true;
            }
        }

        public bool CanEditFoodSafeExpiry => IsEditing && FoodSafeCertified;

        // ── Account ──────────────────────────────────────────────────────────

        private string _role = "Care";
        public string Role { get => _role; set => SetProperty(ref _role, value); }

        private bool _isEnabled = true;
        public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

        // ── Error ────────────────────────────────────────────────────────────

        private string _error = "";
        public string Error { get => _error; set => SetProperty(ref _error, value); }

        // ── Commands ─────────────────────────────────────────────────────────

        public ICommand RefreshCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SetActiveCommand { get; }
        public ICommand SetInactiveCommand { get; }

        public StaffManagementViewModel(IStaffService staffService)
        {
            _staffService = staffService;

            RefreshCommand = new Command(async () => await RefreshAsync());
            NewCommand     = new Command(NewStaff);
            SaveCommand    = new Command(async () => await SaveAsync());

            // Called from the EDIT button in the toolbar — enters edit mode for the selected record
            EditCommand       = new Command(() => IsEditing = true);
            SetActiveCommand   = new Command(() => IsEnabled = true);
            SetInactiveCommand = new Command(() => IsEnabled = false);
        }

        // ── Data loading ─────────────────────────────────────────────────────

        public async Task RefreshAsync()
        {
            Error = "";
            try
            {
                Staff.Clear();
                var items = await _staffService.GetAllAsync();
                foreach (var s in items.OrderBy(s => s.EmployeeId))
                    Staff.Add(s);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        private void LoadFromSelected()
        {
            if (Selected == null) return;

            EmployeeId       = Selected.EmployeeId;
            StaffFName       = Selected.StaffFName;
            StaffLName       = Selected.StaffLName;
            JobTitle         = Selected.JobTitle;
            Department       = Selected.Department;
            EmploymentStatus = Selected.EmploymentStatus;
            HourlyWage       = Selected.HourlyWage;
            ShiftPreference  = Selected.ShiftPreference;

            HasFirstAid      = Selected.Compliance?.HasFirstAid      ?? false;
            FirstAidExpiry   = Selected.Compliance?.FirstAidExpiry   ?? "";
            FoodSafeCertified = Selected.Compliance?.FoodSafeCertified ?? false;
            FoodSafeExpiry   = Selected.Compliance?.FoodSafeExpiry   ?? "";

            Role      = string.IsNullOrWhiteSpace(Selected.Role) ? "Care" : Selected.Role;
            IsEnabled = Selected.IsEnabled;
        }

        // ── Actions ──────────────────────────────────────────────────────────

        private void NewStaff()
        {
            Selected         = null;
            EmployeeId       = GenerateNextEmployeeId();
            StaffFName       = "";
            StaffLName       = "";
            JobTitle         = "";
            Department       = "";
            EmploymentStatus = "";
            HourlyWage       = 0;
            ShiftPreference  = "";
            HasFirstAid      = false;
            FirstAidExpiry   = "";
            FoodSafeCertified = false;
            FoodSafeExpiry   = "";
            Role             = "Care";
            IsEnabled        = true;
            Error            = "";
            IsEditing        = true;
        }

        private string GenerateNextEmployeeId()
        {
            int max = 0;
            foreach (var s in Staff)
            {
                if (s.EmployeeId.StartsWith("EMP-", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(s.EmployeeId[4..], out int n) && n > max)
                    max = n;
            }
            return $"EMP-{max + 1:D3}";
        }

        private async Task SaveAsync()
        {
            Error = "";

            if (string.IsNullOrWhiteSpace(EmployeeId))
            {
                Error = "Employee ID is required (e.g. EMP-021).";
                return;
            }

            if (string.IsNullOrWhiteSpace(StaffFName) || string.IsNullOrWhiteSpace(StaffLName))
            {
                Error = "First name and last name are required.";
                return;
            }

            try
            {
                var record = new StaffRecord
                {
                    EmployeeId       = EmployeeId.Trim(),
                    StaffFName       = StaffFName.Trim(),
                    StaffLName       = StaffLName.Trim(),
                    JobTitle         = JobTitle.Trim(),
                    Department       = Department.Trim(),
                    EmploymentStatus = EmploymentStatus.Trim(),
                    HourlyWage       = HourlyWage,
                    ShiftPreference  = ShiftPreference.Trim(),
                    Role             = string.IsNullOrWhiteSpace(Role) ? "Care" : Role,
                    IsEnabled        = IsEnabled,
                    Compliance       = new StaffCompliance
                    {
                        HasFirstAid       = HasFirstAid,
                        FirstAidExpiry    = HasFirstAid ? FirstAidExpiry.Trim() : "",
                        FoodSafeCertified = FoodSafeCertified,
                        FoodSafeExpiry    = FoodSafeCertified ? FoodSafeExpiry.Trim() : ""
                    }
                };

                await _staffService.AddOrUpdateAsync(record);
                await RefreshAsync();

                // Re-select the saved record and drop back to view mode
                Selected  = Staff.FirstOrDefault(s =>
                    s.EmployeeId.Equals(record.EmployeeId, StringComparison.OrdinalIgnoreCase));
                IsEditing = false;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        // ── INPC boilerplate ─────────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool SetProperty<T>(ref T backing, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(backing, value)) return false;
            backing = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
