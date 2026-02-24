using MedReminder.Models;
using MedReminder.Services.Abstractions;
using System.Text.Json;

namespace MedReminder.Services.Local
{
    public class StaffJsonService : IStaffService
    {
        private const string FileName = "Staff.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly SemaphoreSlim _mutex = new(1, 1);

        private string GetFilePath()
        {
            var baseDir = AppContext.BaseDirectory;

            var candidates = new[]
            {
                Path.Combine(baseDir, "Data", FileName),
                Path.Combine(baseDir, FileName),
                Path.Combine(baseDir, "..", "..", "..", "Data", FileName),
                Path.Combine(baseDir, "..", "..", "..", "..", "Data", FileName),
            };

            foreach (var c in candidates)
            {
                var full = Path.GetFullPath(c);
                if (File.Exists(full))
                    return full;
            }

            // default create location
            return Path.GetFullPath(Path.Combine(baseDir, "Data", FileName));
        }

        public async Task<List<StaffRecord>> GetAllAsync()
        {
            await _mutex.WaitAsync();
            try
            {
                var path = GetFilePath();
                if (!File.Exists(path))
                    return new List<StaffRecord>();

                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<StaffRecord>>(json, JsonOptions) ?? new List<StaffRecord>();
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task SaveAllAsync(List<StaffRecord> staff)
        {
            await _mutex.WaitAsync();
            try
            {
                var path = GetFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var json = JsonSerializer.Serialize(staff, JsonOptions);
                await File.WriteAllTextAsync(path, json);
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task AddOrUpdateAsync(StaffRecord staff)
        {
            if (string.IsNullOrWhiteSpace(staff.EmployeeId))
                throw new InvalidOperationException("EmployeeId is required (e.g. EMP-019).");

            await _mutex.WaitAsync();
            try
            {
                var list = await GetAllAsync();

                var idx = list.FindIndex(s =>
                    s.EmployeeId.Equals(staff.EmployeeId, StringComparison.OrdinalIgnoreCase));

                if (idx >= 0)
                    list[idx] = staff;
                else
                    list.Add(staff);

                await SaveAllAsync(list);
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task SetEnabledAsync(string employeeId, bool isEnabled)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
                return;

            await _mutex.WaitAsync();
            try
            {
                var list = await GetAllAsync();
                var staff = list.FirstOrDefault(s =>
                    s.EmployeeId.Equals(employeeId, StringComparison.OrdinalIgnoreCase));

                if (staff == null)
                    throw new InvalidOperationException("Staff not found.");

                staff.IsEnabled = isEnabled;
                await SaveAllAsync(list);
            }
            finally
            {
                _mutex.Release();
            }
        }
    }
}
