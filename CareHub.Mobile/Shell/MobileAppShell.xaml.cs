namespace CareHub.Mobile.Shell;

public partial class MobileAppShell : Microsoft.Maui.Controls.Shell
{
    public MobileAppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Pages.ResidentDetailPage), typeof(Pages.ResidentDetailPage));
    }
}
