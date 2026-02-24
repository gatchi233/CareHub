using System.Text.Json;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Local
{
    public class ResidentJsonService : IResidentService
    {
        private readonly string _filePath;

        public ResidentJsonService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "Residents.json");
        }

        private async Task EnsureSeedDataAsync()
        {
            // ✅ Always: only seed once if the AppData file doesn't exist
            if (File.Exists(_filePath))
                return;

            await using var inStream = await FileSystem.OpenAppPackageFileAsync("Residents.json");
            await using var outStream = File.Create(_filePath);
            await inStream.CopyToAsync(outStream);
        }

        private async Task<List<Resident>> LoadInternalAsync()
        {
            await EnsureSeedDataAsync();

            if (!File.Exists(_filePath))
                return new List<Resident>();

            await using var stream = File.OpenRead(_filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var residents = await JsonSerializer.DeserializeAsync<List<Resident>>(stream, options)
                           ?? new List<Resident>();

            return residents;
        }

        private async Task SaveInternalAsync(List<Resident> items)
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(
                stream,
                items,
                new JsonSerializerOptions { WriteIndented = true });
        }

        public Task<List<Resident>> LoadAsync() => LoadInternalAsync();

        public async Task UpsertAsync(Resident item)
        {
            var list = await LoadInternalAsync();

            if (item.Id == Guid.Empty)
            {
                {
                    item.Id = Guid.NewGuid();
                }
                list.Add(item);
            }
            else
            {
                var existing = list.FirstOrDefault(r => r.Id == item.Id);
                if (existing != null)
                    list.Remove(existing);

                list.Add(item);
            }

            await SaveInternalAsync(list);
        }

        public async Task DeleteAsync(Resident item)
        {
            var list = await LoadInternalAsync();

            if (item.Id > Guid.Empty)
                list.RemoveAll(r => r.Id == item.Id);

            await SaveInternalAsync(list);
        }
    }
}