using CommunityToolkit.Maui.Views;
using CareHub.Models;
using CareHub.Pages.UI;
using CareHub.Pages.UI.Popups;
using CareHub.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CareHub.Pages.Desktop
{
    [QueryProperty(nameof(Filter), "filter")]
    public partial class MedicationInventoryPage : AuthPage
    {
        private MedicationInventoryViewModel VM => (MedicationInventoryViewModel)BindingContext;

        // Local snapshot for search filtering without changing the ViewModel
        private List<MedicationInventoryRow> _allItems = new();

        private string? _filter;
        public string? Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                OnPropertyChanged(nameof(Filter));
                OnPropertyChanged(nameof(IsFilterActive));
                OnPropertyChanged(nameof(FilterBannerText));
            }
        }

        public bool IsFilterActive => !string.IsNullOrWhiteSpace(_filter);

        public string FilterBannerText => _filter switch
        {
            "lowstock" => "Showing: Low stock items",
            "expiry" => "Showing: Expiry alerts",
            _ => ""
        };

        public MedicationInventoryPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            BindingContext = services.GetRequiredService<MedicationInventoryViewModel>();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MedSearchBar?.Unfocus();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ReloadAndSyncAsync();
        }

        private async System.Threading.Tasks.Task ReloadAndSyncAsync()
        {
            await VM.LoadAsync();
            CaptureItemsSnapshot();
            ApplyFilter();
        }

        private void CaptureItemsSnapshot()
        {
            // VM.Items should be the source list
            _allItems = VM.Items?.ToList() ?? new List<MedicationInventoryRow>();
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = MedSearchBar?.Text?.Trim();
            IEnumerable<MedicationInventoryRow> source = _allItems;

            // Apply dashboard filter first
            if (_filter == "lowstock")
                source = source.Where(x => x.IsLowStock);
            else if (_filter == "expiry")
                source = source.Where(x => x.HasExpiryAlert);

            // Then apply search text
            if (!string.IsNullOrWhiteSpace(query))
            {
                source = source.Where(x => !string.IsNullOrWhiteSpace(x.MedName) &&
                            x.MedName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrWhiteSpace(query) && !IsFilterActive)
            {
                InventoryCollection.ItemsSource = VM.Items;
                return;
            }

            InventoryCollection.ItemsSource = source.ToList();
        }

        private void OnClearFilterClicked(object sender, EventArgs e)
        {
            Filter = null;
            ApplyFilter();
        }

        private async void OnSortClicked(object sender, EventArgs e)
        {
            var popup = new ActionPopup();
            popup.ConfigureSort();

            var result = await this.ShowPopupAsync(popup);

            if (result is not ActionPopup.PopupResult r || string.IsNullOrWhiteSpace(r.SelectedKey))
                return;

            VM.SortMode = r.SelectedKey switch
            {
                "NameAsc"          => InventorySortMode.NameAsc,
                "NameDesc"         => InventorySortMode.NameDesc,
                "StockAsc"         => InventorySortMode.StockAsc,
                "StockDesc"        => InventorySortMode.StockDesc,
                "ReorderLevelAsc"  => InventorySortMode.ReorderLevelAsc,
                "ReorderLevelDesc" => InventorySortMode.ReorderLevelDesc,
                _                  => InventorySortMode.LowStockFirst
            };

            await ReloadAndSyncAsync();
        }

        private async void OnAdjustClicked(object sender, EventArgs e)
        {
            MedicationInventoryRow? row = null;
            if (e is TappedEventArgs te && te.Parameter is MedicationInventoryRow r1)
                row = r1;
            else if ((sender as BindableObject)?.BindingContext is MedicationInventoryRow r2)
                row = r2;
            if (row is null) return;

            var popup = new ActionPopup();
            popup.ConfigureAdjust(
                title: "ADJUST STOCK",
                medName: row.MedName,
                currentStock: row.StockQuantity,
                reorderLevel: row.ReorderLevel);

            var result = await this.ShowPopupAsync(popup);

            if (result is not ActionPopup.PopupResult r || r.AdjustDelta is null)
                return;

            var delta = r.AdjustDelta.Value;
            if (delta == 0) return;

            VM.AdjustStockCommand.Execute(Tuple.Create(row.Med, delta));
        }

        private async void OnDirectOrderClicked(object sender, EventArgs e)
        {
            MedicationInventoryRow? row = null;
            if (e is TappedEventArgs te && te.Parameter is MedicationInventoryRow r1)
                row = r1;
            else if ((sender as BindableObject)?.BindingContext is MedicationInventoryRow r2)
                row = r2;
            if (row is null) return;

            var popup = new ActionPopup();
            popup.ConfigureOrder(
                title: "CREATE ORDER",
                medName: row.MedName,
                currentStock: row.StockQuantity,
                reorderLevel: row.ReorderLevel);

            var result = await this.ShowPopupAsync(popup);

            if (result is not ActionPopup.PopupResult r || string.IsNullOrWhiteSpace(r.Field1))
                return;

            var qty = int.Parse(r.Field1);

            VM.CreateOrderCommand.Execute(Tuple.Create(row.Med, qty, r.Field2));

            await DisplayAlert("Created", "Order created (Status: Requested).", "OK");
        }

        private async void OnCardTapped(object sender, EventArgs e)
        {
            MedicationInventoryRow? row = null;
            if (e is TappedEventArgs te && te.Parameter is MedicationInventoryRow r1)
                row = r1;
            else if ((sender as BindableObject)?.BindingContext is MedicationInventoryRow r2)
                row = r2;
            if (row is null) return;

            await Shell.Current.GoToAsync(
                $"{nameof(MedicationBatchesPage)}?medName={Uri.EscapeDataString(row.MedName)}");
        }

        private async void OnPendingOrdersClicked(object sender, EventArgs e)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["returnTo"] = $"//{nameof(MedicationInventoryPage)}"
            };

            await Shell.Current.GoToAsync($"//MedicationInventoryPage/{nameof(MedicationOrdersPage)}", parameters);
        }

        private async void OnOrdersClicked(object sender, EventArgs e)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["returnTo"] = $"//{nameof(MedicationInventoryPage)}"
            };

            await Shell.Current.GoToAsync($"//MedicationInventoryPage/{nameof(MedicationOrdersPage)}", parameters);
        }

    }
}
