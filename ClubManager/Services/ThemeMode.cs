using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ClubManager.Services
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    public interface IThemeManager
    {
        ThemeMode CurrentTheme { get; }
        void SetTheme(ThemeMode theme);
        void ToggleTheme();
        event EventHandler<ThemeMode> ThemeChanged;
    }

    public class ThemeManager : IThemeManager, INotifyPropertyChanged
    {
        private ThemeMode _currentTheme = ThemeMode.Light;
        private readonly string _configPath;

        public event EventHandler<ThemeMode>? ThemeChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ThemeManager()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme_config.json");
            LoadThemeFromConfig();
            ApplyTheme();
        }

        public ThemeMode CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnPropertyChanged();
                    ThemeChanged?.Invoke(this, value);
                }
            }
        }

        public void SetTheme(ThemeMode theme)
        {
            CurrentTheme = theme;
            ApplyTheme();
            SaveThemeToConfig();
        }

        public void ToggleTheme()
        {
            SetTheme(CurrentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light);
        }

        private void ApplyTheme()
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            // Limpiar recursos de tema anteriores
            var keysToRemove = new[]
            {
                "BackgroundColor", "BackgroundBrush",
                "ForegroundColor", "ForegroundBrush",
                "SecondaryBackgroundColor", "SecondaryBackgroundBrush",
                "BorderColor", "BorderBrush",
                "AccentColor", "AccentBrush",
                "TextBoxBackgroundColor", "TextBoxBackgroundBrush",
                "TextBoxForegroundColor", "TextBoxForegroundBrush",
                "ButtonBackgroundColor", "ButtonBackgroundBrush",
                "HeaderBackgroundColor", "HeaderBackgroundBrush",
                "SidebarBackgroundColor", "SidebarBackgroundBrush",
                "SuccessColor", "SuccessBrush",
                "WarningColor", "WarningBrush",
                "DangerColor", "DangerBrush",
                "InfoColor", "InfoBrush",
                "MutedColor", "MutedBrush"
            };

            foreach (var key in keysToRemove)
            {
                if (app.Resources.Contains(key))
                    app.Resources.Remove(key);
            }

            // Aplicar tema
            if (CurrentTheme == ThemeMode.Light)
            {
                ApplyLightTheme(app);
            }
            else
            {
                ApplyDarkTheme(app);
            }
        }

        private void ApplyLightTheme(Application app)
        {
            // Tema Claro
            var resources = app.Resources;

            // Colores principales
            resources["BackgroundColor"] = Color.FromRgb(248, 249, 250);      // #F8F9FA
            resources["BackgroundBrush"] = new SolidColorBrush((Color)resources["BackgroundColor"]);

            resources["ForegroundColor"] = Color.FromRgb(33, 37, 41);         // #212529
            resources["ForegroundBrush"] = new SolidColorBrush((Color)resources["ForegroundColor"]);

            resources["SecondaryBackgroundColor"] = Color.FromRgb(255, 255, 255); // #FFFFFF
            resources["SecondaryBackgroundBrush"] = new SolidColorBrush((Color)resources["SecondaryBackgroundColor"]);

            resources["BorderColor"] = Color.FromRgb(222, 226, 230);          // #DEE2E6
            resources["BorderBrush"] = new SolidColorBrush((Color)resources["BorderColor"]);

            // Color de acento (azul)
            resources["AccentColor"] = Color.FromRgb(0, 122, 204);            // #007ACC
            resources["AccentBrush"] = new SolidColorBrush((Color)resources["AccentColor"]);

            // TextBox
            resources["TextBoxBackgroundColor"] = Color.FromRgb(255, 255, 255); // #FFFFFF
            resources["TextBoxBackgroundBrush"] = new SolidColorBrush((Color)resources["TextBoxBackgroundColor"]);
            resources["TextBoxForegroundColor"] = Color.FromRgb(0, 0, 0);       // #000000
            resources["TextBoxForegroundBrush"] = new SolidColorBrush((Color)resources["TextBoxForegroundColor"]);

            // Botones
            resources["ButtonBackgroundColor"] = Color.FromRgb(0, 122, 204);   // #007ACC
            resources["ButtonBackgroundBrush"] = new SolidColorBrush((Color)resources["ButtonBackgroundColor"]);

            // Header y Sidebar
            resources["HeaderBackgroundColor"] = Color.FromRgb(248, 249, 250); // #F8F9FA
            resources["HeaderBackgroundBrush"] = new SolidColorBrush((Color)resources["HeaderBackgroundColor"]);
            resources["SidebarBackgroundColor"] = Color.FromRgb(233, 236, 239); // #E9ECEF
            resources["SidebarBackgroundBrush"] = new SolidColorBrush((Color)resources["SidebarBackgroundColor"]);

            // Colores semánticos
            resources["SuccessColor"] = Color.FromRgb(40, 167, 69);           // #28A745
            resources["SuccessBrush"] = new SolidColorBrush((Color)resources["SuccessColor"]);
            resources["WarningColor"] = Color.FromRgb(255, 193, 7);           // #FFC107
            resources["WarningBrush"] = new SolidColorBrush((Color)resources["WarningColor"]);
            resources["DangerColor"] = Color.FromRgb(220, 53, 69);            // #DC3545
            resources["DangerBrush"] = new SolidColorBrush((Color)resources["DangerColor"]);
            resources["InfoColor"] = Color.FromRgb(23, 162, 184);             // #17A2B8
            resources["InfoBrush"] = new SolidColorBrush((Color)resources["InfoColor"]);
            resources["MutedColor"] = Color.FromRgb(108, 117, 125);           // #6C757D
            resources["MutedBrush"] = new SolidColorBrush((Color)resources["MutedColor"]);
        }

        private void ApplyDarkTheme(Application app)
        {
            // Tema Oscuro (colores actuales)
            var resources = app.Resources;

            // Colores principales
            resources["BackgroundColor"] = Color.FromRgb(45, 45, 48);         // #2D2D30
            resources["BackgroundBrush"] = new SolidColorBrush((Color)resources["BackgroundColor"]);

            resources["ForegroundColor"] = Color.FromRgb(255, 255, 255);      // #FFFFFF
            resources["ForegroundBrush"] = new SolidColorBrush((Color)resources["ForegroundColor"]);

            resources["SecondaryBackgroundColor"] = Color.FromRgb(30, 30, 30); // #1E1E1E
            resources["SecondaryBackgroundBrush"] = new SolidColorBrush((Color)resources["SecondaryBackgroundColor"]);

            resources["BorderColor"] = Color.FromRgb(63, 63, 70);             // #3F3F46
            resources["BorderBrush"] = new SolidColorBrush((Color)resources["BorderColor"]);

            // Color de acento (azul)
            resources["AccentColor"] = Color.FromRgb(0, 122, 204);            // #007ACC
            resources["AccentBrush"] = new SolidColorBrush((Color)resources["AccentColor"]);

            // TextBox (fondo blanco, texto negro como solicitado)
            resources["TextBoxBackgroundColor"] = Color.FromRgb(255, 255, 255); // #FFFFFF
            resources["TextBoxBackgroundBrush"] = new SolidColorBrush((Color)resources["TextBoxBackgroundColor"]);
            resources["TextBoxForegroundColor"] = Color.FromRgb(0, 0, 0);       // #000000
            resources["TextBoxForegroundBrush"] = new SolidColorBrush((Color)resources["TextBoxForegroundColor"]);

            // Botones
            resources["ButtonBackgroundColor"] = Color.FromRgb(0, 122, 204);   // #007ACC
            resources["ButtonBackgroundBrush"] = new SolidColorBrush((Color)resources["ButtonBackgroundColor"]);

            // Header y Sidebar
            resources["HeaderBackgroundColor"] = Color.FromRgb(30, 30, 30);    // #1E1E1E
            resources["HeaderBackgroundBrush"] = new SolidColorBrush((Color)resources["HeaderBackgroundColor"]);
            resources["SidebarBackgroundColor"] = Color.FromRgb(26, 26, 26);   // #1A1A1A
            resources["SidebarBackgroundBrush"] = new SolidColorBrush((Color)resources["SidebarBackgroundColor"]);

            // Colores semánticos
            resources["SuccessColor"] = Color.FromRgb(40, 167, 69);           // #28A745
            resources["SuccessBrush"] = new SolidColorBrush((Color)resources["SuccessColor"]);
            resources["WarningColor"] = Color.FromRgb(255, 152, 0);           // #FF9800
            resources["WarningBrush"] = new SolidColorBrush((Color)resources["WarningColor"]);
            resources["DangerColor"] = Color.FromRgb(220, 53, 69);            // #DC3545
            resources["DangerBrush"] = new SolidColorBrush((Color)resources["DangerColor"]);
            resources["InfoColor"] = Color.FromRgb(0, 188, 212);              // #00BCD4
            resources["InfoBrush"] = new SolidColorBrush((Color)resources["InfoColor"]);
            resources["MutedColor"] = Color.FromRgb(108, 117, 125);           // #6C757D
            resources["MutedBrush"] = new SolidColorBrush((Color)resources["MutedColor"]);
        }

        private void LoadThemeFromConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ThemeConfig>(json);
                    if (config != null)
                    {
                        _currentTheme = config.Theme;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme config: {ex.Message}");
                // Si hay error, usar tema por defecto (Light)
                _currentTheme = ThemeMode.Light;
            }
        }

        private void SaveThemeToConfig()
        {
            try
            {
                var config = new ThemeConfig { Theme = CurrentTheme };
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving theme config: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class ThemeConfig
        {
            public ThemeMode Theme { get; set; } = ThemeMode.Light;
        }
    }
}