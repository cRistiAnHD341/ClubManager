using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ClubManager.Commands;

namespace ClubManager.Services
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    public interface IThemeService
    {
        ThemeMode CurrentTheme { get; }
        string CurrentThemeText { get; }
        ICommand ToggleThemeCommand { get; }
        void SetTheme(ThemeMode theme);
        void ToggleTheme();
        event EventHandler<ThemeMode> ThemeChanged;
    }

    public class ThemeService : IThemeService, INotifyPropertyChanged
    {
        private ThemeMode _currentTheme = ThemeMode.Light;
        private readonly string _configPath;

        public event EventHandler<ThemeMode>? ThemeChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ThemeService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme_config.json");
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            LoadThemeFromConfig();

            // Aplicar tema después de un pequeño delay para asegurar que la app esté cargada
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyTheme();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
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
                    OnPropertyChanged(nameof(CurrentThemeText));
                    ThemeChanged?.Invoke(this, value);
                }
            }
        }

        public string CurrentThemeText => CurrentTheme == ThemeMode.Light ? "🌞 Claro" : "🌙 Oscuro";

        public ICommand ToggleThemeCommand { get; }

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
            // 🌞 TEMA CLARO MODERNO - Estilo Material Design
            try
            {
                // Fondo principal: Blanco puro para profesionalidad
                resources["BackgroundColor"] = Color.FromRgb(255, 255, 255);      // #FFFFFF
                resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));

                // Texto principal: Azul muy oscuro (más elegante que negro)
                resources["ForegroundColor"] = Color.FromRgb(23, 43, 77);         // #172B4D - Azul muy oscuro
                resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(23, 43, 77));

                // Fondo secundario: Gris ultraclaro para cards y paneles
                resources["SecondaryBackgroundColor"] = Color.FromRgb(250, 251, 252); // #FAFBFC - Gris ultraclaro
                resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(250, 251, 252));

                // Bordes: Gris claro y suave
                resources["BorderColor"] = Color.FromRgb(223, 225, 230);          // #DFE1E6 - Gris claro suave
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(223, 225, 230));

                // Color de acento: Azul moderno y vibrante
                resources["AccentColor"] = Color.FromRgb(0, 101, 255);            // #0065FF - Azul moderno
                resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 101, 255));

                // TextBox: Fondo blanco con borde sutil
                resources["TextBoxBackgroundColor"] = Color.FromRgb(255, 255, 255); // #FFFFFF
                resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                resources["TextBoxForegroundColor"] = Color.FromRgb(23, 43, 77);     // #172B4D
                resources["TextBoxForegroundBrush"] = new SolidColorBrush(Color.FromRgb(23, 43, 77));

                // Header: Fondo con gradiente sutil
                resources["HeaderBackgroundColor"] = Color.FromRgb(247, 248, 250); // #F7F8FA - Gris claro elegante
                resources["HeaderBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(247, 248, 250));

                // Sidebar: Fondo ligeramente más oscuro para contraste
                resources["SidebarBackgroundColor"] = Color.FromRgb(244, 245, 247); // #F4F5F7 - Gris medio claro
                resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(244, 245, 247));

                // Colores semánticos modernos
                resources["SuccessColor"] = Color.FromRgb(0, 135, 90);            // #00875A - Verde moderno
                resources["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(0, 135, 90));

                resources["WarningColor"] = Color.FromRgb(255, 143, 0);           // #FF8F00 - Naranja vibrante
                resources["WarningBrush"] = new SolidColorBrush(Color.FromRgb(255, 143, 0));

                resources["DangerColor"] = Color.FromRgb(222, 53, 11);            // #DE350B - Rojo moderno
                resources["DangerBrush"] = new SolidColorBrush(Color.FromRgb(222, 53, 11));

                resources["InfoColor"] = Color.FromRgb(0, 116, 224);              // #0074E0 - Azul información
                resources["InfoBrush"] = new SolidColorBrush(Color.FromRgb(0, 116, 224));

                resources["MutedColor"] = Color.FromRgb(107, 119, 140);           // #6B778C - Gris texto secundario
                resources["MutedBrush"] = new SolidColorBrush(Color.FromRgb(107, 119, 140));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en tema claro: {ex.Message}");
            }
        }

        private void ApplyDarkTheme(ResourceDictionary resources)
        {
            // 🌙 TEMA OSCURO
            try
            {
                resources["BackgroundColor"] = Color.FromRgb(45, 45, 48);         // #2D2D30
                resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));

                resources["ForegroundColor"] = Color.FromRgb(255, 255, 255);      // #FFFFFF
                resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));

                resources["SecondaryBackgroundColor"] = Color.FromRgb(30, 30, 30); // #1E1E1E
                resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));

                resources["BorderColor"] = Color.FromRgb(63, 63, 70);             // #3F3F46
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));

                resources["AccentColor"] = Color.FromRgb(0, 122, 204);            // #007ACC
                resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));

                resources["TextBoxBackgroundColor"] = Color.FromRgb(255, 255, 255); // #FFFFFF
                resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));

                resources["TextBoxForegroundColor"] = Color.FromRgb(0, 0, 0);       // #000000
                resources["TextBoxForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 0));

                resources["HeaderBackgroundColor"] = Color.FromRgb(30, 30, 30);    // #1E1E1E
                resources["HeaderBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));

                resources["SidebarBackgroundColor"] = Color.FromRgb(26, 26, 26);   // #1A1A1A
                resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));

                resources["SuccessColor"] = Color.FromRgb(40, 167, 69);           // #28A745
                resources["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(40, 167, 69));

                resources["WarningColor"] = Color.FromRgb(255, 152, 0);           // #FF9800
                resources["WarningBrush"] = new SolidColorBrush(Color.FromRgb(255, 152, 0));

                resources["DangerColor"] = Color.FromRgb(220, 53, 69);            // #DC3545
                resources["DangerBrush"] = new SolidColorBrush(Color.FromRgb(220, 53, 69));

                resources["InfoColor"] = Color.FromRgb(0, 188, 212);              // #00BCD4
                resources["InfoBrush"] = new SolidColorBrush(Color.FromRgb(0, 188, 212));

                resources["MutedColor"] = Color.FromRgb(108, 117, 125);           // #6C757D
                resources["MutedBrush"] = new SolidColorBrush(Color.FromRgb(108, 117, 125));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en tema oscuro: {ex.Message}");
            }
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
                System.Diagnostics.Debug.WriteLine($"Error cargando config tema: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error guardando config tema: {ex.Message}");
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