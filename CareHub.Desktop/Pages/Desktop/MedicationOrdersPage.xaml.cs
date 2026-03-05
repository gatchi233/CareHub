using CommunityToolkit.Maui.Views;
using CareHub.Models;
using CareHub.Pages.UI.Popups;
using CareHub.Services;
using CareHub.Services.Abstractions;
using CareHub.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CareHub.Pages.Desktop
{
    [QueryProperty(nameof(ReturnTo), "returnTo")]
    public partial class MedicationOrdersPage : AuthPage
    {
        private MedicationOrdersViewModel VM => (MedicationOrdersViewModel)BindingContext;
        public string? ReturnTo { get; set; }

        public MedicationOrdersPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            BindingContext = services.GetRequiredService<MedicationOrdersViewModel>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await VM.LoadAsync();
        }

        private async void OnNewOrderClicked(object sender, TappedEventArgs e)
        {
            await VM.LoadAsync();

            var popup = new ActionPopup();
            popup.ConfigureNewMedOrder("NEW ORDER");

            var result = await this.ShowPopupAsync(popup) as ActionPopup.PopupResult;
            if (result?.Field1 == null) return;

            var name = result.Field1;
            var unit = result.Field2;
            var indication = result.Field3;
            var qtyText = result.Field4;
            var reorderText = result.Field5;
            var notes = result.Field6;

            int qty = 0;
            if (!string.IsNullOrWhiteSpace(qtyText))
                int.TryParse(qtyText, out qty);

            int reorderLevel = 0;
            if (!string.IsNullOrWhiteSpace(reorderText))
                int.TryParse(reorderText, out reorderLevel);

            Guid medicationId;

            // Check for existing medication with same name
            var existing = VM.InventoryMedications
                .FirstOrDefault(m => string.Equals(
                    (m.MedName ?? "").Trim(), name.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                var useExisting = await DisplayAlert(
                    "Duplicate",
                    $"'{name}' already exists in inventory. Use existing item?",
                    "Yes", "No");

                if (!useExisting) return;

                medicationId = existing.Id;
            }
            else
            {
                medicationId = await VM.CreateInventoryMedicationAsync(
                    name, reorderLevel, unit, indication);
            }

            var auth = MauiProgram.Services.GetService<AuthService>();
            var user = auth?.CurrentUser != null
                ? $"{auth.CurrentUser.StaffName} ({auth.CurrentUser.Role})"
                : "Unknown";
            await VM.CreateOrderAsync(medicationId, qty, user, notes);
        }

        private async void OnReceiveClicked(object sender, EventArgs e)
        {
            MedicationOrderRow? row = null;
            if (e is TappedEventArgs te && te.Parameter is MedicationOrderRow r1)
                row = r1;
            else if ((sender as BindableObject)?.BindingContext is MedicationOrderRow r2)
                row = r2;
            if (row == null) return;

            var expiryText = await DisplayPromptAsync(
                "Expiry Date",
                "Enter the expiry date for the received stock (YYYY-MM-DD):",
                accept: "OK",
                cancel: "Skip",
                placeholder: "e.g. 2026-12-31");

            DateTimeOffset? expiryDate = null;
            if (!string.IsNullOrWhiteSpace(expiryText) &&
                DateTime.TryParse(expiryText, out var parsed))
            {
                expiryDate = new DateTimeOffset(parsed.Date, TimeSpan.Zero);
            }

            try
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;
                var orderService = services?.GetRequiredService<IMedicationOrderService>();
                if (orderService != null)
                {
                    await orderService.UpdateStatusAsync(row.OrderId, MedicationOrderStatus.Received, expiryDate);
                    await VM.LoadAsync();
                }
            }
            catch { /* local-only service */ }
        }

        private async void OnCloseClicked(object sender, TappedEventArgs e) => await NavigateOutAsync();

        private async Task NavigateOutAsync()
        {
            if (!string.IsNullOrWhiteSpace(ReturnTo))
            {
                await Shell.Current.GoToAsync(ReturnTo);
                return;
            }

            if (Shell.Current?.Navigation?.ModalStack?.Count > 0)
            {
                await Shell.Current.Navigation.PopModalAsync();
                return;
            }

            if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync();
                return;
            }

            await Shell.Current.GoToAsync(nameof(MedicationInventoryPage));
        }

    }
}
