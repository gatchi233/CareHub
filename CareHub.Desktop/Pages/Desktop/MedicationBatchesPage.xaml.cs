using CommunityToolkit.Maui.Views;
using CareHub.Models;
using CareHub.Pages.UI.Popups;
using CareHub.Services;
using CareHub.Services.Abstractions;
using CareHub.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CareHub.Pages.Desktop
{
    [QueryProperty(nameof(MedName), "medName")]
    public partial class MedicationBatchesPage : AuthPage
    {
        private MedicationBatchesViewModel VM => (MedicationBatchesViewModel)BindingContext;

        private string? _medName;
        public string? MedName
        {
            get => _medName;
            set
            {
                _medName = value;
                if (!string.IsNullOrWhiteSpace(value))
                    _ = VM.LoadAsync(Uri.UnescapeDataString(value));
            }
        }

        public MedicationBatchesPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            var vm = services.GetRequiredService<MedicationBatchesViewModel>();

            vm.OnAdjustRequested = async batch =>
            {
                var popup = new ActionPopup();
                popup.ConfigureAdjust(
                    title: "ADJUST STOCK",
                    medName: $"{batch.MedName} (Batch)",
                    currentStock: batch.StockQuantity,
                    reorderLevel: batch.ReorderLevel);

                var result = await this.ShowPopupAsync(popup);

                if (result is ActionPopup.PopupResult r && r.AdjustDelta is not null && r.AdjustDelta.Value != 0)
                {
                    await vm.ApplyAdjustAsync(r.AdjustDelta.Value);
                }
            };

            vm.OnReturnRequested = async batch =>
            {
                var confirm = await DisplayAlert(
                    "Return to Vendor",
                    $"Return {batch.StockQuantity} {batch.QuantityUnit}(s) of {batch.MedName}? Stock will be set to 0.",
                    "Return", "Cancel");

                if (confirm)
                {
                    await vm.ConfirmReturnAsync(batch);
                }
            };

            BindingContext = vm;
        }

        private async void OnOrderClicked(object sender, EventArgs e)
        {
            var popup = new ActionPopup();
            popup.ConfigureOrder(
                title: "CREATE ORDER",
                medName: VM.MedName ?? "Unknown",
                currentStock: VM.TotalStock,
                reorderLevel: VM.ReorderLevel);

            var result = await this.ShowPopupAsync(popup);

            if (result is not ActionPopup.PopupResult r || string.IsNullOrWhiteSpace(r.Field1))
                return;

            var qty = int.Parse(r.Field1);

            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null) return;

            var orderService = services.GetRequiredService<IMedicationOrderService>();
            var auth = services.GetService<AuthService>();
            var user = auth?.CurrentUser != null
                ? $"{auth.CurrentUser.StaffName} ({auth.CurrentUser.Role})"
                : "Unknown";

            // Use the first batch's Med to get the medication ID
            var med = VM.Batches.FirstOrDefault()?.Med;
            if (med == null) return;

            await orderService.CreateAsync(med.Id, qty, user, r.Field2);
            await DisplayAlert("Created", "Order created (Status: Requested).", "OK");
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
