using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels
{
    public class HomePageViewModel : INotifyPropertyChanged
    {
        private readonly IMedicationService _medicationService;
        private readonly IResidentService _residentService;

        private List<Medication> _allMedications = new();

        public ObservableCollection<Medication> Medications { get; } = new();
        public ObservableCollection<Resident> Residents { get; } = new();

        private Resident? _selectedResident;
        public Resident? SelectedResident
        {
            get => _selectedResident;
            set
            {
                if (SetProperty(ref _selectedResident, value))
                {
                    ApplyFilters();
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public HomePageViewModel(IMedicationService medicationService, IResidentService residentService)
        {
            _medicationService = medicationService;
            _residentService = residentService;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public async Task LoadResidentsAsync()
        {
            Residents.Clear();

            // "All Residents" special item (ResidentName is computed from ResidentFName + ResidentLName)
            Residents.Add(new Resident { Id = Guid.Empty, ResidentFName = "All", ResidentLName = "residents" });

            var items = await _residentService.LoadAsync();
            foreach (var r in items)
                Residents.Add(r);

            if (SelectedResident == null)
                SelectedResident = Residents.FirstOrDefault();
        }

        public async Task LoadMedicationsAsync()
        {
            if (Residents.Count == 0)
                await LoadResidentsAsync();

            if (IsBusy) return;
            IsBusy = true;

            try
            {
                _allMedications = (await _medicationService.LoadAsync()).ToList();

                var residentLookup = Residents.ToDictionary(r => r.Id);

                foreach (var m in _allMedications)
                {
                    if (m.ResidentId.HasValue &&
                        residentLookup.TryGetValue(m.ResidentId.Value, out var r))
                    {
                        m.ResidentName = r.ResidentName;
                    }
                    else
                    {
                        m.ResidentName = null;
                    }
                }

                ApplyFilters();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            IEnumerable<Medication> filtered = _allMedications;

            if (SelectedResident != null && SelectedResident.Id != Guid.Empty)
            {
                filtered = filtered.Where(m => m.ResidentId == SelectedResident.Id);
            }

            Medications.Clear();
            foreach (var m in filtered)
                Medications.Add(m);
        }

    }
}
