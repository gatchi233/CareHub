using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Remote
{
    public class MedicationApiService : IMedicationService
    {
        private readonly HttpClient _http;

        public MedicationApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Medication>> LoadAsync()
        {
            if (_http.BaseAddress is null)
                throw new InvalidOperationException("MedicationApiService HttpClient.BaseAddress is NULL. DI registration is wrong.");

            return await _http.GetFromJsonAsync<List<Medication>>("api/medications")
                   ?? new List<Medication>();
        }

        public async Task UpsertAsync(Medication item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
            {
                var resp = await _http.PostAsJsonAsync("api/medications", item);
                resp.EnsureSuccessStatusCode();

                // If API returns created medication, sync the Id
                var created = await resp.Content.ReadFromJsonAsync<Medication>();
                if (created != null && created.Id > Guid.Empty)
                    item.Id = created.Id;

                return;
            }

            var put = await _http.PutAsJsonAsync($"api/medications/{item.Id}", item);
            put.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(Medication item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.Id <= Guid.Empty) return;

            var del = await _http.DeleteAsync($"api/medications/{item.Id}");
            del.EnsureSuccessStatusCode();
        }

        public async Task<List<Medication>> GetLowStockAsync()
        {
            return await _http.GetFromJsonAsync<List<Medication>>("api/medications/lowstock")
                   ?? new List<Medication>();
        }

        public async Task AdjustStockAsync(Guid medicationId, int delta)
        {
            // POST api/medications/{id}/adjustStock?delta=...
            var resp = await _http.PostAsync($"api/medications/{medicationId}/adjustStock?delta={delta}", content: null);
            resp.EnsureSuccessStatusCode();
        }
    }
}