using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CareHub.Models;

namespace CareHub.Mobile.Services;

public class MedicationReadOnlyJsonService
{
    public async Task<List<Medication>> LoadAsync()
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync("Medications.json");
        var items = await JsonSerializer.DeserializeAsync<List<Medication>>(stream);
        return items ?? new List<Medication>();
    }
}
