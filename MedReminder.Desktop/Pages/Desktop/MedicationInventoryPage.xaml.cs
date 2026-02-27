using CommunityToolkit.Maui.Views;
using MedReminder.Models;
using MedReminder.Pages.UI;
using MedReminder.Pages.UI.Popups;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MedReminder.Pages.Desktop
{
    public partial class MedicationInventoryPage : AuthPage
    {
        private MedicationInventoryViewModel VM => (MedicationInventoryViewModel)BindingContext;

        // Local snapshot for search filtering without changing the ViewModel
        private List<MedicationInventoryRow> _allItems = new();

        public MedicationInventoryPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            BindingContext = services.GetRequiredService<MedicationInventoryViewModel>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ReloadAndSyncAsync();
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
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

            if (string.IsNullOrWhiteSpace(query))
            {
                // Restore binding source
                InventoryCollection.ItemsSource = VM.Items;
                return;
            }

            var filtered = _allItems
                .Where(x => !string.IsNullOrWhiteSpace(x.MedName) &&
                            x.MedName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            InventoryCollection.ItemsSource = filtered;
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

        private async void OnOrdersClicked(object sender, EventArgs e)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["returnTo"] = $"//{nameof(MedicationInventoryPage)}"
            };

            await Shell.Current.GoToAsync(nameof(MedicationOrdersPage), true, parameters);
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
