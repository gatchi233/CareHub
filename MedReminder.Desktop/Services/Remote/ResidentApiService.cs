using System.Net.Http.Json;
using MedReminder.Models;
using MedReminder.Services.Abstractions;


namespace MedReminder.Services.Remote
{
    public class ResidentApiService : IResidentService
    {
        private readonly HttpClient _http;

        public ResidentApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Resident>> LoadAsync()
        {
            // GET api/residents
            return await _http.GetFromJsonAsync<List<Resident>>("api/residents")
                   ?? new List<Resident>();
        }

        public async Task UpsertAsync(Resident item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            // Your model uses int Id. We keep the same semantics as your JSON service:
            // - Id == 0 => create
            // - Id > 0  => update
            if (item.Id == Guid.Empty)
            {
                // POST api/residents
                var resp = await _http.PostAsJsonAsync("api/residents", item);
                resp.EnsureSuccessStatusCode();

                // Recommended: API returns the created resident with generated Id
                var created = await resp.Content.ReadFromJsonAsync<Resident>();
                if (created != null && created.Id != Guid.Empty)
                    item.Id = created.Id;

                return;
            }

            // PUT api/residents/{id}
            var put = await _http.PutAsJsonAsync($"api/residents/{item.Id}", item);
            put.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(Resident item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.Id <= Guid.Empty) return;

            // DELETE api/residents/{id}
            var del = await _http.DeleteAsync($"api/residents/{item.Id}");
            del.EnsureSuccessStatusCode();
        }
    }
}