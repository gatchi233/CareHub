using System.Net;
using System.Net.Http.Json;
using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Remote;

public class ResidentApiService : IResidentService
{
    private readonly HttpClient _http;

    public ResidentApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Resident>> LoadAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<Resident>>("api/residents")
                   ?? new List<Resident>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Load Residents).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Load Residents).", ex);
        }
    }

    public async Task UpsertAsync(Resident item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        try
        {
            if (item.Id == Guid.Empty)
            {
                var resp = await _http.PostAsJsonAsync("api/residents", item);
                resp.EnsureSuccessStatusCode();

                var created = await resp.Content.ReadFromJsonAsync<Resident>();
                if (created != null && created.Id != Guid.Empty)
                    item.Id = created.Id;

                return;
            }

            var put = await _http.PutAsJsonAsync($"api/residents/{item.Id}", item);
            put.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Upsert Resident).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Upsert Resident).", ex);
        }
    }

    public Task ReplaceAllAsync(List<Resident> items) => Task.CompletedTask;

    public async Task DeleteAsync(Resident item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (item.Id == Guid.Empty) return;

        try
        {
            var resp = await _http.DeleteAsync($"api/residents/{item.Id}");

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return;

            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Delete Resident).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Delete Resident).", ex);
        }
    }
}
