using System.Net;
using System.Net.Http.Json;
using MedReminder.Desktop.Services.Sync;
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
        try
        {
            var items = await _http.GetFromJsonAsync<List<Observation>>("api/Observations");
            return items ?? new List<Observation>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Load Observations).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Load Observations).", ex);
        }
    }

    public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
    {
        try
        {
            var items = await _http.GetFromJsonAsync<List<Observation>>(
                $"api/Observations/by-resident/{residentId}");
            return items ?? new List<Observation>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Get Observations by resident).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Get Observations by resident).", ex);
        }
    }

    public Task AddAsync(Observation observation)
    {
        if (observation is null)
            throw new ArgumentNullException(nameof(observation));

        return UpsertAsync(observation);
    }

    // Api service itself doesn't "sync" (wrapper does)
    public Task<int> SyncAsync() => Task.FromResult(0);

    public Task ReplaceAllAsync(List<Observation> items) => Task.CompletedTask;

    public async Task UpsertAsync(Observation item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        try
        {
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
        catch (HttpRequestException ex)
        {
            // This is exactly what happens when you stop the API for demo step #5.
            throw new OfflineException("API unreachable (Upsert Observation). Queue this operation.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Upsert Observation). Queue this operation.", ex);
        }
    }

    public async Task DeleteAsync(Observation item)
    {
        if (item is null || item.Id == Guid.Empty)
            return;

        try
        {
            var resp = await _http.DeleteAsync($"api/Observations/{item.Id}");

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return;

            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Delete Observation). Queue this operation.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Delete Observation). Queue this operation.", ex);
        }
    }
}