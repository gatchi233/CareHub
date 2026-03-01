using Microsoft.Maui.Graphics;

namespace CareHub.Resources.Styles
{
    /// Tiny helper so ViewModels can read XAML ResourceDictionary colors without duplicating hex strings in C#.
    /// 
    public static class AppColors
    {
        public static Color Get(string key)
        {
            var app = Application.Current
                ?? throw new InvalidOperationException("Application.Current is null.");

            if (!app.Resources.TryGetValue(key, out var value) || value is not Color color)
                throw new KeyNotFoundException($"Color resource '{key}' not found (or not a Color).");

            return color;
        }
    }
}
