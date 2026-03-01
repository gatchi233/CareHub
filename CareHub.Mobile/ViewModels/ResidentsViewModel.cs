using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using CareHub.Models;
using CareHub.Mobile.Services;

namespace CareHub.Mobile.ViewModels;

public class ResidentsViewModel
{
    private readonly ResidentReadOnlyJsonService _residentService;

    public ObservableCollection<Resident> Residents { get; } = new();

    public ResidentsViewModel(ResidentReadOnlyJsonService residentService)
    {
        _residentService = residentService;
    }

    public async Task LoadAsync()
    {
        Residents.Clear();
        var items = await _residentService.LoadAsync();
        foreach (var r in items)
            Residents.Add(r);
    }
}
