using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CareHub.Models;
using CareHub.Services.Abstractions;

namespace CareHub.ViewModels
{
    public class MedicationBatchesViewModel : INotifyPropertyChanged
    {
        private readonly IMedicationService _medService;

        public MedicationBatchesViewModel(IMedicationService medService)
        {
            _medService = medService;

            AdjustStockCommand = new Command<Medication>(batch =>
            {
                if (batch == null) return;
                _pendingAdjustBatch = batch;
                OnAdjustRequested?.Invoke(batch);
            });

            TrashBatchCommand = new Command<Medication>(batch =>
            {
                if (batch == null || batch.StockQuantity == 0) return;
                _ = TrashAsync(batch);
            });

            ReturnBatchCommand = new Command<Medication>(batch =>
            {
                if (batch == null || batch.StockQuantity == 0) return;
                OnReturnRequested?.Invoke(batch);
            });
        }

        private Medication? _pendingAdjustBatch;
        public Medication? PendingAdjustBatch => _pendingAdjustBatch;

        public Action<Medication>? OnAdjustRequested { get; set; }
        public Action<Medication>? OnReturnRequested { get; set; }

        private async Task TrashAsync(Medication batch)
        {
            await _medService.AdjustStockAsync(batch.Id, -batch.StockQuantity);
            if (MedName != null)
                await LoadAsync(MedName);
        }

        private string? _medName;
        public string? MedName
        {
            get => _medName;
            set { if (_medName == value) return; _medName = value; OnPropertyChanged(); }
        }

        private string? _summary;
        public string? Summary
        {
            get => _summary;
            set { if (_summary == value) return; _summary = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
        }

        private int _totalStock;
        public int TotalStock
        {
            get => _totalStock;
            set { if (_totalStock == value) return; _totalStock = value; OnPropertyChanged(); }
        }

        private int _reorderLevel;
        public int ReorderLevel
        {
            get => _reorderLevel;
            set { if (_reorderLevel == value) return; _reorderLevel = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BatchRow> Batches { get; } = new();

        public ICommand AdjustStockCommand { get; }
        public ICommand TrashBatchCommand { get; }
        public ICommand ReturnBatchCommand { get; }

        public async Task LoadAsync(string medName)
        {
            MedName = medName;
            IsBusy = true;

            try
            {
                Batches.Clear();

                var all = await _medService.LoadAsync();
                var batches = all
                    .Where(m => (m.ResidentId == null || m.ResidentId == Guid.Empty)
                                && string.Equals(m.MedName, medName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(m => m.ExpiryDate)
                    .ToList();

                int num = 1;
                foreach (var b in batches)
                {
                    Batches.Add(new BatchRow(b, num++));
                }

                var totalStock = batches.Sum(b => b.StockQuantity);
                var availableStock = batches.Where(b => !b.IsExpired).Sum(b => b.StockQuantity);
                TotalStock = availableStock;
                ReorderLevel = batches.Count > 0 ? batches.Max(b => b.ReorderLevel) : 0;
                Summary = $"Available stock: {availableStock} · {batches.Count} batch(es)";
            }
            catch { }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ApplyAdjustAsync(int delta)
        {
            if (_pendingAdjustBatch == null) return;
            await _medService.AdjustStockAsync(_pendingAdjustBatch.Id, delta);
            _pendingAdjustBatch = null;
            if (MedName != null)
                await LoadAsync(MedName);
        }

        public async Task ConfirmReturnAsync(Medication batch)
        {
            await _medService.AdjustStockAsync(batch.Id, -batch.StockQuantity);
            if (MedName != null)
                await LoadAsync(MedName);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BatchRow
    {
        public BatchRow(Medication med, int batchNumber)
        {
            Med = med;
            BatchNumber = batchNumber;
        }

        public Medication Med { get; }
        public int BatchNumber { get; }

        public string BatchLabel => $"Batch #{BatchNumber}";
        public int StockQuantity => Med.StockQuantity;
        public string Unit => Med.QuantityUnit ?? "unit";
        public string StockText => $"{StockQuantity} {Unit}(s)";

        public string? PurchaseDateText => Med.PurchaseDateLocal?.ToString("dd MMM yyyy");
        public bool HasPurchaseDate => Med.PurchaseDate.HasValue;

        public string ExpiryDateText => Med.ExpiryDateLocal.ToString("dd MMM yyyy");

        public bool IsExpired => Med.IsExpired;
        public bool IsNotExpired => !IsExpired;
        public bool IsExpiringSoon => !IsExpired && Med.DaysUntilExpiry <= 30;
        public bool ShowWarning => IsExpired || IsExpiringSoon;
        public string WarningText => IsExpired ? "EXPIRED" : "EXPIRING SOON";
        public Color WarningColor => IsExpired
            ? (Color)Application.Current!.Resources["Alert_Error"]
            : (Color)Application.Current!.Resources["Alert_Warning"];

        public Color BorderColor => IsExpired
            ? (Color)Application.Current!.Resources["Alert_Error"]
            : IsExpiringSoon
                ? (Color)Application.Current!.Resources["Alert_Warning"]
                : (Color)Application.Current!.Resources["Medication_Card_Border"];
    }
}
