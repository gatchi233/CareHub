using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MedReminder.Pages.Desktop
{
    public partial class MedicationOrdersPage : AuthPage
    {
        private MedicationOrdersViewModel VM => (MedicationOrdersViewModel)BindingContext;

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

        // Hook this up in XAML: <Button Text="New Order" Clicked="OnNewOrderClicked" />
        private async void OnNewOrderClicked(object sender, EventArgs e)
        {
            // Ensure latest inventory list
            await VM.LoadAsync();

            var choice = await DisplayActionSheet(
                "New Order",
                "Cancel",
                null,
                "Order Existing (Inventory)",
                "Order New Medication (Not in Inventory)");

            if (choice == null || choice == "Cancel")
                return;

            if (choice == "Order Existing (Inventory)")
            {
                if (!VM.InventoryMedications.Any())
                {
                    await DisplayAlert("Inventory Empty", "No inventory medications available to order.", "OK");
                    return;
                }

                var labels = VM.InventoryMedications
                    .Select(m => m.MedName)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                var pickedName = await DisplayActionSheet(
                    "Select medication to order",
                    "Cancel",
                    null,
                    labels);

                if (pickedName == null || pickedName == "Cancel")
                    return;

                var picked = VM.InventoryMedications.FirstOrDefault(m => m.MedName == pickedName);
                if (picked == null)
                    return;

                await CreateOrderForMedicationAsync(picked.Id, picked.MedName);
                return;
            }

            // Order New Medication (not in inventory)
            var newName = await DisplayPromptAsync(
                "New Medication",
                "Enter medication name (e.g., Amoxicillin 500mg):",
                accept: "Next",
                cancel: "Cancel",
                placeholder: "Medication name");

            if (string.IsNullOrWhiteSpace(newName))
                return;

            newName = newName.Trim();

            // Optional: reorder level
            var reorderText = await DisplayPromptAsync(
                "Reorder Level (optional)",
                "Enter reorder level (number) or Skip:",
                accept: "Next",
                cancel: "Skip",
                placeholder: "e.g., 20",
                keyboard: Keyboard.Numeric);

            int reorderLevel = 0;
            if (!string.IsNullOrWhiteSpace(reorderText))
            {
                if (!int.TryParse(reorderText, out reorderLevel) || reorderLevel < 0)
                {
                    await DisplayAlert("Invalid", "Reorder level must be 0 or a positive number.", "OK");
                    return;
                }
            }

            // Create global inventory medication item, then create order
            var createdId = await VM.CreateInventoryMedicationAsync(
                medName: newName,
                reorderLevel: reorderLevel);

            await CreateOrderForMedicationAsync(createdId, newName);
        }

        private async Task CreateOrderForMedicationAsync(Guid medicationId, string medName)
        {
            var qtyText = await DisplayPromptAsync(
                "Quantity",
                $"Enter quantity to order for {medName}:",
                accept: "Next",
                cancel: "Cancel",
                placeholder: "e.g., 30",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyText))
                return;

            if (!int.TryParse(qtyText, out var qty) || qty <= 0)
            {
                await DisplayAlert("Invalid", "Please enter a positive number.", "OK");
                return;
            }

            var notes = await DisplayPromptAsync(
                "Notes (optional)",
                "Add notes for this order (optional):",
                accept: "Create",
                cancel: "Skip",
                placeholder: "e.g., urgent / pharmacy call");

            // TODO: later replace "Staff" with real logged-in user name + role checks
            await VM.CreateOrderAsync(
                medicationId,
                qty,
                "Staff",
                string.IsNullOrWhiteSpace(notes) ? null : notes);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await NavigateOutAsync();
        }

        private static async Task NavigateOutAsync()
        {
            // modal first
            if (Shell.Current?.Navigation?.ModalStack?.Count > 0)
            {
                await Shell.Current.Navigation.PopModalAsync();
                return;
            }

            // normal stack
            if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync();
                return;
            }

            // shell root fallback (previous logical place)
            await Shell.Current.GoToAsync(nameof(MedicationInventoryPage));
        }


    }
}
