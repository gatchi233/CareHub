using System.Net.Http.Json;
using CareHub.Desktop.Models;
using CareHub.Desktop.Services.Sync;
using CareHub.Services.Abstractions;

namespace CareHub.Services.Remote;

public sealed class MarApiService : IMarService
{
    private readonly HttpClient _http;

    public MarApiService(HttpClient http) => _http = http;

    public async Task<List<MarEntry>> LoadAsync(Guid? residentId, DateTime fromUtc, DateTime toUtc)
    {
        try
        {
            var url = $"api/Mar?fromUtc={fromUtc:O}&toUtc={toUtc:O}";
            if (residentId.HasValue)
                url += $"&residentId={residentId.Value}";

            var items = await _http.GetFromJsonAsync<List<MarEntry>>(url);
            return items ?? new List<MarEntry>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Load MAR).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Load MAR).", ex);
        }
    }

    public async Task CreateAsync(MarEntry entry)
    {
        try
        {
            var payload = new
            {
                clientRequestId  = entry.ClientRequestId,
                residentId       = entry.ResidentId,
                medicationId     = entry.MedicationId,
                status           = entry.Status,
                doseQuantity     = entry.DoseQuantity,
                doseUnit         = entry.DoseUnit,
                administeredAtUtc = entry.AdministeredAtUtc,
                scheduledForUtc  = entry.ScheduledForUtc,
                notes            = entry.Notes,
                recordedBy       = entry.RecordedBy
            };

            var resp = await _http.PostAsJsonAsync("api/Mar", payload);
            resp.EnsureSuccessStatusCode();

            var created = await resp.Content.ReadFromJsonAsync<MarEntry>();
            if (created is not null)
            {
                entry.Id = created.Id;
                entry.CreatedAtUtc = created.CreatedAtUtc;
                entry.UpdatedAtUtc = created.UpdatedAtUtc;
            }
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Create MAR).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Create MAR).", ex);
        }
    }

    public Task<int> SyncAsync() => Task.FromResult(0);
}
