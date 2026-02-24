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
            popup.ConfigureList(
                title: "Sort",
                items: new[]
                {
            ("LowStockFirst", "Low stock first (default)"),
            ("Name", "Name (A–Z)"),
            ("Stock", "Stock (low → high)"),
            ("ReorderLevel", "Reorder level (high → low)")
                });

            var result = await this.ShowPopupAsync(popup);

            if (result is not ActionPopup.PopupResult r || string.IsNullOrWhiteSpace(r.SelectedKey))
                return;

            VM.SortMode = r.SelectedKey switch
            {
                "Name" => InventorySortMode.Name,
                "Stock" => InventorySortMode.Stock,
                "ReorderLevel" => InventorySortMode.ReorderLevel,
                _ => InventorySortMode.LowStockFirst
            };

            await ReloadAndSyncAsync();
        }

        private async void OnAdjustClicked(object sender, EventArgs e)
        {
            if (sender is not Button { BindingContext: MedicationInventoryRow row })
                return;

            // TODO (later): Supervisor-only adjustment.

            var input = await DisplayPromptAsync(
                "Adjust Stock",
                $"Current stock: {row.StockQuantity}\n\nEnter adjustment for {row.MedName}.\nUse + to add, - to subtract (e.g., 10 or -5).",
                accept: "OK",
                cancel: "Cancel",
                placeholder: "e.g., 10 or -5",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(input))
                return;

            input = input.Trim();

            if (!int.TryParse(input, out var delta) || delta == 0)
            {
                await DisplayAlert("Invalid", "Please enter a non-zero integer (e.g., 10 or -5).", "OK");
                return;
            }

            var newStock = row.StockQuantity + delta;
            if (newStock < 0)
            {
                var setToZero = await DisplayAlert(
                    "Invalid adjustment",
                    $"That would make stock negative.\n\nCurrent: {row.StockQuantity}\nAdjustment: {delta}\nResult: {newStock}\n\nDo you want to set stock to 0 instead?",
                    "Set to 0",
                    "Cancel");

                if (!setToZero)
                    return;

                delta = -row.StockQuantity;

                if (delta == 0)
                    return;
            }

            VM.AdjustStockCommand.Execute(Tuple.Create(row.Med, delta));
        }

        private async void OnDirectOrderClicked(object sender, EventArgs e)
        {
            if (sender is not Button { BindingContext: MedicationInventoryRow row })
                return;

            var popup = new ActionPopup();
            popup.ConfigureForm(
                title: $"Create Order — {row.MedName}",
                message: null,
                primaryText: "Create",
                field1: ("Quantity", "e.g. 30", Keyboard.Numeric),
                field2: ("Notes (optional)", "e.g., urgent / pharmacy call", Keyboard.Default),
                validator: (qtyText, notes) =>
                {
                    if (string.IsNullOrWhiteSpace(qtyText))
                        return (false, "Quantity is required.");

                    if (!int.TryParse(qtyText, out var qty) || qty <= 0)
                        return (false, "Please enter a positive number.");

                    return (true, null);
                });

            var result = await this.ShowPopupAsync(popup);

            if (result is not ActionPopup.PopupResult r || string.IsNullOrWhiteSpace(r.Field1))
                return;

            var qty = int.Parse(r.Field1);

            VM.CreateOrderCommand.Execute(
                Tuple.Create(row.Med, qty, r.Field2));

            await DisplayAlert("Created", "Order created (Status: Requested).", "OK");
        }

        private async void OnOrdersClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(MedicationOrdersPage));
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
