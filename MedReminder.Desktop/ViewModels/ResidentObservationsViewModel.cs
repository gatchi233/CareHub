using System.Collections.ObjectModel;
using System.Windows.Input;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels;

public class ResidentObservationsViewModel : BaseViewModel
{
    private readonly IObservationService _observations;

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

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public void SetResident(Guid residentId, string residentName)
    {
        ResidentId = residentId;
        ResidentName = residentName ?? "";
        OnPropertyChanged(nameof(ResidentName));
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

            // Sort newest first (ObservedAt is your MAUI model field)
            foreach (var item in list.OrderByDescending(x => x.ObservedAt))
                Items.Add(item);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddAsync()
    {
        if (ResidentId == Guid.Empty)
            return;

        // Minimal: prompt for Type + Value + RecordedBy
        var type = await Shell.Current.DisplayPromptAsync("New Observation", "Type (e.g., Note / BP / Temp):", "OK", "Cancel", "Note");
        if (string.IsNullOrWhiteSpace(type)) return;

        var value = await Shell.Current.DisplayPromptAsync("New Observation", "Value:", "OK", "Cancel", "Example");
        if (string.IsNullOrWhiteSpace(value)) return;

        var by = await Shell.Current.DisplayPromptAsync("New Observation", "Recorded by:", "OK", "Cancel", "Staff");
        if (string.IsNullOrWhiteSpace(by)) by = "Staff";

        var obs = new Observation
        {
            Id = Guid.Empty,
            ResidentId = ResidentId,
            ObservedAt = DateTime.Now,
            ObservedBy = by,
            Notes = $"{type}: {value}"
        };

        await _observations.UpsertAsync(obs);
        await LoadAsync();
    }

    private async Task EditAsync(Observation? item)
    {
        if (item is null) return;

        // Simple edit: edit Notes
        var notes = await Shell.Current.DisplayPromptAsync("Edit Observation", "Notes:", "Save", "Cancel", item.Notes);
        if (notes is null) return;

        item.Notes = notes;
        await _observations.UpsertAsync(item);
        await LoadAsync();
    }

    private async Task DeleteAsync(Observation? item)
    {
        if (item is null) return;

        var ok = await Shell.Current.DisplayAlert("Delete", "Delete this observation?", "Delete", "Cancel");
        if (!ok) return;

        await _observations.DeleteAsync(item);
        await LoadAsync();
    }
}