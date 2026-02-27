using CommunityToolkit.Maui.Views;
using MedReminder.Pages.UI.Popups;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MedReminder.Pages.Desktop
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

            // Step 1 — pick order type
            var typePopup = new ActionPopup();
            typePopup.ConfigureList("NEW ORDER", new[]
            {
                ("existing", "Order Existing (Inventory)"),
                ("new",      "Order New Medication")
            });

            var typeResult = await this.ShowPopupAsync(typePopup) as ActionPopup.PopupResult;
            if (typeResult?.SelectedKey == null) return;

            if (typeResult.SelectedKey == "existing")
            {
                if (!VM.InventoryMedications.Any())
                {
                    await DisplayAlert("Inventory Empty", "No inventory medications available to order.", "OK");
                    return;
                }

                // Step 2a — pick from inventory
                var medItems = VM.InventoryMedications
                    .Where(m => !string.IsNullOrWhiteSpace(m.MedName))
                    .Select(m => (m.Id.ToString(), m.MedName!));

                var medPopup = new ActionPopup();
                medPopup.ConfigureList("SELECT MEDICATION", medItems);

                var medResult = await this.ShowPopupAsync(medPopup) as ActionPopup.PopupResult;
                if (medResult?.SelectedKey == null) return;

                var picked = VM.InventoryMedications.FirstOrDefault(m => m.Id.ToString() == medResult.SelectedKey);
                if (picked == null) return;

                await CreateOrderForMedicationAsync(picked.Id, picked.MedName ?? "");
            }
            else
            {
                // Step 2b — create new medication
                var newMedPopup = new ActionPopup();
                newMedPopup.ConfigureForm(
                    title: "NEW MEDICATION",
                    message: null,
                    primaryText: "NEXT",
                    field1: ("Medication Name", "e.g., Amoxicillin 500mg", Keyboard.Default),
                    field2: ("Reorder Level (optional)", "e.g., 20", Keyboard.Numeric),
                    validator: (name, level) =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return (false, "Medication name is required.");
                        if (!string.IsNullOrWhiteSpace(level) && (!int.TryParse(level, out var l) || l < 0))
                            return (false, "Reorder level must be a positive number.");
                        return (true, null);
                    });

                var newMedResult = await this.ShowPopupAsync(newMedPopup) as ActionPopup.PopupResult;
                if (newMedResult?.Field1 == null) return;

                int reorderLevel = 0;
                if (!string.IsNullOrWhiteSpace(newMedResult.Field2))
                    int.TryParse(newMedResult.Field2, out reorderLevel);

                var createdId = await VM.CreateInventoryMedicationAsync(newMedResult.Field1, reorderLevel);
                await CreateOrderForMedicationAsync(createdId, newMedResult.Field1);
            }
        }

        private async Task CreateOrderForMedicationAsync(Guid medicationId, string medName)
        {
            var orderPopup = new ActionPopup();
            orderPopup.ConfigureForm(
                title: "CREATE ORDER",
                message: $"Medication: {medName}",
                primaryText: "CREATE",
                field1: ("Quantity", "e.g., 30", Keyboard.Numeric),
                field2: ("Notes (optional)", "e.g., urgent / call pharmacy", Keyboard.Default),
                validator: (qty, _) =>
                {
                    if (string.IsNullOrWhiteSpace(qty))
                        return (false, "Quantity is required.");
                    if (!int.TryParse(qty, out var q) || q <= 0)
                        return (false, "Please enter a positive number.");
                    return (true, null);
                });

            var result = await this.ShowPopupAsync(orderPopup) as ActionPopup.PopupResult;
            if (result?.Field1 == null) return;

            int.TryParse(result.Field1, out var quantity);
            await VM.CreateOrderAsync(
                medicationId,
                quantity,
                "Staff",
                string.IsNullOrWhiteSpace(result.Field2) ? null : result.Field2);
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

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
