using System.Text.Json;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Local
{
    public class MedicationJsonService : IMedicationService
    {
        private readonly string _filePath;

        public MedicationJsonService()
        {
            // This is the real persistent store for inventory
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "Medications.json");
        }

        private async Task EnsureInitialInventoryAsync()
        {
            if (File.Exists(_filePath))
                return;

            await ResetInventoryFromPackageAsync();
        }

        public async Task<List<Medication>> LoadAsync()
        {
            await EnsureInitialInventoryAsync();

            if (!File.Exists(_filePath))
                return new List<Medication>();

            try
            {
                await using var stream = File.OpenRead(_filePath);

                var items = await JsonSerializer.DeserializeAsync<List<Medication>>(stream)
                            ?? new List<Medication>();

                return items;
            }
            catch (Exception)
            {
                // Corrupt/legacy JSON (e.g., old numeric IDs). Re-seed from packaged Inventory.json.
                await ResetInventoryFromPackageAsync();

                await using var stream = File.OpenRead(_filePath);
                var items = await JsonSerializer.DeserializeAsync<List<Medication>>(stream)
                            ?? new List<Medication>();
                return items;
            }
        }

        private static async Task ResetInventoryFromPackageAsync()
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "Medications.json");

            await using var inStream = await FileSystem.OpenAppPackageFileAsync("Inventory.json");
            await using var outStream = File.Create(path);
            await inStream.CopyToAsync(outStream);
        }

        private async Task SaveAsync(List<Medication> items)
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(
                stream,
                items,
                new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task UpsertAsync(Medication item)
        {
            var list = await LoadAsync();

            if (item.Id == Guid.Empty)
                item.Id = Guid.NewGuid();

            var existing = list.FirstOrDefault(m => m.Id == item.Id);
            if (existing != null)
                list.Remove(existing);

            list.Add(item);

            await SaveAsync(list);
        }

        public async Task DeleteAsync(Medication item)
        {
            var list = await LoadAsync();
            list.RemoveAll(m => m.Id == item.Id);
            await SaveAsync(list);
        }

        public async Task<List<Medication>> GetLowStockAsync()
        {
            var list = await LoadAsync();

            // Global inventory (whole retirement home): ResidentId is null (or 0 for older data)
            var inventory = list.Where(m => m.ResidentId == null || m.ResidentId == Guid.Empty);

            return inventory
                .Where(m => m.StockQuantity <= m.ReorderLevel)
                .OrderBy(m => m.MedName)
                .ToList();
        }

        public async Task AdjustStockAsync(Guid medicationId, int delta)
        {
            var list = await LoadAsync();
            var med = list.FirstOrDefault(m => m.Id == medicationId);
            if (med == null)
                return;

            med.StockQuantity += delta;
            if (med.StockQuantity < 0)
                med.StockQuantity = 0;

            await SaveAsync(list);
        }
    }
}
