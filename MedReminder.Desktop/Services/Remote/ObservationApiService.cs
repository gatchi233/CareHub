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
        var items = await _http.GetFromJsonAsync<List<Observation>>("api/Observations");
        return items ?? new List<Observation>();
    }

    public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
    {
        var items = await _http.GetFromJsonAsync<List<Observation>>(
            $"api/Observations/by-resident/{residentId}");
        return items ?? new List<Observation>();
    }

    public Task AddAsync(Observation observation)
    {
        if (observation is null)
            throw new ArgumentNullException(nameof(observation));

        return UpsertAsync(observation);
    }

    // Api service itself doesn't "sync" (wrapper does)
    public Task<int> SyncAsync() => Task.FromResult(0);

    public async Task UpsertAsync(Observation item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        // Create
        if (item.Id == Guid.Empty)
        {
            var resp = await _http.PostAsJsonAsync("api/Observations", item);
            resp.EnsureSuccessStatusCode();

            var created = await resp.Content.ReadFromJsonAsync<Observation>();
            if (created is not null)
            {
                item.Id = created.Id;
                item.RecordedAt = created.RecordedAt;
            }

            return;
        }

        // Update
        var putResp = await _http.PutAsJsonAsync($"api/Observations/{item.Id}", item);
        putResp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(Observation item)
    {
        if (item is null || item.Id == Guid.Empty)
            return;

        var resp = await _http.DeleteAsync($"api/Observations/{item.Id}");

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return;

        resp.EnsureSuccessStatusCode();
    }
}