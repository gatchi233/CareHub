using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CareHub.Models;

namespace CareHub.Mobile.Services;

public class ResidentReadOnlyJsonService
{
    public async Task<List<Resident>> LoadAsync()
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync("Residents.json");
        var items = await JsonSerializer.DeserializeAsync<List<Resident>>(stream);
        return items ?? new List<Resident>();
    }
}
