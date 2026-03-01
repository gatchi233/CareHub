using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using CareHub.Models;
using CareHub.Mobile.Services;

namespace CareHub.Mobile.ViewModels;

public class ResidentDetailViewModel
{
    private readonly MedicationReadOnlyJsonService _medService;

    public Resident? Resident { get; private set; }

    public ObservableCollection<Medication> Medications { get; } = new();

    public ResidentDetailViewModel(MedicationReadOnlyJsonService medService)
    {
        _medService = medService;
    }

    public async Task LoadAsync(Resident resident)
    {
        Resident = resident;

        Medications.Clear();
        var allMeds = await _medService.LoadAsync();

        // Adjust this filter if your Medication model uses a different field name.
        // Common patterns: Medication.ResidentId (int) or Medication.ResidentName (string)
        foreach (var m in allMeds.Where(m => m.ResidentId == resident.Id))
            Medications.Add(m);
    }
}
