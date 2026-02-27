using System;
using System.Collections.Generic;
using System.Linq;
using MedReminder.Models;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MedReminder.Services.Abstractions;

namespace MedReminder.Pages.Desktop
{
    public partial class HomePage : AuthPage
    {
        private readonly MedicationViewModel _vm;
        private readonly IMedicationService _medicationService;
        private readonly IResidentService _residentService;
        private readonly IObservationService _observationService;

        private List<Medication> _allMedications = new();
        private List<Resident> _allResidents = new();

        // MUST be parameterless (Shell creates pages)
        public HomePage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            _vm = services.GetRequiredService<MedicationViewModel>();
            _medicationService = services.GetRequiredService<IMedicationService>();
            _residentService = services.GetRequiredService<IResidentService>();
            _observationService = services.GetRequiredService<IObservationService>();

            BindingContext = _vm;

            // Build Hour/Minute pickers in code (consistent, avoids hardcoded XAML lists)
            if (HourPicker != null)
            {
                HourPicker.Items.Clear();
                for (int h = 0; h < 24; h++)
                    HourPicker.Items.Add(h.ToString("00"));
            }

            if (MinutePicker != null)
            {
                MinutePicker.Items.Clear();
                MinutePicker.Items.Add("00");
                MinutePicker.Items.Add("15");
                MinutePicker.Items.Add("30");
                MinutePicker.Items.Add("45");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await _vm.InitializeAsync();

            _allMedications = _vm.Medications?.ToList() ?? new List<Medication>();
            _allResidents = (await _residentService.LoadAsync())?.ToList() ?? new List<Resident>();

            RunSearch();
        }

        private void OnResidentSearchClicked(object sender, EventArgs e) => RunSearch();
        private void OnSearchClicked(object sender, EventArgs e) => RunSearch();

        private void OnClearDayTimeClicked(object sender, EventArgs e)
        {
            if (DayPicker != null) DayPicker.SelectedIndex = -1;
            if (HourPicker != null) HourPicker.SelectedIndex = -1;
            if (MinutePicker != null) MinutePicker.SelectedIndex = -1;

            RunSearch();
        }

        private async void OnAddResidentClicked(object sender, EventArgs e)
        {
            // Standardized route
            await Shell.Current.GoToAsync(nameof(EditResidentPage));
        }

        private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0)
                return;

            if (e.CurrentSelection[0] is not Medication m)
                return;

            if (MedList != null)
                MedList.SelectedItem = null;

            if (!m.ResidentId.HasValue || m.ResidentId.Value <= Guid.Empty)
            {
                await DisplayAlert("Missing resident", "This medication is not linked to a resident.", "OK");
                return;
            }

            // Standardized route
            await Shell.Current.GoToAsync($"{nameof(ViewResidentPage)}?id={m.ResidentId.Value}");
        }

        // HomePage.xaml uses CheckBox.CheckedChanged
        private async void OnDoneCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Medication m)
                return;

            m.IsDone = e.Value;

            await _medicationService.UpsertAsync(m);
        }


        private void RunSearch()
        {
            _allMedications ??= new List<Medication>();
            _allResidents ??= new List<Resident>();

            string residentQuery = ResidentSearchEntry?.Text?.Trim() ?? string.Empty;
            bool hasResidentFilter = !string.IsNullOrWhiteSpace(residentQuery);

            DayOfWeek? dayFilter = GetSelectedDayOfWeek();

            bool hasHour = HourPicker?.SelectedIndex >= 0;
            bool hasMin = MinutePicker?.SelectedIndex >= 0;

            bool hasTimeSelected = dayFilter.HasValue && hasHour;
            TimeSpan selectedTime = TimeSpan.Zero;

            if (hasHour)
            {
                int hour = int.Parse(HourPicker.Items[HourPicker.SelectedIndex]);
                int minute = hasMin ? int.Parse(MinutePicker.Items[MinutePicker.SelectedIndex]) : 0;
                selectedTime = new TimeSpan(hour, minute, 0);
            }

            var results = new List<Medication>();

            foreach (var med in _allMedications)
            {
                Resident? resident = null;

                if (med.ResidentId.HasValue)
                    resident = _allResidents.FirstOrDefault(r => r.Id == med.ResidentId);

                // Prefer linked resident; fall back to any existing name or a placeholder.
                med.ResidentName = resident?.ResidentName
                    ?? med.ResidentName
                    ?? "Unassigned";

                if (hasResidentFilter)
                {
                    var fullName = resident?.ResidentName ?? string.Empty;
                    if (resident == null ||
                        !fullName.Contains(residentQuery, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (hasTimeSelected)
                {
                    bool dayEnabled = dayFilter.Value switch
                    {
                        DayOfWeek.Monday => med.ReminderMon,
                        DayOfWeek.Tuesday => med.ReminderTue,
                        DayOfWeek.Wednesday => med.ReminderWed,
                        DayOfWeek.Thursday => med.ReminderThu,
                        DayOfWeek.Friday => med.ReminderFri,
                        DayOfWeek.Saturday => med.ReminderSat,
                        DayOfWeek.Sunday => med.ReminderSun,
                        _ => true
                    };

                    if (!dayEnabled)
                        continue;

                    if (med.ReminderTime != selectedTime)
                        continue;
                }

                results.Add(med);
            }

            _vm.Medications.Clear();
            foreach (var m in results.OrderBy(x => x.ResidentName).ThenBy(x => x.MedName))
                _vm.Medications.Add(m);
        }

        private DayOfWeek? GetSelectedDayOfWeek()
        {
            if (DayPicker == null || DayPicker.SelectedIndex < 0)
                return null;

            return DayPicker.SelectedIndex switch
            {
                0 => DayOfWeek.Monday,
                1 => DayOfWeek.Tuesday,
                2 => DayOfWeek.Wednesday,
                3 => DayOfWeek.Thursday,
                4 => DayOfWeek.Friday,
                5 => DayOfWeek.Saturday,
                6 => DayOfWeek.Sunday,
                _ => null
            };
        }

        protected async Task LogoutAsync()
        {
            var auth = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<MedReminder.Services.AuthService>();

            auth?.Logout();

            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
        }

        // Add this event handler for the ToolbarItem
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await LogoutAsync();
        }
    }
}
