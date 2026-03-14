using CareHub.Mobile.Pages;
using CareHub.Mobile.Services;
using CareHub.Mobile.Shell;
using CareHub.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace CareHub.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        // DI
        builder.Services.AddSingleton<MobileAppShell>();

        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5001/";
        var apiBase = new Uri(apiBaseUrl);

        // Auth service (plain HttpClient, no token handler needed for login itself)
        builder.Services.AddHttpClient<MobileAuthService>(client =>
        {
            client.BaseAddress = apiBase;
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // DelegatingHandler that attaches JWT Bearer token
        builder.Services.AddTransient<MobileAuthTokenHandler>();

        // API services with auth token handler
        builder.Services.AddHttpClient<ResidentReadOnlyJsonService>(client =>
        {
            client.BaseAddress = apiBase;
            client.Timeout = TimeSpan.FromSeconds(5);
        }).AddHttpMessageHandler<MobileAuthTokenHandler>();

        builder.Services.AddHttpClient<MedicationReadOnlyJsonService>(client =>
        {
            client.BaseAddress = apiBase;
            client.Timeout = TimeSpan.FromSeconds(5);
        }).AddHttpMessageHandler<MobileAuthTokenHandler>();

        builder.Services.AddTransient<ResidentsViewModel>();
        builder.Services.AddTransient<ResidentDetailViewModel>();

        builder.Services.AddTransient<ResidentsPage>();
        builder.Services.AddTransient<ResidentDetailPage>();

        return builder.Build();
    }
}
