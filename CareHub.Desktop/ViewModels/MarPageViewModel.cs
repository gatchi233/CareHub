using CareHub.Desktop.Models;
using CareHub.Desktop.Services.Sync;
using CareHub.Models;
using CareHub.Services;
using CareHub.Services.Abstractions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CareHub.ViewModels;

public class MarPageViewModel : INotifyPropertyChanged
{
    private readonly IMarService _marService;
    private readonly IMedicationService _medicationService;
    private readonly IResidentService _residentService;

    private static readonly TimeSpan MatchTolerance = MarScheduleHelper.MatchTolerance;

    public bool IsOffline => !ConnectivityHelper.IsOnline();

    public enum MarRange
    {
        Today,
        Last3Days,
        Last7Days
    }

    private MarRange _currentRange = MarRange.Today;

    public bool IsRangeToday => _currentRange == MarRange.Today;
    public bool IsRange3Days => _currentRange == MarRange.Last3Days;
    public bool IsRange7Days => _currentRange == MarRange.Last7Days;

    public void SetRange(MarRange range)
    {
        _currentRange = range;
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(IsRangeToday));
        OnPropertyChanged(nameof(IsRange3Days));
        OnPropertyChanged(nameof(IsRange7Days));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public MarPageViewModel(IMarService marService, IMedicationService medicationService, IResidentService residentService)
    {
        _marService = marService;
        _medicationService = medicationService;
        _residentService = residentService;

        RefreshCommand = new Command(async () => await LoadAsync());
    }

    public Guid ResidentId { get; private set; } = Guid.Empty;
    public string ResidentName { get; private set; } = "";

    public ObservableCollection<ResidentMarGroup> ResidentGroups { get; } = new();

    private bool _hasLoadedOnce;
    public bool HasLoadedOnce
    {
        get => _hasLoadedOnce;
        private set { _hasLoadedOnce = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRefreshing));
            if (_isBusy)
                StatusMessage = "Loading...";
        }
    }

    public ICommand RefreshCommand { get; }

    public bool IsRefreshing => IsBusy;

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string Subtitle => _currentRange switch
    {
        MarRange.Today => "Today",
        MarRange.Last3Days => "Last 3 days",
        MarRange.Last7Days => "Last 7 days",
        _ => ""
    };

    public ICommand TodayCommand => new Command(async () =>
    {
        SetRange(MarRange.Today);
        await LoadAsync();
    });

    public ICommand Last3Command => new Command(async () =>
    {
        SetRange(MarRange.Last3Days);
        await LoadAsync();
    });

    public ICommand Last7Command => new Command(async () =>
    {
        SetRange(MarRange.Last7Days);
        await LoadAsync();
    });

    public void SetResident(Guid residentId, string residentName)
    {
        ResidentId = residentId;
        ResidentName = residentName ?? "";
        OnPropertyChanged(nameof(ResidentName));
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ResidentGroups.Clear();

            var (fromUtc, toUtc, fromLocal, toLocal) = GetDateRange(_currentRange);

            // Load medications and residents
            var allMeds = await _medicationService.LoadAsync();
            var allResidents = await _residentService.LoadAsync();

            // Filter medications by resident if specified
            Guid? resFilter = ResidentId == Guid.Empty ? null : ResidentId;
            var meds = resFilter.HasValue
                ? allMeds.Where(m => m.ResidentId == resFilter.Value).ToList()
                : allMeds.Where(m => m.ResidentId.HasValue).ToList();

            // Load MAR entries for the date range
            var marEntries = await _marService.LoadAsync(resFilter, fromUtc, toUtc);

            // Generate schedule slots
            var allSlots = MarScheduleHelper.GenerateSlots(meds, fromLocal, toLocal);

            // Overlay MAR entries onto slots
            var matchedEntryIds = new HashSet<Guid>();
            MarScheduleHelper.OverlayMarEntries(allSlots, marEntries, matchedEntryIds);

            // Find unscheduled entries (MAR entries that didn't match any slot)
            var unscheduledEntries = marEntries
                .Where(e => !matchedEntryIds.Contains(e.Id) && !e.IsVoided)
                .ToList();

            // Group by resident
            var residentIds = allSlots.Select(s => s.ResidentId)
                .Union(unscheduledEntries.Select(e => e.ResidentId))
                .Distinct();

            foreach (var rid in residentIds)
            {
                var resident = allResidents.FirstOrDefault(r => r.Id == rid);
                var group = new ResidentMarGroup
                {
                    ResidentId = rid,
                    ResidentName = resident?.ResidentName ?? "Unknown Resident",
                    RoomNumber = resident?.RoomNumber,
                    ScheduledSlots = allSlots
                        .Where(s => s.ResidentId == rid)
                        .OrderBy(s => s.ScheduledForUtc)
                        .ToList(),
                    UnscheduledEntries = unscheduledEntries
                        .Where(e => e.ResidentId == rid)
                        .Select(e => new MarSlotViewModel
                        {
                            ResidentId = e.ResidentId,
                            MedicationId = e.MedicationId,
                            MedicationName = e.MedicationName,
                            DoseQuantity = e.DoseQuantity,
                            DoseUnit = e.DoseUnit,
                            Status = e.Status,
                            IsUnscheduled = true,
                            LastAdministeredLocal = e.AdministeredAtUtc.ToLocalTime().ToString("HH:mm"),
                            RecordedBy = e.RecordedBy,
                            NotesPreview = e.Notes
                        })
                        .OrderByDescending(s => s.LastAdministeredLocal)
                        .ToList()
                };

                ResidentGroups.Add(group);
            }

            var totalSlots = ResidentGroups.Sum(g => g.ScheduledSlots.Count);
            var totalUnscheduled = ResidentGroups.Sum(g => g.UnscheduledEntries.Count);
            StatusMessage = totalUnscheduled > 0
                ? $"{totalSlots} scheduled slots, {totalUnscheduled} unscheduled"
                : $"{totalSlots} scheduled slots";

            HasLoadedOnce = true;
        }
        catch (OfflineException)
        {
            StatusMessage = "Offline mode (API unreachable)";
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Load Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
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
}
