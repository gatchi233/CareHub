using CareHub.Mobile.Shell;

namespace CareHub.Mobile
{
    public partial class App : Application
    {
        public App(MobileAppShell shell)
        {
            InitializeComponent();
            MainPage = shell;
        }
    }
}
