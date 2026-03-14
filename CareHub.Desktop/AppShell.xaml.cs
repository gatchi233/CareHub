using CareHub.Desktop.Services;
using CareHub.Desktop.Services.Sync;
using CareHub.Models;
using CareHub.Pages;
using CareHub.Pages.Desktop;
using CareHub.Services;
using CareHub.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System.Linq;

namespace CareHub
{
    public partial class AppShell : Shell
    {
        private readonly AuthService _auth;
        public AppShell()
        {
            InitializeComponent();
         
            _auth = MauiProgram.Services.GetService<AuthService>()?? throw new InvalidOperationException("AuthService not found.");


        // Routes – only register sub-pages that are NOT declared as ShellContent in XAML
        Routing.RegisterRoute(nameof(EditMedicationPage), typeof(EditMedicationPage));
        Routing.RegisterRoute(nameof(EditResidentPage), typeof(EditResidentPage));
        Routing.RegisterRoute(nameof(MedicationBatchesPage), typeof(MedicationBatchesPage));
        Routing.RegisterRoute(nameof(MedicationOrdersPage), typeof(MedicationOrdersPage));
        Routing.RegisterRoute(nameof(ResidentMedicationsPage), typeof(ResidentMedicationsPage));
        Routing.RegisterRoute(nameof(ResidentObservationsPage), typeof(ResidentObservationsPage));
        Routing.RegisterRoute(nameof(ResidentReportPage), typeof(ResidentReportPage));
        Routing.RegisterRoute(nameof(ViewResidentPage), typeof(ViewResidentPage));
        Routing.RegisterRoute(nameof(MarPage), typeof(MarPage));
        }

        private bool _handlingUnsaved;

        protected override async void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            var auth = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<AuthService>();

            if (auth == null)
                return;

            // Allow navigation to login page when not logged in
            var target = args.Target?.Location?.OriginalString ?? string.Empty;

            if (!auth.IsLoggedIn && !target.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                args.Cancel();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("//login");
                });
                return;
            }

            // Role-based navigation guards
            if (auth.IsLoggedIn)
            {
                // Admin cannot access MAR or Observations (clinical tasks)
                if (auth.HasRole(StaffRole.Admin))
                {
                    if (target.Contains("MarPage", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("ResidentObservationsPage", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel();
                        return;
                    }
                }

                // CareStaff cannot access Medications, MAR, Reports, or Staff Management
                if (auth.HasRole(StaffRole.CareStaff))
                {
                    if (target.Contains("MarPage", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("MedicationInventory", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("EditMedication", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("MedicationBatches", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("MedicationOrders", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("ResidentMedications", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("ResidentReport", StringComparison.OrdinalIgnoreCase) ||
                        target.Contains("StaffManagement", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel();
                        return;
                    }
                }
            }

            // Check for unsaved changes when switching tabs
            if (args.Source == ShellNavigationSource.ShellSectionChanged && !_handlingUnsaved)
            {
                var currentPage = Shell.Current?.CurrentPage
                    ?? Shell.Current?.Navigation?.NavigationStack?.LastOrDefault();

                if (currentPage is IUnsavedChangesPage unsaved && unsaved.HasUnsavedChanges)
                {
                    var deferral = args.GetDeferral();

                    var action = await DisplayActionSheet(
                        "You have unsaved changes",
                        "Cancel",
                        null,
                        "Save and leave",
                        "Discard changes");

                    if (action == "Save and leave")
                    {
                        await unsaved.SaveAsync();
                        deferral.Complete();
                    }
                    else if (action == "Discard changes")
                    {
                        deferral.Complete();
                    }
                    else
                    {
                        // Cancel — stay on current page
                        deferral.Complete();
                        args.Cancel();
                    }
                }
            }
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);

            // When switching to a root tab, clear all pushed sub-pages
            if (args.Source == ShellNavigationSource.ShellSectionChanged)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _handlingUnsaved = true;
                    try
                    {
                        var nav = Shell.Current?.Navigation;
                        if (nav == null) return;

                        // Pop all pushed pages back to root
                        while (nav.NavigationStack.Count > 1)
                        {
                            await Shell.Current.GoToAsync("..");
                        }
                    }
                    finally
                    {
                        _handlingUnsaved = false;
                    }
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyRbac();
        }

        private void ApplyRbac()
        {
            if (!_auth.IsLoggedIn)
            {
                if (StaffManagementItem != null)
                    StaffManagementItem.IsVisible = false;
                if (MedicationInventoryItem != null)
                    MedicationInventoryItem.IsVisible = false;
                return;
            }

            // Staff Management: Admin only
            if (StaffManagementItem != null)
                StaffManagementItem.IsVisible = _auth.HasRole(StaffRole.Admin);

            // Inventory / Medications: Admin and Nurse only (not CareStaff)
            if (MedicationInventoryItem != null)
                MedicationInventoryItem.IsVisible = _auth.HasRole(StaffRole.Admin, StaffRole.Nurse);
        }

        public async Task LogoutAsync()
        {
            _auth.Logout();

            ApplyRbac();

            await GoToAsync("//LoginPage");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await LogoutAsync();
        }

        private async void OnSyncClicked(object sender, EventArgs e)
        {
            try
            {
                using var http= new HttpClient {BaseAddress = new Uri("http://localhost:5001/") };
                http.Timeout = TimeSpan.FromSeconds(8);

                var health = await http.GetAsync("health");
                if (!health.IsSuccessStatusCode)
                {
                    await DisplayAlert("Offline", "API not reachable (health check failed).", "OK");
                    return;
                }

                int total = 0;

                var residentSvc = MauiProgram.Services.GetService<IResidentService>() as ResidentService;
                if (residentSvc != null)
                    total += await residentSvc.SyncAsync();

                var medSvc = MauiProgram.Services.GetService<IMedicationService>() as MedicationService;
                if (medSvc != null)
                    total += await medSvc.SyncAsync();

                var obsSvc = MauiProgram.Services.GetService<IObservationService>() as ObservationService;
                if (obsSvc != null)
                    total += await obsSvc.SyncAsync();

                var marSvc = MauiProgram.Services.GetService<IMarService>() as MarService;
                if (marSvc != null)
                    total += await marSvc.SyncAsync();

                var message = total > 0
                    ? $"Synced {total} record(s)"
                    : "No pending records to sync";

                await DisplayAlert("Sync", message, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Sync Error", ex.Message, "OK");
            }
        }
    }
}
