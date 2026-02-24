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

        public IList<string> Roles { get; } = new List<string> { "Admin", "Staff" };

        private StaffRecord? _selected;
        public StaffRecord? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                    LoadFromSelected();
            }
        }

        // Editor fields
        private string _employeeId = "";
        public string EmployeeId { get => _employeeId; set => SetProperty(ref _employeeId, value); }

        private string _firstName = "";
        public string FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }

        private string _lastName = "";
        public string LastName { get => _lastName; set => SetProperty(ref _lastName, value); }

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

        private string _firstAidExpiry = "";
        public string FirstAidExpiry { get => _firstAidExpiry; set => SetProperty(ref _firstAidExpiry, value); }

        private bool _foodSafeCertified;
        public bool FoodSafeCertified { get => _foodSafeCertified; set => SetProperty(ref _foodSafeCertified, value); }

        private string _role = "Staff";
        public string Role { get => _role; set => SetProperty(ref _role, value); }

        private bool _isEnabled = true;
        public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

        private string _error = "";
        public string Error { get => _error; set => SetProperty(ref _error, value); }

        public string ToggleEnabledText => IsEnabled ? "Disable" : "Enable";

        public ICommand RefreshCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ToggleEnabledCommand { get; }

        public StaffManagementViewModel(IStaffService staffService)
        {
            _staffService = staffService;

            RefreshCommand = new Command(async () => await RefreshAsync());
            NewCommand = new Command(NewStaff);
            SaveCommand = new Command(async () => await SaveAsync());
            ToggleEnabledCommand = new Command(async () => await ToggleEnabledAsync());
        }

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

        private void NewStaff()
        {
            Selected = null;
            EmployeeId = "";
            FirstName = "";
            LastName = "";
            JobTitle = "";
            Department = "";
            EmploymentStatus = "";
            HourlyWage = 0;
            ShiftPreference = "";
            FirstAidExpiry = "";
            FoodSafeCertified = false;
            Role = "Staff";
            IsEnabled = true;
            Error = "";
            OnPropertyChanged(nameof(ToggleEnabledText));
        }

        private void LoadFromSelected()
        {
            if (Selected == null) return;

            EmployeeId = Selected.EmployeeId;
            FirstName = Selected.FirstName;
            LastName = Selected.LastName;
            JobTitle = Selected.JobTitle;
            Department = Selected.Department;
            EmploymentStatus = Selected.EmploymentStatus;
            HourlyWage = Selected.HourlyWage;
            ShiftPreference = Selected.ShiftPreference;

            FirstAidExpiry = Selected.Compliance?.FirstAidExpiry ?? "";
            FoodSafeCertified = Selected.Compliance?.FoodSafeCertified ?? false;

            Role = string.IsNullOrWhiteSpace(Selected.Role) ? "Staff" : Selected.Role;
            IsEnabled = Selected.IsEnabled;

            OnPropertyChanged(nameof(ToggleEnabledText));
        }

        private async Task SaveAsync()
        {
            Error = "";

            if (string.IsNullOrWhiteSpace(EmployeeId))
            {
                Error = "EmployeeId is required (e.g. EMP-019).";
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                Error = "FirstName and LastName are required.";
                return;
            }

            try
            {
                var record = new StaffRecord
                {
                    EmployeeId = EmployeeId.Trim(),
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    JobTitle = JobTitle.Trim(),
                    Department = Department.Trim(),
                    EmploymentStatus = EmploymentStatus.Trim(),
                    HourlyWage = HourlyWage,
                    ShiftPreference = ShiftPreference.Trim(),
                    Role = string.IsNullOrWhiteSpace(Role) ? "Staff" : Role,
                    IsEnabled = IsEnabled,
                    Compliance = new StaffCompliance
                    {
                        FirstAidExpiry = FirstAidExpiry.Trim(),
                        FoodSafeCertified = FoodSafeCertified
                    }
                };

                await _staffService.AddOrUpdateAsync(record);
                await RefreshAsync();

                Selected = Staff.FirstOrDefault(s =>
                    s.EmployeeId.Equals(record.EmployeeId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        private async Task ToggleEnabledAsync()
        {
            if (string.IsNullOrWhiteSpace(EmployeeId))
                return;

            try
            {
                var newValue = !IsEnabled;
                await _staffService.SetEnabledAsync(EmployeeId, newValue);

                IsEnabled = newValue;
                OnPropertyChanged(nameof(ToggleEnabledText));

                await RefreshAsync();

                Selected = Staff.FirstOrDefault(s =>
                    s.EmployeeId.Equals(EmployeeId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool SetProperty<T>(ref T backing, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(backing, value))
                return false;

            backing = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
