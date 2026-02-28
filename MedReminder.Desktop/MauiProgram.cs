using CommunityToolkit.Maui;
using MedReminder.Desktop.Services;
using MedReminder.Desktop.Services.Sync;
using MedReminder.Pages;
using MedReminder.Pages.Desktop;
using MedReminder.Services;
using MedReminder.Services.Abstractions;
using MedReminder.Services.Local;
using MedReminder.Services.Remote;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

#if WINDOWS
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;
using static System.Net.WebRequestMethods;
#endif

namespace MedReminder
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        const int width = 1200;
                        const int height = 800;

                        window.ExtendsContentIntoTitleBar = false;

                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

                        appWindow.SetPresenter(
                            appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter
                            ?? Microsoft.UI.Windowing.OverlappedPresenter.Create());

                        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                        {
                            presenter.IsResizable = true;
                            presenter.IsMaximizable = true;
                            presenter.IsMinimizable = true;
                        }
                    });
                });
#endif
            });

            // One constant BaseUrl
            var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5001/";
            var apiBase = new Uri(apiBaseUrl);

            // Sync queue (registered early so all services can use it)
            builder.Services.AddSingleton<ISyncQueue, JsonSyncQueue>();

            // HTTP clients for API services
            builder.Services.AddHttpClient<ResidentApiService>(client =>
            {
                client.BaseAddress = apiBase;
                client.Timeout = TimeSpan.FromSeconds(2);
            });
            builder.Services.AddHttpClient<MedicationApiService>(client =>
            {
                client.BaseAddress = apiBase;
                client.Timeout = TimeSpan.FromSeconds(2);
            });
            builder.Services.AddHttpClient<ObservationApiService>(client =>
            {
                client.BaseAddress = apiBase;
                client.Timeout = TimeSpan.FromSeconds(2);
            });

            // Local JSON services
            builder.Services.AddSingleton<ResidentJsonService>();
            builder.Services.AddSingleton<MedicationJsonService>();
            builder.Services.AddSingleton<ObservationJsonService>();

            // Medications: API + Local + Wrapper
            builder.Services.AddSingleton<IMedicationService>(sp =>
            {
                var api = sp.GetRequiredService<MedicationApiService>();
                var local = sp.GetRequiredService<MedicationJsonService>();
                var queue = sp.GetRequiredService<ISyncQueue>();
                return new MedReminder.Desktop.Services.MedicationService(api, local, queue);
            });

            // Observations: API + Local + Wrapper
            builder.Services.AddSingleton<IObservationService>(sp =>
            {
                var api = sp.GetRequiredService<ObservationApiService>();
                var local = sp.GetRequiredService<ObservationJsonService>();
                var queue = sp.GetRequiredService<ISyncQueue>();
                return new ObservationService(api, local, queue);
            });

            // Residents: API + Local + Wrapper (registered after Meds/Obs so it can remap their IDs)
            builder.Services.AddSingleton<IResidentService>(sp =>
            {
                var api = sp.GetRequiredService<ResidentApiService>();
                var local = sp.GetRequiredService<ResidentJsonService>();
                var localMeds = sp.GetRequiredService<MedicationJsonService>();
                var localObs = sp.GetRequiredService<ObservationJsonService>();
                var queue = sp.GetRequiredService<ISyncQueue>();
                return new MedReminder.Desktop.Services.ResidentService(api, local, localMeds, localObs, queue);
            });

            builder.Services.AddSingleton<IMedicationOrderService, MedicationOrderJsonService>();
            builder.Services.AddSingleton<IStaffService, StaffJsonService>();
            builder.Services.AddSingleton<AuthService>();

            // ViewModels
            builder.Services.AddTransient<FloorPlanViewModel>();
            builder.Services.AddTransient<HomePageViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MedicationInventoryViewModel>();
            builder.Services.AddTransient<MedicationOrdersViewModel>();
            builder.Services.AddTransient<MedicationViewModel>();
            builder.Services.AddTransient<ResidentReportViewModel>();
            builder.Services.AddTransient<ResidentsPageViewModel>();
            builder.Services.AddTransient<StaffManagementViewModel>();
            builder.Services.AddTransient<ResidentObservationsViewModel>();

            // Pages
            builder.Services.AddTransient<EditMedicationPage>();
            builder.Services.AddTransient<EditResidentPage>();
            builder.Services.AddTransient<FloorPlanPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MedicationInventoryPage>();
            builder.Services.AddTransient<MedicationOrdersPage>();
            builder.Services.AddTransient<ResidentMedicationsPage>();
            builder.Services.AddTransient<ResidentReportPage>();
            builder.Services.AddTransient<ResidentsPage>();
            builder.Services.AddTransient<ViewResidentPage>();
            builder.Services.AddTransient<StaffManagementPage>();
            builder.Services.AddTransient<ResidentObservationsPage>();
            builder.Services.AddTransient<LoginPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            var app = builder.Build();
            Services = app.Services;

            // Load persisted connectivity state (sync-safe, no deadlock)
            ConnectivityHelper.InitializeSync();

            // Probe API reachability in the background (non-blocking)
            _ = ConnectivityHelper.ProbeApiInBackgroundAsync(apiBase);

            return app;
        }
    }
}
