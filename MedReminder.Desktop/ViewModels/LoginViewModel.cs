using MedReminder.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace MedReminder.ViewModels
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
            var success = _auth.Login(Username, Password);

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