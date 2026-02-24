using System.Text.Json;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Local
{
    public class ObservationJsonService : IObservationService
    {
        private readonly string _filePath;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        public ObservationJsonService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "Observations.json");
        }

        private async Task EnsureSeedDataAsync()
        {
            if (File.Exists(_filePath))
                return;

            try
            {
                await using var inStream =
                    await FileSystem.OpenAppPackageFileAsync("Observations.json");
                await using var outStream = File.Create(_filePath);
                await inStream.CopyToAsync(outStream);
            }
            catch
            {
                await WriteAllTextWithRetryAsync(_filePath, "[]");
            }
        }

        public async Task<List<Observation>> LoadAsync()
        {
            await EnsureSeedDataAsync();

            try
            {
                var fi = new FileInfo(_filePath);
                if (fi.Exists && fi.Length == 0)
                {
                    await WriteAllTextWithRetryAsync(_filePath, "[]");
                    return new List<Observation>();
                }
            }
            catch
            {
                // ignore and continue to normal flow
            }

            try
            {
                await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                var items = await JsonSerializer.DeserializeAsync<List<Observation>>(stream, _jsonOptions);
                return items ?? new List<Observation>();
            }
            catch (JsonException)
            {
                // Backup the corrupt file and reset
                try
                {
                    var backup = _filePath + ".corrupt_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    File.Copy(_filePath, backup, overwrite: true);
                }
                catch
                {
                    // ignore backup errors
                }

                await WriteAllTextWithRetryAsync(_filePath, "[]");
                return new List<Observation>();
            }
            catch
            {
                // Any other IO issue → safe fallback
                return new List<Observation>();
            }
        }

        public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
        {
            var list = await LoadAsync();
            return list.Where(o => o.ResidentId == residentId)
                       .OrderByDescending(o => o.ObservedAt)
                       .ToList();
        }

        private async Task SaveAsync(List<Observation> items)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    await using var stream = new FileStream(
                        _filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Read);

                    await JsonSerializer.SerializeAsync(stream, items, _jsonOptions);
                    return;
                }
                catch (IOException) when (attempt < 2)
                {
                    await Task.Delay(50);
                }
            }
        }

        public async Task UpsertAsync(Observation item)
        {
            var list = await LoadAsync();

            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
                list.Add(item);
            }
            else
            {
                var existing = list.FirstOrDefault(m => m.Id == item.Id);
                if (existing != null)
                    list.Remove(existing);

                list.Add(item);
            }   

            await SaveAsync(list);
        }

        public async Task DeleteAsync(Observation item)
        {
            var list = await LoadAsync();
            list.RemoveAll(m => m.Id == item.Id);
            await SaveAsync(list);
        }

        public async Task<List<Observation>> LoadByResidentIdAsync(Guid residentId)
        {
            var list = await LoadAsync();
            return list
                .Where(o => o.ResidentId == residentId)
                .OrderByDescending(o => o.ObservedAt)
                .ToList();
        }

        public async Task<List<Observation>> LoadRecentAsync(Guid residentId, int days)
        {
            var cutoff = DateTime.Now.AddDays(-days);
            var list = await LoadAsync();

            return list
                .Where(o => o.ResidentId == residentId && o.ObservedAt >= cutoff)
                .OrderByDescending(o => o.ObservedAt)
                .ToList();
        }

        public async Task<Observation?> LoadLatestAsync(Guid residentId)
        {
            var list = await LoadAsync();

            return list
                .Where(o => o.ResidentId == residentId)
                .OrderByDescending(o => o.ObservedAt)
                .FirstOrDefault();
        }

        private static async Task WriteAllTextWithRetryAsync(string path, string content)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    await File.WriteAllTextAsync(path, content);
                    return;
                }
                catch (IOException) when (attempt < 2)
                {
                    await Task.Delay(50);
                }
            }
        }
    }
}
