using CareHub.Desktop.Models;
using CareHub.Models;
using CareHub.Services.Abstractions;
using CareHub.Services.Remote;
using CareHub.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CareHub.Pages.Desktop
{
    [QueryProperty(nameof(ResidentId), "residentId")]
    [QueryProperty(nameof(ResidentName), "residentName")]
    [QueryProperty(nameof(ReturnTo), "returnTo")]
    public partial class ResidentMedicationsPage : AuthPage
    {
        private readonly IMedicationService _medicationService;
        private readonly IMarService _marService;

        public Guid ResidentId { get; set; }
        public string? ResidentName { get; set; }
        public string? ReturnTo { get; set; }

        public ObservableCollection<Medication> Medications { get; } =
            new ObservableCollection<Medication>();

        // MAR data
        public ObservableCollection<MarTimeSlotGroup> MarGroups { get; } = new();
        private MarRange _currentMarRange = MarRange.Today;

        private enum MarRange { Today, Last3Days, Last7Days }

        public ResidentMedicationsPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            _medicationService = services.GetRequiredService<IMedicationService>();
            _marService = services.GetRequiredService<IMarService>();

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            HeaderLabel.Text = string.IsNullOrWhiteSpace(ResidentName)
                ? "Resident Medications"
                : ResidentName;

            await LoadMedicationsAsync();
            await LoadMarAsync();
            UpdateMarFilterButtons();
        }

        private async Task LoadMedicationsAsync()
        {
            try
            {
                var all = await _medicationService.LoadAsync();
                var filtered = all.Where(m => m.ResidentId == ResidentId)
                                  .OrderBy(m => m.MedName ?? string.Empty);

                Medications.Clear();
                int index = 1;
                foreach (var med in filtered)
                {
                    med.DisplayIndex = index++;
                    Medications.Add(med);
                }
            }
            catch
            {
                // Offline — keep whatever's currently in the list
            }
        }

        private async Task LoadMarAsync()
        {
            try
            {
                MarLoadingIndicator.IsRunning = true;
                MarLoadingIndicator.IsVisible = true;

                var (fromUtc, toUtc, fromLocal, toLocal) = GetDateRange(_currentMarRange);

                // Load medications for slot generation
                var allMeds = await _medicationService.LoadAsync();
                var residentMeds = allMeds.Where(m => m.ResidentId == ResidentId).ToList();

                // Load MAR entries (use null filter like HomePage, then filter client-side
                // to avoid mismatches between API/local resident ID mappings)
                var allMarEntries = await _marService.LoadAsync(null, fromUtc, toUtc);
                var marEntries = allMarEntries
                    .Where(e => e.ResidentId == ResidentId)
                    .ToList();

                // Generate schedule slots
                var slots = MarScheduleHelper.GenerateSlots(residentMeds, fromLocal, toLocal);

                // Overlay MAR entries onto slots
                var matchedEntryIds = new HashSet<Guid>();
                MarScheduleHelper.OverlayMarEntries(slots, marEntries, matchedEntryIds);

                // Mark past unmatched slots as Missed (>60 min past scheduled time)
                var now = DateTimeOffset.Now;
                foreach (var slot in slots)
                {
                    if (slot.Status == "Pending" && now > slot.ScheduledForUtc.AddMinutes(60))
                        slot.Status = "Missed";
                }

                // Find unscheduled entries
                var unscheduledSlots = marEntries
                    .Where(e => !matchedEntryIds.Contains(e.Id) && !e.IsVoided)
                    .Select(e => new MarSlotViewModel
                    {
                        ResidentId = e.ResidentId,
                        MedicationId = e.MedicationId,
                        MedicationName = e.MedicationName,
                        DoseQuantity = e.DoseQuantity,
                        DoseUnit = e.DoseUnit,
                        Status = e.Status,
                        IsUnscheduled = true,
                        LocalDate = e.AdministeredAtUtc.ToLocalTime().Date,
                        ScheduledLocalTime = e.AdministeredAtUtc.ToLocalTime().ToString("HH:mm"),
                        LastAdministeredLocal = e.AdministeredAtUtc.ToLocalTime().ToString("HH:mm"),
                        RecordedBy = e.RecordedBy,
                        NotesPreview = e.Notes
                    })
                    .ToList();

                // Merge scheduled + unscheduled, then group by date+time
                var allSlotsCombined = slots.Concat(unscheduledSlots).ToList();

                var groups = allSlotsCombined
                    .GroupBy(s => new { s.LocalDate.Date, s.ScheduledLocalTime })
                    .OrderBy(g => g.Key.Date)
                    .ThenBy(g => g.Key.ScheduledLocalTime)
                    .Select(g =>
                    {
                        // Deduplicate: keep one entry per medication.
                        // Prefer the entry that has a real status (not Pending) over Pending.
                        var deduped = g
                            .GroupBy(s => s.MedicationId)
                            .Select(mg => mg
                                .OrderBy(s => s.Status == "Pending" ? 1 : 0)
                                .First())
                            .ToList();

                        return new MarTimeSlotGroup
                        {
                            LocalDate = g.Key.Date,
                            ScheduledLocalTime = g.Key.ScheduledLocalTime,
                            ScheduledForUtc = deduped.First().ScheduledForUtc,
                            Slots = deduped
                        };
                    })
                    .ToList();

                MarGroups.Clear();
                foreach (var g in groups)
                    MarGroups.Add(g);

                var rangeLabel = _currentMarRange switch
                {
                    MarRange.Today => "Today",
                    MarRange.Last3Days => "Last 3 days",
                    MarRange.Last7Days => "Last 7 days",
                    _ => ""
                };
                var totalMeds = MarGroups.Sum(g => g.Slots.Count);
                MarStatusLabel.Text = $"{rangeLabel} \u2022 {MarGroups.Count} time slots, {totalMeds} records";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MAR-LOAD-ERROR] {ex}");
                MarStatusLabel.Text = "Unable to load MAR records.";
            }
            finally
            {
                MarLoadingIndicator.IsRunning = false;
                MarLoadingIndicator.IsVisible = false;
            }
        }

        private static (DateTime fromUtc, DateTime toUtc, DateTime fromLocal, DateTime toLocal) GetDateRange(MarRange range)
        {
            var todayLocal = DateTime.Now.Date;

            var fromLocal = range switch
            {
                MarRange.Today => todayLocal,
                MarRange.Last3Days => todayLocal.AddDays(-2),
                MarRange.Last7Days => todayLocal.AddDays(-6),
                _ => todayLocal
            };

            var toLocal = todayLocal.AddDays(1);
            var fromUtc = fromLocal.ToUniversalTime();
            var toUtc = toLocal.ToUniversalTime();

            return (fromUtc, toUtc, fromLocal, toLocal);
        }

        private void UpdateMarFilterButtons()
        {
            Color activeColor, inactiveColor;
            Application.Current!.Resources.TryGetValue("Alert_Info", out var active);
            Application.Current!.Resources.TryGetValue("Button_Cancel", out var inactive);
            activeColor = active as Color ?? Colors.CornflowerBlue;
            inactiveColor = inactive as Color ?? Colors.Gray;

            BtnToday.BackgroundColor = _currentMarRange == MarRange.Today ? activeColor : inactiveColor;
            Btn3Days.BackgroundColor = _currentMarRange == MarRange.Last3Days ? activeColor : inactiveColor;
            Btn7Days.BackgroundColor = _currentMarRange == MarRange.Last7Days ? activeColor : inactiveColor;
        }

        private async void OnMarTodayClicked(object sender, EventArgs e)
        {
            _currentMarRange = MarRange.Today;
            UpdateMarFilterButtons();
            await LoadMarAsync();
        }

        private async void OnMar3DaysClicked(object sender, EventArgs e)
        {
            _currentMarRange = MarRange.Last3Days;
            UpdateMarFilterButtons();
            await LoadMarAsync();
        }

        private async void OnMar7DaysClicked(object sender, EventArgs e)
        {
            _currentMarRange = MarRange.Last7Days;
            UpdateMarFilterButtons();
            await LoadMarAsync();
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is not BindableObject bindable || bindable.BindingContext is not Medication med)
                return;

            var parameters = new Dictionary<string, object?>
            {
                ["Item"] = med,
                ["residentId"] = ResidentId,
                ["residentName"] = ResidentName,
                ["returnTo"] = ReturnTo
            };

            await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (sender is not BindableObject bindable || bindable.BindingContext is not Medication med)
                return;

            bool ok = await DisplayAlert(
                "Delete medication",
                $"Delete \"{med.MedName}\" for {ResidentName}?",
                "Delete", "Cancel");

            if (!ok)
                return;

            try
            {
                await _medicationService.DeleteAsync(med);
            }
            catch
            {
                // Offline — delete queued by wrapper
            }

            Medications.Remove(med);
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ReturnTo))
            {
                await Shell.Current.GoToAsync(ReturnTo);
                return;
            }

            if (ResidentId != Guid.Empty)
            {
                await Shell.Current.GoToAsync($"{nameof(ViewResidentPage)}?id={ResidentId}");
                return;
            }

            await Shell.Current.GoToAsync("..");
        }

        private async void OnAddMedicationClicked(object sender, EventArgs e)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["residentId"] = ResidentId,
                ["residentName"] = ResidentName,
                ["returnTo"] = ReturnTo
            };

            await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
        }

        private async void OnEditResidentClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"{nameof(EditResidentPage)}?id={ResidentId}");
        }

        private async void OnAiExplainClicked(object sender, EventArgs e)
        {
            if (sender is not BindableObject bindable || bindable.BindingContext is not Medication med)
                return;

            var ai = MauiProgram.Services.GetService<AiApiService>();
            if (ai == null)
            {
                await DisplayAlert("Unavailable", "AI service is not configured.", "OK");
                return;
            }

            try
            {
                var result = await ai.MedicationExplainAsync(med.MedName ?? "Unknown", med.Dosage);

                var message = result.Success
                    ? $"{result.Content}\n\n--- {result.Disclaimer} ---"
                    : result.Content;

                await DisplayAlert($"AI: {med.MedName}", message, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("AI Error", $"Could not get AI response: {ex.Message}", "OK");
            }
        }

    }
}
