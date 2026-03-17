using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using CareHub.Models;
using CareHub.Services.Abstractions;

namespace CareHub.Services.Local
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
            if (!File.Exists(_filePath))
            {
                await using var inStream = await FileSystem.OpenAppPackageFileAsync("Residents.json");
                await using var outStream = File.Create(_filePath);
                await inStream.CopyToAsync(outStream);
                return;
            }

            // Migrate legacy "DOB" key to "DateOfBirth" in existing AppData JSON
            var text = await File.ReadAllTextAsync(_filePath);
            if (text.Contains("\"DOB\""))
            {
                text = text.Replace("\"DOB\"", "\"DateOfBirth\"");
                await File.WriteAllTextAsync(_filePath, text);
            }
        }

        private async Task<List<Resident>> LoadInternalAsync()
        {
            await EnsureSeedDataAsync();

            if (!File.Exists(_filePath))
                return new List<Resident>();

            var text = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(text))
                return new List<Resident>();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                return JsonSerializer.Deserialize<List<Resident>>(text, options) ?? new List<Resident>();
            }
            catch (JsonException)
            {
                var migrated = TryParseLegacyResidents(text);
                if (migrated.Count > 0)
                    await SaveInternalAsync(migrated);
                return migrated;
            }
        }

        private static List<Resident> TryParseLegacyResidents(string json)
        {
            var list = new List<Resident>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return list;

                foreach (var e in doc.RootElement.EnumerateArray())
                {
                    var id = Guid.NewGuid();
                    if (e.TryGetProperty("Id", out var idProp))
                    {
                        if (idProp.ValueKind == JsonValueKind.String
                            && Guid.TryParse(idProp.GetString(), out var gid))
                            id = gid;
                        else if (idProp.ValueKind == JsonValueKind.Number
                            && idProp.TryGetInt32(out var iid))
                            id = LegacyIntToGuid(iid);
                    }

                    string Get(params string[] names)
                    {
                        foreach (var n in names)
                        {
                            if (e.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String)
                                return p.GetString() ?? string.Empty;
                        }
                        return string.Empty;
                    }

                    list.Add(new Resident
                    {
                        Id = id,
                        ResidentFName = Get("ResidentFName", "FirstName"),
                        ResidentLName = Get("ResidentLName", "LastName"),
                        SIN = Get("SIN"),
                        DateOfBirth = Get("DateOfBirth", "DOB"),
                        Gender = string.IsNullOrWhiteSpace(Get("Gender")) ? null : Get("Gender"),
                        Address = string.IsNullOrWhiteSpace(Get("Address")) ? null : Get("Address"),
                        City = string.IsNullOrWhiteSpace(Get("City")) ? null : Get("City"),
                        Province = string.IsNullOrWhiteSpace(Get("Province")) ? null : Get("Province"),
                        PostalCode = string.IsNullOrWhiteSpace(Get("PostalCode")) ? null : Get("PostalCode"),
                        EmergencyContactName1 = Get("EmergencyContactName1"),
                        EmergencyContactPhone1 = Get("EmergencyContactPhone1"),
                        EmergencyRelationship1 = Get("EmergencyRelationship1"),
                        EmergencyContactName2 = string.IsNullOrWhiteSpace(Get("EmergencyContactName2")) ? null : Get("EmergencyContactName2"),
                        EmergencyContactPhone2 = string.IsNullOrWhiteSpace(Get("EmergencyContactPhone2")) ? null : Get("EmergencyContactPhone2"),
                        EmergencyRelationship2 = string.IsNullOrWhiteSpace(Get("EmergencyRelationship2")) ? null : Get("EmergencyRelationship2"),
                        DoctorName = Get("DoctorName"),
                        DoctorContact = Get("DoctorContact"),
                        RoomNumber = string.IsNullOrWhiteSpace(Get("RoomNumber")) ? null : Get("RoomNumber"),
                        RoomType = string.IsNullOrWhiteSpace(Get("RoomType")) ? null : Get("RoomType"),
                        BedLabel = string.IsNullOrWhiteSpace(Get("BedLabel")) ? null : Get("BedLabel"),
                        Remarks = string.IsNullOrWhiteSpace(Get("Remarks")) ? null : Get("Remarks"),
                        AllergyOtherItems = string.IsNullOrWhiteSpace(Get("AllergyItems", "AllergyOtherItems")) ? null : Get("AllergyItems", "AllergyOtherItems")
                    });
                }
            }
            catch
            {
                return new List<Resident>();
            }

            return list;
        }

        private static Guid LegacyIntToGuid(int value)
        {
            Span<byte> bytes = stackalloc byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes);
            return new Guid(bytes);
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
                    item.Id = Guid.NewGuid();
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

            if (item.Id != Guid.Empty)
                list.RemoveAll(r => r.Id == item.Id);

            await SaveInternalAsync(list);
        }

        public async Task ReplaceAllAsync(List<Resident> items)
        {
            items ??= new List<Resident>();
            await SaveInternalAsync(items);
        }
    }
}
