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

        builder.Services.AddSingleton<ResidentReadOnlyJsonService>();
        builder.Services.AddSingleton<MedicationReadOnlyJsonService>();

        builder.Services.AddTransient<ResidentsViewModel>();
        builder.Services.AddTransient<ResidentDetailViewModel>();

        builder.Services.AddTransient<ResidentsPage>();
        builder.Services.AddTransient<ResidentDetailPage>();

        return builder.Build();
    }
}
