using System.Net;
using System.Net.Http.Json;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Remote;

public sealed class ObservationApiService : IObservationService
{
    private readonly HttpClient _http;

    public ObservationApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Observation>> LoadAsync()
    {
        var items = await _http.GetFromJsonAsync<List<Observation>>("api/observations");
        return items ?? new List<Observation>();
    }

    public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
    {
        var items = await _http.GetFromJsonAsync<List<Observation>>(
            $"api/observations/byResident/{residentId}");
        return items ?? new List<Observation>();
    }

    public async Task UpsertAsync(Observation item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        // Create
        if (item.Id == Guid.Empty)
        {
            var resp = await _http.PostAsJsonAsync("api/observations", item);
            resp.EnsureSuccessStatusCode();

            // If API returns the created entity, update local object
            var created = await resp.Content.ReadFromJsonAsync<Observation>();
            if (created is not null)
            {
                item.Id = created.Id;
                item.ObservedAt = created.ObservedAt;
            }

            return;
        }

        // Update
        var putResp = await _http.PutAsJsonAsync($"api/observations/{item.Id}", item);
        putResp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(Observation item)
    {
        if (item is null || item.Id == Guid.Empty)
            return;

        var resp = await _http.DeleteAsync($"api/observations/{item.Id}");

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return;

        resp.EnsureSuccessStatusCode();
    }
}