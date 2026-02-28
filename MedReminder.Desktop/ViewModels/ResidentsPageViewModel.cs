using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels
{
    public class ResidentsPageViewModel : INotifyPropertyChanged
    {
        private readonly IResidentService _residentService;

        private List<Resident> _allResidents = new();

        public string NameFilter { get; private set; } = string.Empty;

        public ObservableCollection<Resident> Residents { get; } = new();

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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ResidentsPageViewModel(IResidentService residentService)
        {
            _residentService = residentService;
        }

        public async Task LoadResidentsAsync()
        {
            try
            {
                var list = await _residentService.LoadAsync();

                _allResidents = list?.ToList() ?? new List<Resident>();

                ApplyFilters();
                StatusMessage = "";
            }
            catch (Exception)
            {
                StatusMessage = "Offline — showing cached data";
                // keep current UI list as-is
            }
        }

        public async Task DeleteResidentAsync(Resident resident)
        {
            if (resident == null)
                return;

            try
            {
                await _residentService.DeleteAsync(resident);

                _allResidents = _allResidents
                    .Where(r => r.Id != resident.Id)
                    .ToList();

                ApplyFilters();

                if (!ConnectivityHelper.IsOnline())
                    StatusMessage = "Saved offline (queued) — sync when online";
            }
            catch (Exception)
            {
                StatusMessage = "Saved offline (queued) — sync when online";

                _allResidents = _allResidents
                    .Where(r => r.Id != resident.Id)
                    .ToList();

                ApplyFilters();
            }
        }

        public void UpdateFilters(string nameFilter)
        {
            NameFilter = nameFilter?.Trim() ?? string.Empty;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<Resident> query = _allResidents;

            if (!string.IsNullOrWhiteSpace(NameFilter))
            {
                var q = NameFilter.Trim();

                query = query.Where(r =>
                {
                    var first = (r.ResidentFName ?? "").Trim();
                    var last = (r.ResidentLName ?? "").Trim();

                    var full1 = $"{first} {last}".Trim();
                    var full2 = $"{last} {first}".Trim();

                    return full1.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                        || full2.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                        || first.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                        || last.Contains(q, System.StringComparison.OrdinalIgnoreCase);
                });
            }

            query = query
                .OrderBy(r => DateTime.TryParse(r.DateOfBirth, out var d) ? d : DateTime.MinValue)
                .ThenBy(r => r.ResidentName);

            Residents.Clear();
            foreach (var r in query)
                Residents.Add(r);
        }
    }
}
