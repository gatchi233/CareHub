using CareHub.Pages.Desktop;
using CareHub.Services;
using CareHub.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CareHub.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<LoginViewModel>();
            BindingContext = vm;
        }

        public LoginPage(LoginViewModel vm) : this()
        {
            BindingContext = vm;
        }

        protected override bool OnBackButtonPressed()
        {
            // Disable back navigation on Login page
            return true;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var auth = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<AuthService>();

            if (auth?.IsLoggedIn == true)
            {
                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
        }

        private void OnPasswordCompleted(object sender, EventArgs e)
        {
            // Invoke the ViewModel's LoginCommand
            if (BindingContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
            {
                vm.LoginCommand.Execute(null);
            }
        }

    }
}
