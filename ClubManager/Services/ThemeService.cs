using System;
using System.Windows;
using System.Windows.Media;

namespace ClubManager.Services
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    public class ThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Light;

        public void SetTheme(ThemeMode theme)
        {
            CurrentTheme = theme;
            ApplyTheme();
        }

        public void ToggleTheme()
        {
            SetTheme(CurrentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light);
        }

        private void ApplyTheme()
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                if (CurrentTheme == ThemeMode.Light)
                {
                    ApplyLightTheme(app.Resources);
                }
                else
                {
                    ApplyDarkTheme(app.Resources);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando tema: {ex.Message}");
            }
        }

        private void ApplyLightTheme(ResourceDictionary resources)
        {
            // Tema Claro
            resources["BackgroundColor"] = Color.FromRgb(248, 249, 250);
            resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            resources["ForegroundColor"] = Color.FromRgb(33, 37, 41);
            resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(33, 37, 41));

            resources["SecondaryBackgroundColor"] = Color.FromRgb(255, 255, 255);
            resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            resources["BorderColor"] = Color.FromRgb(222, 226, 230);
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(222, 226, 230));

            resources["AccentColor"] = Color.FromRgb(0, 122, 204);
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
        }

        private void ApplyDarkTheme(ResourceDictionary resources)
        {
            // Tema Oscuro
            resources["BackgroundColor"] = Color.FromRgb(45, 45, 48);
            resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            resources["ForegroundColor"] = Color.FromRgb(255, 255, 255);
            resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            resources["SecondaryBackgroundColor"] = Color.FromRgb(30, 30, 30);
            resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            resources["BorderColor"] = Color.FromRgb(63, 63, 70);
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));

            resources["AccentColor"] = Color.FromRgb(0, 122, 204);
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
        }
    }
}