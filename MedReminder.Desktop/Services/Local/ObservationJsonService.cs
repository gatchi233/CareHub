using System.Text.Json;
using System.Text.Json.Serialization;
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

        static ObservationJsonService()
        {
            _jsonOptions.Converters.Add(new LegacyGuidConverter());
        }

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
                var json = await File.ReadAllTextAsync(_filePath);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<Observation>();

                return JsonSerializer.Deserialize<List<Observation>>(json, _jsonOptions)
                       ?? new List<Observation>();
            }
            catch (System.Text.Json.JsonException)
            {
                await WriteAllTextWithRetryAsync(_filePath, "[]");
                return new List<Observation>();
            }
            catch (IOException)
            {
                return new List<Observation>();
            }
        }

        public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
        {
            var list = await LoadAsync();
            return list.Where(o => o.ResidentId == residentId)
                       .OrderByDescending(o => o.RecordedAt)
                       .ToList();
        }

        public Task AddAsync(Observation observation)
        {
            if (observation is null)
                throw new ArgumentNullException(nameof(observation));

            return UpsertAsync(observation);
        }

        public Task<int> SyncAsync()
        {
            return Task.FromResult(0);
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

        public async Task ReplaceAllAsync(List<Observation> items)
        {
            items ??= new List<Observation>();
            await SaveAsync(items);
        }

        public async Task<List<Observation>> LoadByResidentIdAsync(Guid residentId)
        {
            var list = await LoadAsync();
            return list
                .Where(o => o.ResidentId == residentId)
                .OrderByDescending(o => o.RecordedAt)
                .ToList();
        }

        public async Task<List<Observation>> LoadRecentAsync(Guid residentId, int days)
        {
            var todayLocal = DateTime.Now.Date;
            var fromLocal = todayLocal.AddDays(-(Math.Max(days, 1) - 1));
            var cutoff = fromLocal.ToUniversalTime();
            var list = await LoadAsync();

            return list
                .Where(o => o.ResidentId == residentId && o.RecordedAt >= cutoff)
                .OrderByDescending(o => o.RecordedAt)
                .ToList();
        }

        public async Task<Observation?> LoadLatestAsync(Guid residentId)
        {
            var list = await LoadAsync();

            return list
                .Where(o => o.ResidentId == residentId)
                .OrderByDescending(o => o.RecordedAt)
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

        private sealed class LegacyGuidConverter : JsonConverter<Guid>
        {
            public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var s = reader.GetString();
                    if (Guid.TryParse(s, out var g))
                        return g;

                    if (long.TryParse(s, out var l))
                        return GuidFromInt64(l);
                }

                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var num))
                    return GuidFromInt64(num);

                if (reader.TokenType == JsonTokenType.Null)
                    return Guid.Empty;

                throw new JsonException($"Unable to convert token '{reader.TokenType}' to Guid.");
            }

            public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }

            private static Guid GuidFromInt64(long value)
            {
                var bytes = new byte[16];
                BitConverter.GetBytes(value).CopyTo(bytes, 0);
                return new Guid(bytes);
            }
        }
    }
}
