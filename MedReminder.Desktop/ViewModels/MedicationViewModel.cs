using MedReminder.Models;
using MedReminder.Pages.Desktop;
using MedReminder.Services.Abstractions;
using MedReminder.Services.Local;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedReminder.ViewModels
{
    public class MedicationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly IMedicationService _service;
        private bool _isBusy;
        private string _searchText = string.Empty;
        private IDispatcherTimer? _dailyTimer;
        private bool _timerStarted;

        private List<Medication> _allMedications = new();

        public ObservableCollection<Medication> Medications { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }

        public MedicationViewModel(IMedicationService service)
        {
            _service = service;

            RefreshCommand = new Command(async () => await LoadAsync());
            AddCommand = new Command(async () => await GoToEditAsync(null));
            SaveCommand = new Command<Medication>(async m => await SaveAsync(m));
            DeleteCommand = new Command<Medication>(async m => await DeleteAsync(m));
            EditCommand = new Command<Medication>(async m => await GoToEditAsync(m));
        }

        public async Task InitializeAsync()
        {
            await LoadAsync();

            if (!_timerStarted)
            {
                StartDailyReminderTimer();
                _timerStarted = true;
            }
        }

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var items = await _service.LoadAsync();
                _allMedications = items.OrderBy(m => m.ExpiryDate).ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                var msg = MedReminder.Desktop.Services.Sync.OfflineException.IsOffline(ex)
                    ? "Offline — showing cached data"
                    : ex.Message;
                await ShowAlertAsync("Load", msg);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Saves the medication data without navigating away.
        /// Returns true if save succeeded, false otherwise.
        /// </summary>
        public async Task<bool> SaveDataAsync(Medication? med)
        {
            try
            {
                if (med is null) return false;

                if (string.IsNullOrWhiteSpace(med.MedName))
                {
                    await ShowAlertAsync("Validation", "Name is required.");
                    return false;
                }

                if (med.ExpiryDate.Date < DateTime.Today)
                {
                    await ShowAlertAsync("Validation", "Expiry date cannot be in the past.");
                    return false;
                }

                await _service.UpsertAsync(med);

                _allMedications = (await _service.LoadAsync())
                    .OrderBy(m => m.ExpiryDate)
                    .ToList();

                ApplyFilter();

                if (!MedReminder.Desktop.Services.Sync.ConnectivityHelper.IsOnline())
                    await ShowAlertAsync("Saved offline", "Saved offline (queued) — sync when online");

                return true;
            }
            catch (Exception ex)
            {
                var msg = MedReminder.Desktop.Services.Sync.OfflineException.IsOffline(ex)
                    ? "Saved offline (queued) — sync when online"
                    : ex.Message;
                await ShowAlertAsync("Save", msg);
                return false;
            }
        }

        public async Task SaveAsync(Medication? med)
        {
            if (await SaveDataAsync(med))
                await Shell.Current.GoToAsync("..");
        }

        public async Task DeleteAsync(Medication? med)
        {
            try
            {
                if (med is null) return;

                await _service.DeleteAsync(med);
                _allMedications = (await _service.LoadAsync()).ToList();
                ApplyFilter();

                if (!MedReminder.Desktop.Services.Sync.ConnectivityHelper.IsOnline())
                    await ShowAlertAsync("Saved offline", "Saved offline (queued) — sync when online");
            }
            catch (Exception ex)
            {
                var msg = MedReminder.Desktop.Services.Sync.OfflineException.IsOffline(ex)
                    ? "Saved offline (queued) — sync when online"
                    : ex.Message;
                await ShowAlertAsync("Delete", msg);
            }
        }

        public async Task GoToEditAsync(Medication? med)
        {
            try
            {
                var parameters = new Dictionary<string, object?>
                {
                    ["Item"] = med  // Global inventory meds
                };

                await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Navigation Error", ex.Message);
            }
        }

        private void ApplyFilter()
        {
            var query = (_searchText ?? string.Empty).Trim();
            IEnumerable<Medication> filtered = _allMedications;

            if (!string.IsNullOrEmpty(query))
            {
                var q = query.ToLowerInvariant();
                filtered = _allMedications.Where(m =>
                    (m.MedName ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (m.Dosage ?? string.Empty).ToLowerInvariant().Contains(q));
            }

            filtered = filtered.OrderBy(m => m.ReminderTime);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Medications.Clear();
                foreach (var m in filtered)
                    Medications.Add(m);
            });
        }

        private void StartDailyReminderTimer()
        {
            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher is null) return;

                _dailyTimer = dispatcher.CreateTimer();
                _dailyTimer.Interval = TimeSpan.FromMinutes(1);
                _dailyTimer.Tick += async (_, __) =>
                {
                    try { await CheckRemindersAsync(); }
                    catch { /* avoid crash from timer thread */ }
                };
                _dailyTimer.Start();
            }
            catch
            {
                // ignore timer setup failures
            }
        }

        private async Task CheckRemindersAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var dayOfWeek = now.DayOfWeek;

            foreach (var m in _allMedications)
            {
                if (today > m.ExpiryDate.Date) continue;

                bool anyDaySet =
                    m.ReminderMon || m.ReminderTue || m.ReminderWed ||
                    m.ReminderThu || m.ReminderFri || m.ReminderSat || m.ReminderSun;

                bool dayEnabled = dayOfWeek switch
                {
                    DayOfWeek.Monday => m.ReminderMon,
                    DayOfWeek.Tuesday => m.ReminderTue,
                    DayOfWeek.Wednesday => m.ReminderWed,
                    DayOfWeek.Thursday => m.ReminderThu,
                    DayOfWeek.Friday => m.ReminderFri,
                    DayOfWeek.Saturday => m.ReminderSat,
                    DayOfWeek.Sunday => m.ReminderSun,
                    _ => true
                };

                if (anyDaySet && !dayEnabled)
                    continue;

                var reminderTime = m.ReminderTime;

                bool shouldNotify =
                    now.Hour == reminderTime.Hours &&
                    now.Minute == reminderTime.Minutes;

                if (shouldNotify)
                {
                    PlayReminderSound();
                    await ShowAlertAsync("Reminder", $"Take {m.MedName} ({m.Dosage}).");
                }
            }
        }

        private static void PlayReminderSound()
        {
        #if WINDOWS
            try
            {
                Console.Beep();
            }
            catch
            {
                // Ignore audio errors so reminder still works
            }
        #endif
        }

        private static Page? TryGetPage()
            => Application.Current?.Windows.FirstOrDefault()?.Page;

        private static async Task ShowAlertAsync(string title, string message)
        {
            var page = TryGetPage();
            if (page is not null)
                await page.DisplayAlert(title, message, "OK");
        }
    }
}
