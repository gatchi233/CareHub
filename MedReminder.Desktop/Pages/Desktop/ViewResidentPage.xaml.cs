using MedReminder.Models;
using MedReminder.Services;
using MedReminder.Services.Abstractions;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MedReminder.Pages.Desktop
{
    [QueryProperty(nameof(ResidentId), "id")]
    [QueryProperty(nameof(ReturnTo), "returnTo")]
    public partial class ViewResidentPage : AuthPage
    {
        private readonly IResidentService _residentService;
        private readonly IMedicationService _medicationService;

        public Guid ResidentId { get; set; }

        // Where to return when Close is pressed (e.g. //FloorPlanPage or //ResidentsPage)
        public string? ReturnTo { get; set; }

        private Resident? _resident;
        private Guid _ResidentID;

        // Bind this from XAML via x:Reference (so we can keep BindingContext = Resident)
        public ObservableCollection<Medication> MedicationSchedules { get; } = new();

        public ViewResidentPage(IResidentService residentService, IMedicationService medicationService)
        {
            InitializeComponent();
            _residentService = residentService;
            _medicationService = medicationService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var auth = MauiProgram.Services.GetService<AuthService>();
            var canEditResident = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;

            if (EditAction != null)
            {
                // Disable the Edit action
                EditAction.IsEnabled = canEditResident;
                EditAction.InputTransparent = !canEditResident;
                EditAction.Opacity = canEditResident ? 1.0 : 0.45;
            }

            var residents = await _residentService.LoadAsync();
            _resident = residents.FirstOrDefault(r => r.Id == Guid.Empty);

            if (_resident == null)
            {
                await DisplayAlert("Not found", "Resident not found.", "OK");
                await GoBackAsync();
                return;
            }

            // Keep existing bindings (XAML binds to Resident fields)
            BindingContext = _resident;

            DobAgeLabel.Text = BuildDobAgeText(_resident.DOB);

            // Load medication schedule for this resident
            MedicationSchedules.Clear();
            var allMeds = await _medicationService.LoadAsync();
            var residentMeds = allMeds
                .Where(m => m.ResidentId.HasValue && m.ResidentId.Value == _ResidentID)
                .OrderBy(m => m.MedName ?? string.Empty);

            foreach (var m in residentMeds)
                MedicationSchedules.Add(m);
        }

        private async Task GoBackAsync()
        {
            var target = string.IsNullOrWhiteSpace(ReturnTo)
                ? $"//{nameof(ResidentsPage)}"
                : ReturnTo;

            await Shell.Current.GoToAsync(target);
        }

        private string BuildDobAgeText(string? dobString)
        {
            if (string.IsNullOrWhiteSpace(dobString))
                return "DOB: (not recorded)";

            if (!DateTime.TryParse(dobString, out var dob))
                return $"DOB: {dobString}";

            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age))
                age--;

            return $"DOB: {dob:yyyy-MM-dd}  (Age {age})";
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            var auth = MauiProgram.Services.GetService<AuthService>();
            var canEditResident = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;

            if (!canEditResident)
            {
                await DisplayAlert("Access denied", "You don't have permission to edit resident records.", "OK");
                return;
            }

            if (_resident == null)
                return;

            // Pass returnTo forward so EditResidentPage can Close back correctly too
            var returnTo = Uri.EscapeDataString(string.IsNullOrWhiteSpace(ReturnTo)
                ? $"//{nameof(ResidentsPage)}"
                : ReturnTo);

            await Shell.Current.GoToAsync($"{nameof(EditResidentPage)}?id={ResidentId}&returnTo={returnTo}");
        }

        private async void OnViewMedicationsClicked(object sender, EventArgs e)
        {
            if (_resident == null)
                return;

            var parameters = new Dictionary<string, object?>
            {
                ["residentId"] = _resident.Id,
                ["residentName"] = _resident.FullName
            };

            await Shell.Current.GoToAsync(nameof(ResidentMedicationsPage), true, parameters);
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await GoBackAsync();
        }

        private async void OnReportClicked(object sender, EventArgs e)
        {
            if (_resident == null)
                return;

            await Shell.Current.GoToAsync(
                nameof(ResidentReportPage),
                true,
                new Dictionary<string, object>
                {
                    ["residentId"] = _resident.Id,
                    // If you later add ReturnTo to report, pass it too
                });
        }
    }
}
