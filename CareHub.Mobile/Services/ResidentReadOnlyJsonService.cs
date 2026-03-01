using System;
using System.Collections.Generic;
using System.Text;
using CareHub.Models;
using System.Net.Http.Json;

namespace CareHub.Mobile.Services;

public class ResidentReadOnlyJsonService
{
    private readonly HttpClient _http;

    public ResidentReadOnlyJsonService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Resident>> LoadAsync()
    {
        try
        {
            var items = await _http.GetFromJsonAsync<List<Resident>>("api/residents");
            return items ?? new List<Resident>();
        }
        catch
        {
            return new List<Resident>();
        }
    }
}
