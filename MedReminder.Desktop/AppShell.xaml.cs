using MedReminder.Models;
using MedReminder.Pages;
using MedReminder.Pages.Desktop;
using MedReminder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MedReminder
{
    public partial class AppShell : Shell
    {
        private readonly AuthService _auth;
        public AppShell()
        {
            InitializeComponent();
         
            _auth = MauiProgram.Services.GetService<AuthService>()?? throw new InvalidOperationException("AuthService not found.");


        // Routes
        Routing.RegisterRoute(nameof(EditMedicationPage), typeof(EditMedicationPage));
        Routing.RegisterRoute(nameof(EditResidentPage), typeof(EditResidentPage));
        Routing.RegisterRoute(nameof(FloorPlanPage), typeof(FloorPlanPage));
        Routing.RegisterRoute(nameof(MedicationInventoryPage), typeof(MedicationInventoryPage));
        Routing.RegisterRoute(nameof(MedicationOrdersPage), typeof(MedicationOrdersPage));
        Routing.RegisterRoute(nameof(ResidentMedicationsPage), typeof(ResidentMedicationsPage));
        Routing.RegisterRoute(nameof(ResidentObservationsPage), typeof(ResidentObservationsPage));
        Routing.RegisterRoute(nameof(ResidentReportPage), typeof(ResidentReportPage));
        Routing.RegisterRoute(nameof(ResidentsPage), typeof(ResidentsPage));
        Routing.RegisterRoute(nameof(ViewResidentPage), typeof(ViewResidentPage));
        Routing.RegisterRoute(nameof(StaffManagementPage), typeof(StaffManagementPage));
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));

        // Login is not under Desktop namespace
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
    }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
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
                // Hide everything except login if needed
                if (StaffManagementItem != null)
                    StaffManagementItem.IsVisible = false;

                return;
            }

            // Admin-only
            if (StaffManagementItem != null)
            {
                StaffManagementItem.IsVisible =
                    _auth.HasRole(StaffRole.Admin);
            }

            // You can expand later:
            // InventoryItem.IsVisible = _auth.HasRole(StaffRole.Admin, StaffRole.Nurse);
        }

        public async Task LogoutAsync()
        {
            _auth.Logout();

            ApplyRbac();

            // go back to login route
            await GoToAsync("//LoginPage");
        }
    }
}
