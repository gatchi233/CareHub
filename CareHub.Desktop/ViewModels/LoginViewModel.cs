using CareHub.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace CareHub.ViewModels
{
    public class LoginViewModel
    {
        private readonly AuthService _auth;

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public Command LoginCommand { get; }

        public LoginViewModel(AuthService auth)
        {
            _auth = auth;
            LoginCommand = new Command(async () => await OnLoginAsync());
        }

        private async Task OnLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter username and password.", "OK");
                return;
            }

            // Try API login first, falls back to local if offline
            var success = await _auth.LoginAsync(Username, Password);

            if (!success)
            {
                await Shell.Current.DisplayAlert("Error", "Invalid username or password.", "OK");
                return;
            }

            Application.Current.MainPage = new AppShell();

            // Small delay to ensure MainPage is set before navigation
            await Task.Delay(50);

            // Navigate to Home page after login
            await Shell.Current.GoToAsync("//HomePage");
        }
    }
}
