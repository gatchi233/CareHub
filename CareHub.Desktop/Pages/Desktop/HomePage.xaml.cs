using System;
using System.Collections.Generic;
using System.Linq;
using CareHub.Models;
using CareHub.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using CareHub.Services;
using CareHub.Services.Abstractions;
using CareHub.Services.Local;
using CareHub.Desktop.Models;

namespace CareHub.Pages.Desktop
{
    public partial class HomePage : AuthPage
    {
        private readonly MedicationViewModel _vm;
        private readonly IMedicationService _medicationService;
        private readonly IResidentService _residentService;
        private readonly IObservationService _observationService;
        private readonly IMarService _marService;
        private readonly MarJsonService _localMar;
        private readonly MedicationJsonService _localMeds;

        private List<Medication> _allMedications = new();
        private List<Resident> _allResidents = new();
        private List<Medication> _inventoryMeds = new();
        private List<MarEntry> _todayMarEntries = new();
        private static bool _demoSeeded;

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
            _marService = services.GetRequiredService<IMarService>();
            _localMar = services.GetRequiredService<MarJsonService>();
            _localMeds = services.GetRequiredService<MedicationJsonService>();

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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ResidentSearchEntry?.Unfocus();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _vm.InitializeAsync();

                _allResidents = (await _residentService.LoadAsync())?.ToList() ?? new List<Resident>();

                // Ensure packaged resident medications are in local store before loading
                if (!_demoSeeded)
                    await _localMeds.SeedResidentMedicationsAsync(_allResidents);

                _allMedications = (await _medicationService.LoadAsync())?.ToList() ?? new List<Medication>();

                // Remap stale seed ResidentIds to actual resident IDs (in memory)
                RemapStaleResidentIds(_allMedications, _allResidents);

                _inventoryMeds = _allMedications
                    .Where(m => !m.ResidentId.HasValue || m.ResidentId.Value == Guid.Empty)
                    .ToList();

                // Seed demo MAR entries for any meds that don't have entries yet
                try
                {
                    var (fromUtc, toUtc, _, _) = MarScheduleHelper.GetTodayRange();

                    if (!_demoSeeded)
                    {
                        // First load this session: purge old demo entries, re-seed all
                        var allMarLocal = await _localMar.LoadAllAsync();
                        int removed = allMarLocal.RemoveAll(e => e.Notes == "SEED_DEMO");
                        if (removed > 0)
                            await _localMar.ReplaceAllAsync(allMarLocal);

                        await SeedLocalMarDemoAsync();
                        _demoSeeded = true;
                    }

                    var marEntries = await _marService.LoadAsync(null, fromUtc, toUtc);
                    _todayMarEntries = marEntries?.ToList() ?? new List<MarEntry>();
                    _vm.ComputeMarDashboard(_allMedications, _todayMarEntries);
                }
                catch
                {
                    // MAR dashboard is non-critical — skip on failure
                }

