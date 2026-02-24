using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedReminder.Models;
using MedReminder.Services.Local;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels
{

    public class ResidentsPageViewModel
    {
        private readonly IResidentService _residentService;

        private List<Resident> _allResidents = new();

        public string NameFilter { get; private set; } = string.Empty;

        public ObservableCollection<Resident> Residents { get; } = new();

        public ResidentsPageViewModel(IResidentService residentService)
        {
            _residentService = residentService;
        }

        public async Task LoadResidentsAsync()
        {
            try
            {
                var list = await _residentService.LoadAsync();
                Residents.Clear();
                foreach (var r in list) Residents.Add(r);
            }
            catch (HttpRequestException)
            {
                await Shell.Current.DisplayAlert(
                    "Server not running",
                    "Cannot reach the API. Start MedReminder.Api and try again.",
                    "OK");
            }
        }

        public async Task DeleteResidentAsync(Resident resident)
        {
            if (resident == null)
                return;

            await _residentService.DeleteAsync(resident);

            _allResidents = _allResidents
                .Where(r => r.Id != resident.Id)
                .ToList();

            ApplyFilters();
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
                    var first = (r.FirstName ?? "").Trim();
                    var last = (r.LastName ?? "").Trim();

                    var full1 = $"{first} {last}".Trim();
                    var full2 = $"{last} {first}".Trim();

                    return full1.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                        || full2.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                        || first.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                        || last.Contains(q, System.StringComparison.OrdinalIgnoreCase);
                });
            }

            query = query
                .OrderBy(r => DateTime.TryParse(r.DOB, out var d) ? d : DateTime.MinValue)
                .ThenBy(r => r.FullName);

            Residents.Clear();
            foreach (var r in query)
            {
                Residents.Add(r);
            }
        }
    }
}
