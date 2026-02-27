using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedReminder.Models;
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

                // ✅ IMPORTANT: keep master list for filtering/sorting
                _allResidents = list?.ToList() ?? new List<Resident>();

                // ✅ Apply current filter to populate the UI list
                ApplyFilters();
            }
            catch (HttpRequestException)
            {
                await Shell.Current.DisplayAlert(
                    "Server not running",
                    "Cannot reach the API. Start MedReminder.Api and try again.",
                    "OK");

                // optional: keep current UI list as-is
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
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
                .OrderBy(r => DateTime.TryParse(r.DOB, out var d) ? d : DateTime.MinValue)
                .ThenBy(r => r.ResidentName);

            Residents.Clear();
            foreach (var r in query)
                Residents.Add(r);
        }
    }
}
