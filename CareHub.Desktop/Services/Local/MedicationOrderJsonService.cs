using System.Text.Json;
using System.Text.Json.Serialization;
using CareHub.Models;
using CareHub.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CareHub.Services.Local
{
    public class MedicationOrderJsonService : IMedicationOrderService
    {
        private static string GetCurrentUserLabel()
        {
            var auth = MauiProgram.Services.GetService<AuthService>();
            if (auth?.CurrentUser == null) return "Unknown";
            return $"{auth.CurrentUser.StaffName} ({auth.CurrentUser.Role})";
        }

        private readonly string _filePath;
        private readonly IMedicationService _medicationService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters =
            {
                new MedicationOrderStatusJsonConverter()
            }
        };

        public MedicationOrderJsonService(IMedicationService medicationService)
        {
            _medicationService = medicationService;
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "MedicationOrders.json");
        }

        public async Task<List<MedicationOrder>> LoadAsync()
        {
            if (!File.Exists(_filePath))
                return new List<MedicationOrder>();

            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<MedicationOrder>>(stream, JsonOptions)
                   ?? new List<MedicationOrder>();
        }

        private async Task SaveAsync(List<MedicationOrder> items)
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions);
        }

        public async Task<MedicationOrder> CreateAsync(Guid medicationId, int requestedQuantity, string? requestedBy, string? notes)
        {
            var list = await LoadAsync();

            // Resolve medication name for display
            string? medName = null;
            try
            {
                var meds = await _medicationService.LoadAsync();
                medName = meds.FirstOrDefault(m => m.Id == medicationId)?.MedName;
            }
            catch { /* offline — name stays null */ }

            var order = new MedicationOrder
            {
                Id = Guid.NewGuid(),
                MedicationId = medicationId,
                MedicationName = medName,
                RequestedQuantity = requestedQuantity,
                Status = MedicationOrderStatus.Requested,
                RequestedAt = DateTime.UtcNow,
                RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "Staff" : requestedBy,
                Notes = notes
            };

            list.Add(order);
            await SaveAsync(list);

            return order;
        }

        public async Task UpdateStatusAsync(Guid orderId, MedicationOrderStatus newStatus, DateTimeOffset? expiryDate = null)
        {
            var list = await LoadAsync();
            var order = list.FirstOrDefault(x => x.Id == orderId);
            if (order == null)
                return;

            var oldStatus = order.Status;

            // No-op
            if (oldStatus == newStatus)
                return;

            // Step 2: lock transitions
            // Allowed:
            // Requested -> Ordered or Cancelled
            // Ordered   -> Received or Cancelled
            // Received  -> (no transitions)
            // Cancelled -> (no transitions)
            var isAllowed = oldStatus switch
            {
                MedicationOrderStatus.Requested => newStatus is MedicationOrderStatus.Ordered or MedicationOrderStatus.Cancelled,
                MedicationOrderStatus.Ordered => newStatus is MedicationOrderStatus.Received or MedicationOrderStatus.Cancelled,
                MedicationOrderStatus.Received => false,
                MedicationOrderStatus.Cancelled => false,
                _ => false
            };

            if (!isAllowed)
                return;

            order.Status = newStatus;

            if (newStatus == MedicationOrderStatus.Ordered)
            {
                order.OrderedAt ??= DateTime.Now;

                // Keep these if your model supports them (you’ve been showing “by Staff” in UI)
                order.OrderedBy ??= GetCurrentUserLabel();
            }

            if (newStatus == MedicationOrderStatus.Received)
            {
                order.ReceivedAt ??= DateTime.Now;
                order.ReceivedBy ??= GetCurrentUserLabel();
                order.ReceivedExpiryDate = expiryDate;

                // Increase inventory only once (when transitioning into Received)
                if (oldStatus != MedicationOrderStatus.Received)
                {
                    await _medicationService.AdjustStockAsync(order.MedicationId, order.RequestedQuantity);

                    // Update medication expiry date if provided
                    if (expiryDate.HasValue)
                    {
                        var meds = await _medicationService.LoadAsync();
                        var med = meds.FirstOrDefault(m => m.Id == order.MedicationId);
                        if (med != null)
                        {
                            med.ExpiryDate = expiryDate.Value;
                            await _medicationService.UpsertAsync(med);
                        }
                    }
                }
            }

            if (newStatus == MedicationOrderStatus.Cancelled)
            {
                order.CancelledAt ??= DateTime.Now;
                order.CancelledBy ??= GetCurrentUserLabel();

                // Do not touch OrderedAt/ReceivedAt here.
                // Because:
                // - Cancelling is only allowed before Received (guarded above)
                // - Keeping OrderedAt is useful if it was already ordered
            }

            await SaveAsync(list);
        }

        public async Task DeleteAsync(Guid orderId)
        {
            var list = await LoadAsync();
            list.RemoveAll(x => x.Id == orderId);
            await SaveAsync(list);
        }

        public async Task<List<MedicationOrder>> GetByMedicationIdAsync(Guid medicationId)
        {
            var list = await LoadAsync();
            return list
                .Where(x => x.MedicationId == medicationId)
                .OrderByDescending(x => x.RequestedAt)
                .ToList();
        }
    }

    /// <summary>
    /// Tolerant converter so older JSON doesn't crash:
    /// - Accepts enum names: "Requested", "Ordered", "Received", "Cancelled"
    /// - Accepts UI strings: "Pending to Order", "Pending to Stock In", "Completed"
    /// - Accepts numbers: 0/1/2/3
    /// </summary>
    public class MedicationOrderStatusJsonConverter : JsonConverter<MedicationOrderStatus>
    {
        public override MedicationOrderStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Numeric enum values
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var i) && Enum.IsDefined(typeof(MedicationOrderStatus), i))
                    return (MedicationOrderStatus)i;

                return MedicationOrderStatus.Requested;
            }

            // String values (enum names or UI labels)
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = (reader.GetString() ?? "").Trim();

                // Exact enum names (case-insensitive)
                if (Enum.TryParse<MedicationOrderStatus>(s, ignoreCase: true, out var parsed))
                    return parsed;

                // Backward/alternate display strings
                return s.ToLowerInvariant() switch
                {
                    "pending to order" => MedicationOrderStatus.Requested,
                    "pending to stock in" => MedicationOrderStatus.Ordered,
                    "completed" => MedicationOrderStatus.Received,
                    "canceled" => MedicationOrderStatus.Cancelled,
                    "cancelled" => MedicationOrderStatus.Cancelled,
                    _ => MedicationOrderStatus.Requested
                };
            }

            // Fallback
            return MedicationOrderStatus.Requested;
        }

        public override void Write(Utf8JsonWriter writer, MedicationOrderStatus value, JsonSerializerOptions options)
        {
            // Always write enum name to keep JSON clean + stable
            writer.WriteStringValue(value.ToString());
        }
    }
}
