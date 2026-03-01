using System;
using System.Collections.Generic;
using System.Text;
using CareHub.Models;
using System.Net.Http.Json;

namespace CareHub.Mobile.Services;

public class MedicationReadOnlyJsonService
{
    private readonly HttpClient _http;

    public MedicationReadOnlyJsonService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Medication>> LoadAsync()
    {
        try
        {
            var items = await _http.GetFromJsonAsync<List<Medication>>("api/medications");
            return items ?? new List<Medication>();
        }
        catch
        {
            return new List<Medication>();
        }
    }
}
