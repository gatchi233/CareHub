using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels
{
    public enum InventorySortMode
    {
        LowStockFirst,
        NameAsc,
        NameDesc,
        StockAsc,
        StockDesc,
        ReorderLevelAsc,
        ReorderLevelDesc
    }

    public class MedicationInventoryViewModel : INotifyPropertyChanged
    {
        private readonly IMedicationService _medService;
        private readonly IMedicationOrderService _orderService;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
        }

        private InventorySortMode _sortMode = InventorySortMode.LowStockFirst;
        public InventorySortMode SortMode
        {
            get => _sortMode;
            set { if (_sortMode == value) return; _sortMode = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MedicationInventoryRow> Items { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand AdjustStockCommand { get; }
        public ICommand CreateOrderCommand { get; }

        public MedicationInventoryViewModel(
            IMedicationService medService,
            IMedicationOrderService orderService)
        {
            _medService = medService;
            _orderService = orderService;

            RefreshCommand = new Command(async () => await LoadAsync());

            AdjustStockCommand = new Command<Tuple<Medication, int>>(async tuple =>
            {
                if (tuple?.Item1 == null) return;

                await _medService.AdjustStockAsync(tuple.Item1.Id, tuple.Item2);
                await LoadAsync();
            });

            CreateOrderCommand = new Command<Tuple<Medication, int, string?>>(async tuple =>
            {
                if (tuple?.Item1 == null) return;

                var med = tuple.Item1;
                var qty = tuple.Item2;
                var notes = tuple.Item3;

                await _orderService.CreateAsync(med.Id, qty, "Staff", notes);
                await LoadAsync();
            });
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                Items.Clear();

                var list = await _medService.LoadAsync();

                // Global inventory for the whole retirement home
                var inventory = list
                    .Where(m => m.ResidentId == null || m.ResidentId == Guid.Empty)
                    .Select(m => new MedicationInventoryRow(m))
                    .ToList();

                IEnumerable<MedicationInventoryRow> sorted = SortMode switch
                {
                    InventorySortMode.NameAsc =>
                        inventory.OrderBy(x => x.MedName),

                    InventorySortMode.NameDesc =>
                        inventory.OrderByDescending(x => x.MedName),

                    InventorySortMode.StockAsc =>
                        inventory.OrderBy(x => x.StockQuantity).ThenBy(x => x.MedName),

                    InventorySortMode.StockDesc =>
                        inventory.OrderByDescending(x => x.StockQuantity).ThenBy(x => x.MedName),

                    InventorySortMode.ReorderLevelAsc =>
                        inventory.OrderBy(x => x.ReorderLevel).ThenBy(x => x.MedName),

                    InventorySortMode.ReorderLevelDesc =>
                        inventory.OrderByDescending(x => x.ReorderLevel).ThenBy(x => x.MedName),

                    _ => // LowStockFirst
                        inventory.OrderByDescending(x => x.IsLowStock)
                                 .ThenBy(x => x.StockQuantity)
                                 .ThenBy(x => x.MedName)
                };

                foreach (var row in sorted)
                    Items.Add(row);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MedicationInventoryRow
    {
        public MedicationInventoryRow(Medication med)
        {
            Med = med;
        }

        public Medication Med { get; }

        public Guid Id => Med.Id;
        public string MedName => Med.MedName;
        public int StockQuantity => Med.StockQuantity;
        public int ReorderLevel => Med.ReorderLevel;
        public string? Usage => Med.Usage;

        public bool IsLowStock => StockQuantity <= ReorderLevel;
    }
}
