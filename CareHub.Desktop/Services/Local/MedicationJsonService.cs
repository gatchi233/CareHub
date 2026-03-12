using System.Text.Json;
using CareHub.Models;
using CareHub.Services.Abstractions;

namespace CareHub.Services.Local
{
    public class MedicationJsonService : IMedicationService
    {
        private readonly string _filePath;

        public MedicationJsonService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "Medications.json");
        }

        private const string InventoryVersion = "inventory_v3";

        private async Task EnsureInitialInventoryAsync()
        {
            if (!File.Exists(_filePath))
            {
                await ResetInventoryFromPackageAsync();
                WriteVersionMarker();
                return;
            }

            // Version-based re-seed: if marker is missing, purge stale inventory and re-merge
            var markerPath = Path.Combine(FileSystem.AppDataDirectory, InventoryVersion);
            if (!File.Exists(markerPath))
            {
                try
                {
                    await using var stream = File.OpenRead(_filePath);
                    var items = await JsonSerializer.DeserializeAsync<List<Medication>>(stream)
                                ?? new List<Medication>();

                    // Remove old inventory-only items (ResidentId == null/Empty)
                    items.RemoveAll(m => m.ResidentId == null || m.ResidentId == Guid.Empty);

                    // Re-merge from packaged seed
                    await using var seedStream = await FileSystem.OpenAppPackageFileAsync("Inventory.json");
                    var seedItems = await JsonSerializer.DeserializeAsync<List<Medication>>(seedStream)
                                    ?? new List<Medication>();

                    items.AddRange(seedItems);
                    await SaveAsync(items);
                }
                catch
                {
                    await ResetInventoryFromPackageAsync();
                }

                WriteVersionMarker();
                return;
            }

            // File exists — check if inventory items are already present
            try
            {
                await using var stream = File.OpenRead(_filePath);
                var items = await JsonSerializer.DeserializeAsync<List<Medication>>(stream)
                            ?? new List<Medication>();

                bool hasInventory = items.Any(m => m.ResidentId == null || m.ResidentId == Guid.Empty);
                if (hasInventory)
                    return;

                // No inventory items — merge seed data into existing medications
                await using var seedStream = await FileSystem.OpenAppPackageFileAsync("Inventory.json");
                var seedItems = await JsonSerializer.DeserializeAsync<List<Medication>>(seedStream)
                                ?? new List<Medication>();

                items.AddRange(seedItems);
                await SaveAsync(items);
            }
            catch
            {
                // Corrupt file — full reset
                await ResetInventoryFromPackageAsync();
            }
        }

        private static void WriteVersionMarker()
        {
            var markerPath = Path.Combine(FileSystem.AppDataDirectory, InventoryVersion);
            File.WriteAllText(markerPath, InventoryVersion);
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

            var inventory = list.Where(m => m.ResidentId == null || m.ResidentId == Guid.Empty);

            return inventory
                .GroupBy(m => m.MedName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var batches = g.ToList();
                    var totalStock = batches.Sum(b => b.StockQuantity);
                    var expiringStock = batches
                        .Where(b => b.IsExpired || b.DaysUntilExpiry <= 30)
                        .Sum(b => b.StockQuantity);
                    var usableStock = totalStock - expiringStock;
                    var reorderLevel = batches.Max(b => b.ReorderLevel);
                    var representative = batches.First();

                    return new { representative, usableStock, reorderLevel };
                })
                .Where(x => x.usableStock <= x.reorderLevel)
                .OrderBy(x => x.representative.MedName)
                .Select(x =>
                {
                    // Return a representative med with StockQuantity set to usable stock for display
                    x.representative.StockQuantity = x.usableStock;
                    return x.representative;
                })
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
        public async Task AdjustStockFifoAsync(string medName, int delta)
        {
            var list = await LoadAsync();
            var batches = list
                .Where(m => (m.ResidentId == null || m.ResidentId == Guid.Empty)
                    && string.Equals(m.MedName, medName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.ExpiryDate)
                .ToList();

            if (batches.Count == 0)
                return;

            if (delta < 0)
            {
                // Deduct from earliest-expiring batch first (FIFO)
                int remaining = -delta;
                foreach (var batch in batches)
                {
                    if (remaining <= 0) break;

                    int deduct = Math.Min(batch.StockQuantity, remaining);
                    batch.StockQuantity -= deduct;
                    remaining -= deduct;
                }
            }
            else if (delta > 0)
            {
                // Restore to the earliest batch
                batches[0].StockQuantity += delta;
            }

            await SaveAsync(list);
        }

        public async Task ReplaceAllAsync(List<Medication> items)
        {
            items ??= new List<Medication>();
            await SaveAsync(items);
        }

        /// <summary>
        /// Merges resident medications from the packaged Medications.json seed
        /// into the local store. Uses resident list to remap seed ResidentIds
        /// to actual IDs (in case the API assigned different GUIDs).
        /// Deduplicates by (MedName, ResidentId) to avoid double-seeding.
        /// </summary>
        public async Task SeedResidentMedicationsAsync(List<Resident>? residents = null)
        {
            try
            {
                await using var medStream = await FileSystem.OpenAppPackageFileAsync("Medications.json");
                var seedItems = await JsonSerializer.DeserializeAsync<List<Medication>>(medStream)
                                ?? new List<Medication>();

                if (seedItems.Count == 0)
                    return;

                // Build a seed-ResidentId → actual-ResidentId lookup via Residents.json names
                var idRemap = new Dictionary<Guid, Guid>();
                if (residents != null && residents.Count > 0)
                {
                    // Load packaged residents to get the seed-side IDs
                    try
                    {
                        await using var resStream = await FileSystem.OpenAppPackageFileAsync("Residents.json");
                        var seedResidents = await JsonSerializer.DeserializeAsync<List<Resident>>(resStream)
                                            ?? new List<Resident>();

                        foreach (var sr in seedResidents)
                        {
                            var seedName = $"{sr.ResidentFName} {sr.ResidentLName}".Trim();
                            var actual = residents.FirstOrDefault(r =>
                                string.Equals($"{r.ResidentFName} {r.ResidentLName}".Trim(),
                                              seedName, StringComparison.OrdinalIgnoreCase));
                            if (actual != null && sr.Id != actual.Id)
                                idRemap[sr.Id] = actual.Id;
                        }
                    }
                    catch { /* seed residents not available */ }
                }

                var list = await LoadAsync();

                // Fix existing entries that have stale seed ResidentIds
                if (idRemap.Count > 0)
                {
                    foreach (var med in list)
                    {
                        if (med.ResidentId.HasValue && idRemap.TryGetValue(med.ResidentId.Value, out var remappedId))
                            med.ResidentId = remappedId;
                    }

                    // Remove duplicates created by remapping (keep first per MedName+ResidentId)
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    list.RemoveAll(m =>
                    {
                        if (!m.ResidentId.HasValue || m.ResidentId.Value == Guid.Empty)
                            return false;
                        var k = $"{(m.MedName ?? "").ToLowerInvariant()}|{m.ResidentId}";
                        return !seen.Add(k);
                    });
                }

                // Build existing (MedName, ResidentId) keys
                var existingKeys = new HashSet<string>(
                    list.Where(m => m.ResidentId.HasValue && m.ResidentId.Value != Guid.Empty)
                        .Select(m => $"{(m.MedName ?? "").ToLowerInvariant()}|{m.ResidentId}"),
                    StringComparer.Ordinal);

                int added = 0;
                foreach (var med in seedItems)
                {
                    if (!med.ResidentId.HasValue || med.ResidentId.Value == Guid.Empty)
                        continue;

                    // Remap resident ID if needed
                    if (idRemap.TryGetValue(med.ResidentId.Value, out var actualId))
                        med.ResidentId = actualId;

                    var key = $"{(med.MedName ?? "").ToLowerInvariant()}|{med.ResidentId}";
                    if (existingKeys.Contains(key))
                        continue;

                    // Keep original seed ID so both API and desktop use the same IDs,
                    // preventing DataSeedService from re-inserting duplicates on API restart.
                    if (med.Id == Guid.Empty)
                        med.Id = Guid.NewGuid();
                    list.Add(med);
                    existingKeys.Add(key);
                    added++;
                }

                if (added > 0 || idRemap.Count > 0)
                    await SaveAsync(list);
            }
            catch
            {
                // Seed file not available — skip silently
            }
        }
    }
}
