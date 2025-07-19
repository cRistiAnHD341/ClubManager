using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;
using ClubManager.Services;
using ClubManager.Models;
using ClubManager.Views;
using ClubManager.Data;
using ClubManager.Commands;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILicenseService _licenseService;
        private readonly ClubDbContext _dbContext;
        private DispatcherTimer _timer;

        private object? _currentView;
        private string _windowTitle = "Club Manager";
        private string _clubName = "Mi Club Deportivo";
        private string _currentSeason = "2024-2025";
        private string _currentUser = "Usuario";
        private string _statusMessage = "Listo";
        private string _licenseStatusText = "Sin Licencia";
        private string _licenseStatusColor = "#FFF44336";
        private string _currentDateTime = "";
        private int _totalAbonados = 0;
        private int _abonadosActivos = 0;
        private bool _canEdit = false;

        public MainViewModel()
        {
            _licenseService = new LicenseService();
            _dbContext = new ClubDbContext();

            // Obtener referencia al servicio de temas
            ThemeService = App.ThemeService;

            InitializeCommands();
            InitializeTimer();
            InitializeDatabase();
            LoadClubConfiguration();
            UpdateLicenseStatus();
            LoadStatistics();

            // Mostrar dashboard por defecto
            ShowDashboard();
        }

        #region Properties

        public IThemeService ThemeService { get; }

        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        public string ClubName
        {
            get => _clubName;
            set { _clubName = value; OnPropertyChanged(); }
        }

        public string CurrentSeason
        {
            get => _currentSeason;
            set { _currentSeason = value; OnPropertyChanged(); }
        }

        public string CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string LicenseStatusText
        {
            get => _licenseStatusText;
            set { _licenseStatusText = value; OnPropertyChanged(); }
        }

        public string LicenseStatusColor
        {
            get => _licenseStatusColor;
            set { _licenseStatusColor = value; OnPropertyChanged(); }
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set { _currentDateTime = value; OnPropertyChanged(); }
        }

        public int TotalAbonados
        {
            get => _totalAbonados;
            set { _totalAbonados = value; OnPropertyChanged(); }
        }

        public int AbonadosActivos
        {
            get => _abonadosActivos;
            set { _abonadosActivos = value; OnPropertyChanged(); }
        }

        public bool CanEdit
        {
            get => _canEdit;
            set { _canEdit = value; OnPropertyChanged(); }
        }

        public string? ClubLogo { get; set; } = null;

        #endregion

        #region Commands

        public ICommand ShowDashboardCommand { get; private set; }
        public ICommand ShowAbonadosCommand { get; private set; }
        public ICommand ShowTiposAbonoCommand { get; private set; }
        public ICommand ShowGestoresCommand { get; private set; }
        public ICommand ShowPeñasCommand { get; private set; }
        public ICommand ShowUsuariosCommand { get; private set; }
        public ICommand ShowHistorialCommand { get; private set; }
        public ICommand NewSeasonCommand { get; private set; }
        public ICommand ChangeLicenseCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            ShowDashboardCommand = new RelayCommand(ShowDashboard);
            ShowAbonadosCommand = new RelayCommand(ShowAbonados);
            ShowTiposAbonoCommand = new RelayCommand(ShowTiposAbono);
            ShowGestoresCommand = new RelayCommand(ShowGestores);
            ShowPeñasCommand = new RelayCommand(ShowPeñas);
            ShowUsuariosCommand = new RelayCommand(ShowUsuarios);
            ShowHistorialCommand = new RelayCommand(ShowHistorial);
            NewSeasonCommand = new RelayCommand(NewSeason);
            ChangeLicenseCommand = new RelayCommand(ChangeLicense);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ExitCommand = new RelayCommand(Exit);
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _timer.Start();
            CurrentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private async void InitializeDatabase()
        {
            try
            {
                await _dbContext.InitializeDatabaseAsync();
                StatusMessage = "Base de datos inicializada correctamente";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al inicializar la base de datos: {ex.Message}";
                MessageBox.Show($"Error al inicializar la base de datos:\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadClubConfiguration()
        {
            try
            {
                var config = await _dbContext.Configuracion.FirstOrDefaultAsync();
                if (config != null)
                {
                    ClubName = config.NombreClub;
                    CurrentSeason = config.TemporadaActual ?? "2024-2025";
                    WindowTitle = $"{ClubName} - Club Manager";

                    if (!string.IsNullOrEmpty(config.RutaEscudo) && System.IO.File.Exists(config.RutaEscudo))
                    {
                        ClubLogo = config.RutaEscudo;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar configuración: {ex.Message}";
            }
        }

        private void UpdateLicenseStatus()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();

            if (licenseInfo.IsValid)
            {
                if (licenseInfo.IsExpired)
                {
                    LicenseStatusText = "SOLO LECTURA";
                    LicenseStatusColor = "#FFFF9800"; // Amarillo/Naranja
                    CanEdit = false;
                    StatusMessage = "Licencia expirada - Modo solo lectura";
                }
                else
                {
                    LicenseStatusText = "LICENCIA VÁLIDA";
                    LicenseStatusColor = "#FF4CAF50"; // Verde
                    CanEdit = true;
                    StatusMessage = $"Licencia válida hasta {licenseInfo.ExpirationDate:dd/MM/yyyy}";
                }
            }
            else
            {
                LicenseStatusText = "SIN LICENCIA";
                LicenseStatusColor = "#FFF44336"; // Rojo
                CanEdit = false;
                StatusMessage = "Sin licencia válida - Modo solo lectura";
            }
        }

        private async void LoadStatistics()
        {
            try
            {
                TotalAbonados = await _dbContext.Abonados.CountAsync();
                AbonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar estadísticas: {ex.Message}";
            }
        }

        #endregion

        #region Command Methods

        private void ShowDashboard()
        {
            CurrentView = new Views.DashboardView();
            StatusMessage = "Dashboard cargado";
        }

        private void ShowAbonados()
        {
            CurrentView = new Views.AbonadosView();
            StatusMessage = "Lista de abonados cargada";
        }

        private void ShowTiposAbono()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para editar. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentView = new TiposAbonoView();
            StatusMessage = "Tipos de abono cargados";
        }

        private void ShowGestores()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para editar. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentView = new GestoresView();
            StatusMessage = "Gestores cargados";
        }

        private void ShowPeñas()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para editar. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentView = new PeñasView();
            StatusMessage = "Peñas cargadas";
        }

        private void ShowUsuarios()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para gestionar usuarios. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentView = new Views.UsuariosView();
            StatusMessage = "Gestión de usuarios cargada";
        }

        private void ShowHistorial()
        {
            CurrentView = new HistorialView();
            StatusMessage = "Historial cargado";
        }

        private void NewSeason()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para crear nueva temporada. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("¿Estás seguro de que quieres crear una nueva temporada?\n\n" +
                                       "Esta acción afectará a todos los abonados existentes.",
                                       "Nueva Temporada", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var newSeasonWindow = new Views.NuevaTemporadaWindow();
                if (newSeasonWindow.ShowDialog() == true)
                {
                    LoadStatistics();
                    LoadClubConfiguration();
                    StatusMessage = "Nueva temporada creada exitosamente";
                }
            }
        }

        private void ChangeLicense()
        {
            var licenseWindow = new LicenseWindow();
            if (licenseWindow.ShowDialog() == true)
            {
                UpdateLicenseStatus();
                LoadStatistics();
                StatusMessage = "Licencia actualizada";
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                LoadClubConfiguration();
                StatusMessage = "Configuración actualizada";
            }
        }

        private void Exit()
        {
            var result = MessageBox.Show("¿Estás seguro de que quieres salir del programa?",
                                       "Salir", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _timer?.Stop();
            _dbContext?.Dispose();
        }

        #endregion
    }

    // Placeholder Views - se implementarán después
    public class TiposAbonoView : System.Windows.Controls.UserControl
    {
        public TiposAbonoView()
        {
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "🎫 Tipos de Abono - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
            };
        }
    }

    public class GestoresView : System.Windows.Controls.UserControl
    {
        public GestoresView()
        {
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "🏢 Gestores - Cargando...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
            };
            ((System.Windows.Controls.TextBlock)Content).SetResourceReference(
                System.Windows.Controls.TextBlock.ForegroundProperty, "ForegroundBrush");
        }
    }

    public class PeñasView : System.Windows.Controls.UserControl
    {
        public PeñasView()
        {
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "🎪 Peñas - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
            };
            ((System.Windows.Controls.TextBlock)Content).SetResourceReference(
                System.Windows.Controls.TextBlock.ForegroundProperty, "ForegroundBrush");
        }
    }

    public class HistorialView : System.Windows.Controls.UserControl
    {
        public HistorialView()
        {
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "📜 Historial - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
            };
            ((System.Windows.Controls.TextBlock)Content).SetResourceReference(
                System.Windows.Controls.TextBlock.ForegroundProperty, "ForegroundBrush");
        }
    }
}