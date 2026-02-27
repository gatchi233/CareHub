using MedReminder.Pages;

namespace MedReminder.Pages.Desktop
{
    public partial class HelpPage : AuthPage
    {
        public HelpPage()
        {
            InitializeComponent();
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
