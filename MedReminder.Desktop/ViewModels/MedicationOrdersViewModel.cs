using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels
{
    public class MedicationOrdersViewModel : INotifyPropertyChanged
    {
        private readonly IMedicationService _medicationService;
        private readonly IMedicationOrderService _orderService;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Medication> InventoryMedications { get; } = new();
        public ObservableCollection<MedicationOrderRow> Orders { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand MarkOrderedCommand { get; }
        public ICommand MarkReceivedCommand { get; }
        public ICommand CancelCommand { get; }

        public MedicationOrdersViewModel(IMedicationService medicationService, IMedicationOrderService orderService)
        {
            _medicationService = medicationService;
            _orderService = orderService;

            RefreshCommand = new Command(async () => await LoadAsync());

            MarkOrderedCommand = new Command<MedicationOrderRow>(async row =>
            {
                if (row == null) return;
                await _orderService.UpdateStatusAsync(row.OrderId, MedicationOrderStatus.Ordered);
                await LoadAsync();
            });

            MarkReceivedCommand = new Command<MedicationOrderRow>(async row =>
            {
                if (row == null) return;
                await _orderService.UpdateStatusAsync(row.OrderId, MedicationOrderStatus.Received);
                await LoadAsync();
            });

            CancelCommand = new Command<MedicationOrderRow>(async row =>
            {
                if (row == null) return;
                await _orderService.UpdateStatusAsync(row.OrderId, MedicationOrderStatus.Cancelled);
                await LoadAsync();
            });
        }

        private static bool IsInventoryMedication(Medication m)
        {
            // Inventory meds are not attached to a resident
            return !m.ResidentId.HasValue || m.ResidentId.Value == Guid.Empty;
        }
        public async Task LoadAsync()
        {
            IsBusy = true;

            try
            {
                var meds = await _medicationService.LoadAsync();

                // Global inventory only
                var inventory = meds
                    .Where(IsInventoryMedication)
                    .OrderBy(m => m.MedName)
                    .ToList();

                InventoryMedications.Clear();
                foreach (var m in inventory)
                    InventoryMedications.Add(m);

                // Orders
                var orders = await _orderService.LoadAsync();
                var medNameMap = meds.ToDictionary(m => m.Id, m => m.MedName);

                Orders.Clear();
                foreach (var o in orders.OrderByDescending(x => x.RequestedAt))
                {
                    medNameMap.TryGetValue(o.MedicationId, out var medName);
                    medName ??= $"Medication #{o.MedicationId}";
                    Orders.Add(new MedicationOrderRow(o, medName));
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CreateOrderAsync(Guid medicationId, int qty, string? requestedBy, string? notes)
        {
            if (qty <= 0) return;
            await _orderService.CreateAsync(medicationId, qty, requestedBy, notes);
            await LoadAsync();
        }

        // Create a new GLOBAL inventory medication item (without using resident schedule edit page)
        public async Task<Guid> CreateInventoryMedicationAsync(string medName, int reorderLevel)
        {
            medName = (medName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(medName))
                throw new ArgumentException("Medication name is required.", nameof(medName));

            var list = await _medicationService.LoadAsync();

            var existing = list.FirstOrDefault(m => IsInventoryMedication(m) &&
                string.Equals((m.MedName ?? "").Trim(), medName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                return existing.Id;

            var item = new Medication
            {
                ResidentId = null,
                MedName = medName,
                ReorderLevel = reorderLevel,
                StockQuantity = 0,
                Usage = null
            };

            await _medicationService.UpsertAsync(item);
            return item.Id;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MedicationOrderRow
    {
        private readonly MedicationOrder _o;

        public MedicationOrderRow(MedicationOrder o, string medName)
        {
            _o = o;
            MedicationName = medName;
        }

        public Guid OrderId => _o.Id;
        public string MedicationName { get; }
        public string QuantityText => $"Qty: {_o.RequestedQuantity}";

        private static string Who(string? name) => string.IsNullOrWhiteSpace(name) ? "(unknown)" : name;

        // Status text
        public string StatusDisplay => _o.Status switch
        {
            MedicationOrderStatus.Requested => "Pending to Order",
            MedicationOrderStatus.Ordered => "Pending to Stock In",
            MedicationOrderStatus.Received => "Completed",
            MedicationOrderStatus.Cancelled => "Cancelled",
            _ => _o.Status.ToString()
        };

        public Color StatusBadgeColor => _o.Status switch
        {
            MedicationOrderStatus.Requested => Color.FromArgb("#F0AD4E"),
            MedicationOrderStatus.Ordered => Color.FromArgb("#5BC0DE"),
            MedicationOrderStatus.Received => Color.FromArgb("#5CB85C"),
            MedicationOrderStatus.Cancelled => Color.FromArgb("#777777"),
            _ => Color.FromArgb("#777777")
        };

        // Timeline line strings (we keep them short because the tag already says the word)
        public string RequestedLine => $"{_o.RequestedAt:yyyy-MM-dd HH:mm}  by {Who(_o.RequestedBy)}";

        public string OrderedLine =>
            _o.OrderedAt == null ? "—" : $"{_o.OrderedAt:yyyy-MM-dd HH:mm}  by {Who(_o.OrderedBy)}";

        public string ReceivedLine =>
            _o.ReceivedAt == null ? "—" : $"{_o.ReceivedAt:yyyy-MM-dd HH:mm}  by {Who(_o.ReceivedBy)}";

        public string CancelledLine =>
            _o.CancelledAt == null ? "—" : $"{_o.CancelledAt:yyyy-MM-dd HH:mm}  by {Who(_o.CancelledBy)}";

        public Color RequestedTagColor => Color.FromArgb("#F0AD4E");
        public Color OrderedTagColor => Color.FromArgb("#5BC0DE");
        public Color ReceivedTagColor => Color.FromArgb("#5CB85C");
        public Color CancelledTagColor => Color.FromArgb("#777777");

        public bool IsCancelled => _o.Status == MedicationOrderStatus.Cancelled;

        // Visibility rules (no blank lines):
        // - Requested always shown
        // - Ordered shown only when ordered exists OR status moved past Requested (Ordered/Received/Cancelled)
        // - Third row shown only when Cancelled OR Received
        public bool ShowOrderedRow =>
            _o.OrderedAt != null ||
            _o.Status == MedicationOrderStatus.Ordered ||
            _o.Status == MedicationOrderStatus.Received ||
            _o.Status == MedicationOrderStatus.Cancelled;

        public bool ShowThirdRow =>
            _o.Status == MedicationOrderStatus.Cancelled ||
            _o.Status == MedicationOrderStatus.Received;

        // Third row content (Cancelled OR Received)
        public string ThirdTagText => IsCancelled ? "Cancelled" : "Received";
        public Color ThirdTagColor => IsCancelled ? CancelledTagColor : ReceivedTagColor;
        public string ThirdLineText => IsCancelled ? CancelledLine : ReceivedLine;

        // Button (no delete)
        public bool ShowCancelRequest => _o.Status == MedicationOrderStatus.Requested;
        public bool ShowMarkOrdered => _o.Status == MedicationOrderStatus.Requested;

        public bool ShowCancelOrder => _o.Status == MedicationOrderStatus.Ordered;
        public bool ShowMarkReceived => _o.Status == MedicationOrderStatus.Ordered;
    }
}
