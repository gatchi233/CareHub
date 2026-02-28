using Microsoft.UI.Xaml;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MedReminder.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            try
            {
                var currentWindow = Microsoft.Maui.Controls.Application.Current?.Windows?[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (currentWindow == null) return;

                IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(currentWindow);
                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);

                if (displayArea != null)
                {
                    var screenWidth = displayArea.WorkArea.Width;
                    var screenHeight = displayArea.WorkArea.Height;

                    int newWidth = (int)(screenWidth * 0.80);
                    int newHeight = (int)(screenHeight * 0.80);

                    int newX = (screenWidth - newWidth) / 2;
                    int newY = (screenHeight - newHeight) / 2;

                    appWindow.MoveAndResize(new RectInt32(newX, newY, newWidth, newHeight));
                }
            }
            catch
            {
                // Window positioning failed — app will still show with default size
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
