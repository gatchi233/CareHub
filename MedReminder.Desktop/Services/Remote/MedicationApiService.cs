using System.Net;
using System.Net.Http.Json;
using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Remote;

public class MedicationApiService : IMedicationService
{
    private readonly HttpClient _http;

    public MedicationApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Medication>> LoadAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<Medication>>("api/medications")
                   ?? new List<Medication>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Load Medications).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Load Medications).", ex);
        }
    }

    public async Task UpsertAsync(Medication item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        // Normalize empty Guid to null to avoid FK violations in API DB.
        if (item.ResidentId.HasValue && item.ResidentId.Value == Guid.Empty)
            item.ResidentId = null;

        try
        {
            if (item.Id == Guid.Empty)
            {
                var resp = await _http.PostAsJsonAsync("api/medications", item);
                resp.EnsureSuccessStatusCode();

                var created = await resp.Content.ReadFromJsonAsync<Medication>();
                if (created != null && created.Id != Guid.Empty)
                    item.Id = created.Id;

                return;
            }

            var put = await _http.PutAsJsonAsync($"api/medications/{item.Id}", item);
            put.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Upsert Medication).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Upsert Medication).", ex);
        }
    }

    public Task ReplaceAllAsync(List<Medication> items) => Task.CompletedTask;

    public async Task DeleteAsync(Medication item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (item.Id == Guid.Empty) return;

        try
        {
            var resp = await _http.DeleteAsync($"api/medications/{item.Id}");

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return;

            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Delete Medication).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Delete Medication).", ex);
        }
    }

    public async Task<List<Medication>> GetLowStockAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<Medication>>("api/medications/lowstock")
                   ?? new List<Medication>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (GetLowStock).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (GetLowStock).", ex);
        }
    }

    public async Task AdjustStockAsync(Guid medicationId, int delta)
    {
        try
        {
            var resp = await _http.PostAsync($"api/medications/{medicationId}/adjustStock?delta={delta}", content: null);
            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (AdjustStock).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (AdjustStock).", ex);
        }
    }
}