                RunSearch();
                _vm.ComputeAlerts();
            }
            catch
            {
                // Offline — show empty state, no crash
                _allMedications ??= new List<Medication>();
                _allResidents ??= new List<Resident>();
            }
        }

        private void OnSearchClicked(object sender, EventArgs e) => RunSearch();

        private void OnClearDayTimeClicked(object sender, EventArgs e)
        {
            if (ResidentSearchEntry != null) ResidentSearchEntry.Text = string.Empty;
            if (DayPicker != null) DayPicker.SelectedIndex = -1;
            if (HourPicker != null) HourPicker.SelectedIndex = -1;
            if (MinutePicker != null) MinutePicker.SelectedIndex = -1;

            RunSearch();
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

        private void ApplyMarStatuses(List<Medication> meds)
        {
            var today = DateTime.Today;
            var dayOfWeek = today.DayOfWeek;
            var matchTolerance = MarScheduleHelper.MatchTolerance;
            var missedCutoff = TimeSpan.FromMinutes(60);

            foreach (var med in meds)
            {
                if (!med.ResidentId.HasValue)
                    continue;

                var times = MarScheduleHelper.GetTimesForDay(med, dayOfWeek);
                int slotsToUse = Math.Max(1, Math.Min(3, med.TimesPerDay));

                for (int i = 0; i < slotsToUse && i < times.Count; i++)
                {
                    var scheduledLocal = today.Add(times[i]);
                    var scheduledUtc = new DateTimeOffset(scheduledLocal, TimeZoneInfo.Local.GetUtcOffset(scheduledLocal)).ToUniversalTime();

                    var match = _todayMarEntries
                        .Where(e =>
                            !e.IsVoided &&
                            e.MedicationId == med.Id &&
                            e.ResidentId == med.ResidentId.Value &&
                            Math.Abs((e.AdministeredAtUtc - scheduledUtc).TotalMinutes) <= matchTolerance.TotalMinutes)
                        .OrderByDescending(e => e.AdministeredAtUtc)
                        .FirstOrDefault();

                    string status = match?.Status ?? "Pending";
                    string? adminTime = match != null
                        ? "@ " + match.AdministeredAtUtc.ToLocalTime().ToString("h:mm tt")
                        : null;

                    // Auto-miss: if scheduled time has passed by >60 min and still no MAR entry
                    if (match == null && DateTime.Now > scheduledLocal.Add(missedCutoff))
                        status = "Missed";

                    switch (i)
                    {
                        case 0: med.Slot1Status = status; med.Slot1AdminTime = adminTime; break;
                        case 1: med.Slot2Status = status; med.Slot2AdminTime = adminTime; break;
                        case 2: med.Slot3Status = status; med.Slot3AdminTime = adminTime; break;
                    }
                }
            }
        }


        private async Task SeedLocalMarDemoAsync()
        {
            var todayLocal = DateTime.Now.Date;
            var dayOfWeek = todayLocal.DayOfWeek;

            var residentMeds = _allMedications
                .Where(m => m.ResidentId.HasValue && m.ResidentId.Value != Guid.Empty)
                .ToList();

            if (residentMeds.Count == 0) return;

            var rng = new Random(42);
            var statuses = new[] { "Given", "Given", "Given", "Refused", "Missed" };
            var nurses = new[] { "Nurse Sarah", "Nurse James", "Nurse Emily" };

            foreach (var med in residentMeds)
            {
                if (!MarScheduleHelper.IsDayEnabled(med, dayOfWeek))
                    continue;

                var times = MarScheduleHelper.GetTimesForDay(med, dayOfWeek);
                int slotsToUse = Math.Max(1, Math.Min(3, med.TimesPerDay));

                for (int i = 0; i < slotsToUse && i < times.Count; i++)
                {
                    var scheduledTime = times[i];
                    if (scheduledTime == TimeSpan.Zero) continue;

                    var scheduledLocal = todayLocal.Add(scheduledTime);

                    // Only seed past slots
                    if (scheduledLocal > DateTime.Now)
                        continue;

                    var scheduledOffset = TimeZoneInfo.Local.GetUtcOffset(scheduledLocal);
                    var scheduledUtc = new DateTimeOffset(scheduledLocal, scheduledOffset).ToUniversalTime();

                    var status = statuses[rng.Next(statuses.Length)];
                    var delayMinutes = status == "Given" ? rng.Next(1, 15) : 0;
                    var administeredUtc = status == "Given"
                        ? scheduledUtc.AddMinutes(delayMinutes)
                        : scheduledUtc;

                    var entry = new MarEntry
                    {
                        Id = Guid.NewGuid(),
                        ClientRequestId = Guid.NewGuid(),
                        ResidentId = med.ResidentId!.Value,
                        MedicationId = med.Id,
                        Status = status,
                        DoseQuantity = med.Quantity > 0 ? med.Quantity : 1,
                        DoseUnit = med.QuantityUnit ?? "tablet",
                        ScheduledForUtc = scheduledUtc,
                        AdministeredAtUtc = administeredUtc,
                        RecordedBy = nurses[rng.Next(nurses.Length)],
                        MedicationName = med.MedName,
                        ResidentName = med.ResidentName ?? "Unknown",
                        Notes = "SEED_DEMO",
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        UpdatedAtUtc = DateTimeOffset.UtcNow,
                    };

                    await _localMar.CreateAsync(entry);
                }
            }
        }

        private async void OnViewLowStockClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MedicationInventoryPage?filter=lowstock");
        }

        private async void OnViewExpiryAlertClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MedicationInventoryPage?filter=expiry");
        }

        private async void OnViewMarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MarPage");
        }

        /// <summary>
        /// Fix medications whose ResidentId doesn't match any loaded resident.
        /// Builds a name-based lookup from packaged Residents.json seed IDs
        /// to actual resident IDs, then remaps in memory.
        /// </summary>
        private void RemapStaleResidentIds(List<Medication> meds, List<Resident> residents)
        {
            if (residents.Count == 0) return;

            var validIds = new HashSet<Guid>(residents.Select(r => r.Id));

            // Check if any medications have unmatched ResidentIds
            var orphans = meds.Where(m =>
                m.ResidentId.HasValue
                && m.ResidentId.Value != Guid.Empty
                && !validIds.Contains(m.ResidentId.Value)).ToList();

            if (orphans.Count == 0) return;

            // Build seed-ID → resident-name lookup from packaged Residents.json
            Dictionary<Guid, string> seedIdToName;
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("Residents.json").GetAwaiter().GetResult();
                var seedResidents = System.Text.Json.JsonSerializer.DeserializeAsync<List<Resident>>(stream)
                    .GetAwaiter().GetResult() ?? new List<Resident>();
                seedIdToName = seedResidents.ToDictionary(
                    r => r.Id,
                    r => $"{r.ResidentFName} {r.ResidentLName}".Trim());
            }
            catch { return; }

            // Build name → actual-ID lookup
            var nameToActualId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in residents)
            {
                var name = $"{r.ResidentFName} {r.ResidentLName}".Trim();
                nameToActualId.TryAdd(name, r.Id);
            }

            foreach (var med in orphans)
            {
                if (seedIdToName.TryGetValue(med.ResidentId!.Value, out var name)
                    && nameToActualId.TryGetValue(name, out var actualId))
                {
                    med.ResidentId = actualId;
                }
            }
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
                // Skip inventory items — they have no resident
                if (!med.ResidentId.HasValue || med.ResidentId.Value == Guid.Empty)
                    continue;

                Resident? resident = _allResidents.FirstOrDefault(r => r.Id == med.ResidentId);

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

                    var times = MarScheduleHelper.GetTimesForDay(med, dayFilter.Value);
                    int slotsToUse = Math.Max(1, Math.Min(3, med.TimesPerDay));
                    bool timeMatch = false;
                    for (int i = 0; i < slotsToUse && i < times.Count; i++)
                    {
                        if (times[i] == selectedTime) { timeMatch = true; break; }
                    }
                    if (!timeMatch)
                        continue;
                }

                results.Add(med);
            }

            // Deduplicate by (MedName, ResidentId) — keeps the first occurrence per group.
            // This handles any existing duplicates in the API database.
            var deduped = results
                .GroupBy(m => $"{(m.MedName ?? "").ToLowerInvariant()}|{m.ResidentId}")
                .Select(g => g.First())
                .ToList();

            var sorted = deduped.OrderBy(x => x.ResidentName).ThenBy(x => x.MedName).ToList();
            ApplyMarStatuses(sorted);

            _vm.Medications.Clear();
            foreach (var m in sorted)
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

    }
}
