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

        private Guid _residentId = Guid.Empty;
        public string? ResidentId
        {
            get => _residentId == Guid.Empty ? null : _residentId.ToString();
            set
            {
                _residentId = Guid.TryParse(value, out var id) ? id : Guid.Empty;
            }
        }

        // Where to return when Close is pressed (e.g. //FloorPlanPage or //ResidentsPage)
        public string? ReturnTo { get; set; }

private Resident? _resident;
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
            _resident = residents.FirstOrDefault(r => r.Id == _residentId);

            if (_resident == null)
            {
                await DisplayAlert("Not found", "Resident not found.", "OK");
                await GoBackAsync();
                return;
            }

            // Keep existing bindings (XAML binds to Resident fields)
            BindingContext = _resident;

            DobAgeLabel.Text = BuildDobAgeText(_resident.DOB);
            AllergySummaryLabel.Text = BuildAllergySummary(_resident);

            // Load medication schedule for this resident
            MedicationSchedules.Clear();
            var allMeds = await _medicationService.LoadAsync();
            var residentMeds = allMeds
                .Where(m => m.ResidentId.HasValue && m.ResidentId.Value == _residentId)
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

        private static string BuildAllergySummary(Resident r)
        {
            if (r.AllergyNone) return "No known allergies.";

            var items = new List<string>();
            if (r.AllergyPeanuts)   items.Add("Peanuts");
            if (r.AllergyTreeNuts)  items.Add("Tree nuts");
            if (r.AllergyMilk)      items.Add("Milk");
            if (r.AllergyEggs)      items.Add("Eggs");
            if (r.AllergyShellfish) items.Add("Shellfish");
            if (r.AllergyFish)      items.Add("Fish");
            if (r.AllergyWheat)     items.Add("Wheat");
            if (r.AllergySoy)       items.Add("Soy");
            if (r.AllergyLatex)     items.Add("Latex");
            if (r.AllergyPenicillin)items.Add("Penicillin");
            if (r.AllergySulfa)     items.Add("Sulfa");
            if (r.AllergyAspirin)   items.Add("Aspirin");
            if (!string.IsNullOrWhiteSpace(r.AllergyOtherItems))
                items.Add(r.AllergyOtherItems);

            return items.Count > 0 ? string.Join(", ", items) : "Not recorded.";
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

        private async void OnEditClicked(object sender, TappedEventArgs e)
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

            await Shell.Current.GoToAsync($"{nameof(EditResidentPage)}?id={_residentId}&returnTo={returnTo}");
        }

        private async void OnViewMedicationsClicked(object sender, TappedEventArgs e)
        {
            if (_resident == null)
                return;

            var returnTo = BuildReturnToForSelf();
            var parameters = new Dictionary<string, object?>
            {
                ["residentId"] = _resident.Id,
                ["residentName"] = _resident.ResidentName,
                ["returnTo"] = returnTo
            };

            await Shell.Current.GoToAsync(nameof(ResidentMedicationsPage), true, parameters);
        }

        private async void OnCloseClicked(object sender, TappedEventArgs e)
        {
            await GoBackAsync();
        }

        private async void OnObserviationsClicked(object sender, TappedEventArgs e)
        {
            if (_resident == null)
                return;

            var returnTo = BuildReturnToForSelf();
            await Shell.Current.GoToAsync(
                nameof(ResidentObservationsPage),
                true,
                new Dictionary<string, object>
                {
                    ["residentId"] = _resident.Id,
                    ["returnTo"] = returnTo
                });
        }

        private string BuildReturnToForSelf()
        {
            var baseReturn = string.IsNullOrWhiteSpace(ReturnTo)
                ? $"//{nameof(ResidentsPage)}"
                : ReturnTo;

            return $"{nameof(ViewResidentPage)}?id={_residentId}&returnTo={baseReturn}";
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
