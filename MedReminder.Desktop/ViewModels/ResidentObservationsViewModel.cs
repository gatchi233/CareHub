using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services;
using MedReminder.Services.Abstractions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MedReminder.ViewModels;

public class ResidentObservationsViewModel : INotifyPropertyChanged
{
    private readonly IObservationService _observations;
    private bool _hasLoadedOnce;
    public bool IsOffline => !ConnectivityHelper.IsOnline();
    public enum ObservationRange
    {
        Today,
        Last3Days,
        Last7Days
    }

    private ObservationRange _currentRange = ObservationRange.Today;

    public bool IsRangeToday  => _currentRange == ObservationRange.Today;
    public bool IsRange3Days  => _currentRange == ObservationRange.Last3Days;
    public bool IsRange7Days  => _currentRange == ObservationRange.Last7Days;

    public void SetRange(ObservationRange range)
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

    public ResidentObservationsViewModel(IObservationService observations)
    {
        _observations = observations;

        RefreshCommand = new Command(async () => await LoadAsync());
        AddCommand = new Command(async () => await AddAsync());
        EditCommand = new Command<Observation>(async o => await EditAsync(o));
        DeleteCommand = new Command<Observation>(async o => await DeleteAsync(o));
    }

    public Guid ResidentId { get; private set; } = Guid.Empty;
    public string ResidentName { get; private set; } = "";

    public ObservableCollection<Observation> Items { get; } = new();
    public ObservableCollection<Observation> Observations => Items;

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
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            if (_isBusy)
                StatusMessage = "Loading...";
        }
    }

    private Observation? _selectedObservation;
    public Observation? SelectedObservation
    {
        get => _selectedObservation;
        set
        {
            if (_selectedObservation == value) return;
            _selectedObservation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEdit));
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public ICommand AddObservationCommand => AddCommand;
    public bool IsRefreshing => IsBusy;
    public bool CanAdd => !IsBusy && ResidentId != Guid.Empty;
    public bool CanEdit => SelectedObservation != null && !IsBusy;
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
        ObservationRange.Today => "Today",
        ObservationRange.Last3Days => "Last 3 days",
        ObservationRange.Last7Days => "Last 7 days",
        _ => ""
    };

    public ICommand TodayCommand => new Command(async () =>
    {
        SetRange(ObservationRange.Today);
        await LoadAsync();
    });

    public ICommand Last3Command => new Command(async () =>
    {
        SetRange(ObservationRange.Last3Days);
        await LoadAsync();
    });

    public ICommand Last7Command => new Command(async () =>
    {
        SetRange(ObservationRange.Last7Days);
        await LoadAsync();
    });

    public void SetResident(Guid residentId, string residentName)
    {
        ResidentId = residentId;
        ResidentName = residentName ?? "";
        OnPropertyChanged(nameof(ResidentName));
        OnPropertyChanged(nameof(CanAdd));
    }

    public async Task InitializeAsync()
    {
        if (HasLoadedOnce) return;
        await LoadAsync();
        HasLoadedOnce = true;
    }

    public async Task LoadAsync()
    {
        if (IsBusy || ResidentId == Guid.Empty)
            return;

        try
        {
            IsBusy = true;
            Items.Clear();

            var list = await _observations.GetByResidentIdAsync(ResidentId);

            var filtered = ApplyDateFilter(list, _currentRange);

            foreach (var item in filtered.OrderByDescending(x => x.RecordedAt))
                Items.Add(item);

            StatusMessage = $"{Items.Count} observations";
            SelectedObservation = null;
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

    private string GetRecordedBy()
    {
        var auth = MauiProgram.Services.GetService<AuthService>();
        return auth?.CurrentUser?.StaffName ?? "Unknown";
    }

    // Called from the page after ADD popup returns
    public async Task AddObservationFromPopupAsync(
        string? temperature, string? bpHigh, string? bpLow,
        string? pulse, string? spo2, string? notes)
    {
        if (ResidentId == Guid.Empty || IsBusy)
            return;

        try
        {
            IsBusy = true;

            var obs = new Observation
            {
                Id = Guid.Empty,
                ResidentId = ResidentId,
                RecordedBy = GetRecordedBy()
            };

            obs.SetVitals(new VitalsData
            {
                Temp = string.IsNullOrWhiteSpace(temperature) ? null : temperature,
                BpHigh = string.IsNullOrWhiteSpace(bpHigh) ? null : bpHigh,
                BpLow = string.IsNullOrWhiteSpace(bpLow) ? null : bpLow,
                Pulse = string.IsNullOrWhiteSpace(pulse) ? null : pulse,
                Spo2 = string.IsNullOrWhiteSpace(spo2) ? null : spo2,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
            });

            await _observations.UpsertAsync(obs);
        }
        catch (OfflineException)
        {
            StatusMessage = "Saved offline (queued) - sync when online";
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Add Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    // Called from the page after EDIT popup returns
    public async Task EditObservationFromPopupAsync(Observation item,
        string? temperature, string? bpHigh, string? bpLow,
        string? pulse, string? spo2, string? notes)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            item.SetVitals(new VitalsData
            {
                Temp = string.IsNullOrWhiteSpace(temperature) ? null : temperature,
                BpHigh = string.IsNullOrWhiteSpace(bpHigh) ? null : bpHigh,
                BpLow = string.IsNullOrWhiteSpace(bpLow) ? null : bpLow,
                Pulse = string.IsNullOrWhiteSpace(pulse) ? null : pulse,
                Spo2 = string.IsNullOrWhiteSpace(spo2) ? null : spo2,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
            });

            await _observations.UpsertAsync(item);
        }
        catch (OfflineException)
        {
            StatusMessage = "Edit saved offline (queued) - sync when online";
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Edit Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    private Task AddAsync()
    {
        _addRequested?.Invoke();
        return Task.CompletedTask;
    }

    private Task EditAsync(Observation? item)
    {
        if (item is null) return Task.CompletedTask;
        _editRequested?.Invoke(item);
        return Task.CompletedTask;
    }

    // The page sets these actions to trigger popups
    private Action? _addRequested;
    public Action? AddRequested
    {
        get => _addRequested;
        set => _addRequested = value;
    }

    private Action<Observation>? _editRequested;
    public Action<Observation>? EditRequested
    {
        get => _editRequested;
        set => _editRequested = value;
    }

    private async Task DeleteAsync(Observation? item)
    {
        if (item is null) return;

        var ok = await Shell.Current.DisplayAlert("Delete", "Delete this observation?", "Delete", "Cancel");
        if (!ok) return;

        try
        {
            await _observations.DeleteAsync(item);
        }
        catch (OfflineException)
        {
            StatusMessage = "Delete queued offline - sync when online";
        }
        await LoadAsync();
    }

    private IEnumerable<Observation> ApplyDateFilter(
    IEnumerable<Observation> source,
    ObservationRange range)
    {
        var todayLocal = DateTime.Now.Date;

        var fromLocal = range switch
        {
            ObservationRange.Today => todayLocal,
            ObservationRange.Last3Days => todayLocal.AddDays(-2),
            ObservationRange.Last7Days => todayLocal.AddDays(-6),
            _ => todayLocal
        };

        var fromUtc = fromLocal.ToUniversalTime();

        return source
            .Where(o => o.RecordedAt >= fromUtc)
            .OrderByDescending(o => o.RecordedAt);
    }
}
