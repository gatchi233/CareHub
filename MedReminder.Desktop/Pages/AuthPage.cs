using MedReminder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MedReminder.Pages;

public class AuthPage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();

        var auth = MauiProgram.Services.GetService<AuthService>();

        if (auth == null || !auth.IsLoggedIn)
        {
            Shell.Current.GoToAsync("//LoginPage");
        }
    }
}