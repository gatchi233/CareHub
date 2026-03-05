using CareHub.Desktop.Models;
using CareHub.Models;
using CareHub.Pages.Desktop;
using CareHub.Services.Abstractions;
using CareHub.Services.Local;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CareHub.ViewModels
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

        // Inventory alert properties (read-only dashboard)
        public ObservableCollection<InventoryAlertItem> LowStockTop3 { get; } = new();
        public ObservableCollection<InventoryAlertItem> ExpiringSoonTop3 { get; } = new();

        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            private set { if (_lowStockCount == value) return; _lowStockCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLowStock)); }
        }

        private int _expiringSoonCount;
        public int ExpiringSoonCount
        {
            get => _expiringSoonCount;
            private set { if (_expiringSoonCount == value) return; _expiringSoonCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasExpiringSoon)); }
        }

        public bool HasLowStock => LowStockCount > 0;
        public bool HasExpiringSoon => ExpiringSoonCount > 0;

        // MAR dashboard properties
        public ObservableCollection<MarDashboardItem> PendingMarTop { get; } = new();

        private int _pendingMarCount;
        public int PendingMarCount
        {
            get => _pendingMarCount;
            private set { if (_pendingMarCount == value) return; _pendingMarCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingMar)); }
        }

        public bool HasPendingMar => PendingMarCount > 0;

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
                var msg = CareHub.Desktop.Services.Sync.OfflineException.IsOffline(ex)
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

                if (!CareHub.Desktop.Services.Sync.ConnectivityHelper.IsOnline())
                    await ShowAlertAsync("Saved offline", "Saved offline (queued) — sync when online");

                return true;
            }
            catch (Exception ex)
            {
                var msg = CareHub.Desktop.Services.Sync.OfflineException.IsOffline(ex)
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

                if (!CareHub.Desktop.Services.Sync.ConnectivityHelper.IsOnline())
                    await ShowAlertAsync("Saved offline", "Saved offline (queued) — sync when online");
            }
            catch (Exception ex)
            {
                var msg = CareHub.Desktop.Services.Sync.OfflineException.IsOffline(ex)
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

        public void ComputeAlerts()
        {
            var now = DateTimeOffset.Now;

            // Build usable stock per med name from global inventory batches.
            // Usable stock = total stock − stock of expired/expiring-within-30-days batches.
            var inventoryGroups = _allMedications
                .Where(m => m.ResidentId == null || m.ResidentId == Guid.Empty)
                .GroupBy(m => m.MedName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var batches = g.ToList();
                    var totalStock = batches.Sum(b => b.StockQuantity);
                    var expiringStock = batches
                        .Where(b => b.IsExpired || b.DaysUntilExpiry <= 30)
                        .Sum(b => b.StockQuantity);
                    var usableStock = totalStock - expiringStock;
                    var reorderLevel = batches.Max(b => b.ReorderLevel);
                    var representative = batches.First();
                    return new { representative, usableStock, reorderLevel };
                })
                .ToList();

            var usableStockByName = inventoryGroups
                .ToDictionary(x => x.representative.MedName, x => x.usableStock, StringComparer.OrdinalIgnoreCase);

            // Low stock: one row per med name, usable stock <= reorder level
            var lowStock = inventoryGroups
                .Where(x => x.usableStock <= x.reorderLevel)
                .OrderBy(x => x.usableStock)
                .ToList();

            LowStockCount = lowStock.Count;
            LowStockTop3.Clear();
            foreach (var x in lowStock.Take(3))
            {
                LowStockTop3.Add(new InventoryAlertItem
                {
                    MedName = x.representative.MedName,
                    Detail = $"{x.usableStock} {x.representative.QuantityUnit} (reorder at {x.reorderLevel})",
                    ResidentName = x.representative.ResidentName
                });
            }

            // Expiry alert: expired OR expiring within 30 days (local time)
            // Same dedup: prefer global inventory entries over resident-assigned duplicates.
            var thirtyDaysFromNow = now.AddDays(30);
            var expiryAlertAll = _allMedications
                .Where(m =>
                {
                    var expiryLocal = m.ExpiryDate.ToLocalTime();
                    return expiryLocal.Date <= thirtyDaysFromNow.Date;
                })
                .ToList();

            var globalExpiryNames = new HashSet<string>(
                expiryAlertAll
                    .Where(m => m.ResidentId == null || m.ResidentId == Guid.Empty)
                    .Select(m => m.MedName),
                StringComparer.OrdinalIgnoreCase);

            var expiryAlerts = expiryAlertAll
                .Where(m =>
                    (m.ResidentId == null || m.ResidentId == Guid.Empty) ||
                    !globalExpiryNames.Contains(m.MedName))
                .OrderBy(m => m.ExpiryDate)
                .ToList();

            ExpiringSoonCount = expiryAlerts.Count;
            ExpiringSoonTop3.Clear();
            foreach (var m in expiryAlerts.Take(3))
            {
                var expiryLocal = m.ExpiryDate.ToLocalTime().Date;
                var daysLeft = (int)(expiryLocal - now.Date).TotalDays;
                var detail = daysLeft < 0
                    ? $"EXPIRED ({expiryLocal:dd MMM yyyy})"
                    : $"{expiryLocal:yyyy-MM-dd} ({daysLeft} day{(daysLeft == 1 ? "" : "s")} left)";
                ExpiringSoonTop3.Add(new InventoryAlertItem
                {
                    MedName = m.MedName,
                    Detail = detail,
                    ResidentName = m.ResidentName
                });
            }
        }

        public void ComputeMarDashboard(List<Medication> meds, List<MarEntry> marEntries)
        {
            var (_, _, fromLocal, toLocal) = MarScheduleHelper.GetTodayRange();

            // Only resident-assigned meds
            var residentMeds = meds.Where(m => m.ResidentId.HasValue).ToList();

            var slots = MarScheduleHelper.GenerateSlots(residentMeds, fromLocal, toLocal);
            var matchedIds = new HashSet<Guid>();
            MarScheduleHelper.OverlayMarEntries(slots, marEntries, matchedIds);

            // Group by resident, count pending + missed
            var groups = slots
                .GroupBy(s => s.ResidentId)
                .Select(g => new MarDashboardItem
                {
                    ResidentId = g.Key,
                    ResidentName = g.First().MedicationName, // placeholder, overwritten below
                    PendingCount = g.Count(s => s.Status == "Pending"),
                    MissedCount = g.Count(s => s.Status == "Missed")
                })
                .Where(d => d.PendingCount > 0 || d.MissedCount > 0)
                .OrderByDescending(d => d.MissedCount)
                .ThenByDescending(d => d.PendingCount)
                .ToList();

            // Resolve resident names from meds
            foreach (var item in groups)
            {
                var med = meds.FirstOrDefault(m => m.ResidentId == item.ResidentId);
                item.ResidentName = med?.ResidentName ?? "Unknown";
            }

            var totalPending = groups.Sum(g => g.PendingCount);
            var totalMissed = groups.Sum(g => g.MissedCount);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PendingMarCount = totalPending + totalMissed;
                PendingMarTop.Clear();
                foreach (var item in groups.Take(5))
                    PendingMarTop.Add(item);
            });
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

    public class InventoryAlertItem
    {
        public string MedName { get; set; } = "";
        public string Detail { get; set; } = "";
        public string? ResidentName { get; set; }
    }

    public class MarDashboardItem
    {
        public Guid ResidentId { get; set; }
        public string ResidentName { get; set; } = "";
        public int PendingCount { get; set; }
        public int MissedCount { get; set; }

        public string Summary
        {
            get
            {
                var parts = new List<string>();
                if (PendingCount > 0) parts.Add($"{PendingCount} pending");
                if (MissedCount > 0) parts.Add($"{MissedCount} missed");
                return string.Join(", ", parts);
            }
        }
    }
}
