using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;
        private DispatcherTimer _timer;

        // Estado de la aplicación
        private bool _isLoggedIn;
        private object? _currentView;
        private string _statusMessage = "Listo";
        private string _currentDateTime = "";
        private int _totalAbonados;
        private int _abonadosActivos;

        // Visibilidad de controles
        private Visibility _showMainContent = Visibility.Collapsed;
        private Visibility _canAccessDashboard = Visibility.Collapsed;
        private Visibility _canAccessAbonados = Visibility.Collapsed;
        private Visibility _canAccessTiposAbono = Visibility.Collapsed;
        private Visibility _canAccessGestores = Visibility.Collapsed;
        private Visibility _canAccessPeñas = Visibility.Collapsed;
        private Visibility _canAccessUsuarios = Visibility.Collapsed;
        private Visibility _canAccessHistorial = Visibility.Collapsed;
        private Visibility _canAccessConfiguracion = Visibility.Collapsed;
        private Visibility _canAccessTemplates = Visibility.Collapsed;
        private Visibility _canChangeLicense = Visibility.Collapsed;

        public MainViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            InitializeCommands();
            InitializeTimer();

            // Inicializar base de datos
            _ = InitializeDatabaseAsync();
        }

        #region Properties

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        public int TotalAbonados
        {
            get => _totalAbonados;
            set => SetProperty(ref _totalAbonados, value);
        }

        public int AbonadosActivos
        {
            get => _abonadosActivos;
            set => SetProperty(ref _abonadosActivos, value);
        }

        public Visibility ShowMainContent
        {
            get => _showMainContent;
            set => SetProperty(ref _showMainContent, value);
        }

        // Propiedades de visibilidad
        public Visibility CanAccessDashboard
        {
            get => _canAccessDashboard;
            set => SetProperty(ref _canAccessDashboard, value);
        }

        public Visibility CanAccessAbonados
        {
            get => _canAccessAbonados;
            set => SetProperty(ref _canAccessAbonados, value);
        }

        public Visibility CanAccessTiposAbono
        {
            get => _canAccessTiposAbono;
            set => SetProperty(ref _canAccessTiposAbono, value);
        }

        public Visibility CanAccessGestores
        {
            get => _canAccessGestores;
            set => SetProperty(ref _canAccessGestores, value);
        }

        public Visibility CanAccessPeñas
        {
            get => _canAccessPeñas;
            set => SetProperty(ref _canAccessPeñas, value);
        }

        public Visibility CanAccessUsuarios
        {
            get => _canAccessUsuarios;
            set => SetProperty(ref _canAccessUsuarios, value);
        }

        public Visibility CanAccessHistorial
        {
            get => _canAccessHistorial;
            set => SetProperty(ref _canAccessHistorial, value);
        }

        public Visibility CanAccessConfiguracion
        {
            get => _canAccessConfiguracion;
            set => SetProperty(ref _canAccessConfiguracion, value);
        }

        public Visibility CanAccessTemplates
        {
            get => _canAccessTemplates;
            set => SetProperty(ref _canAccessTemplates, value);
        }

        public Visibility CanChangeLicense
        {
            get => _canChangeLicense;
            set => SetProperty(ref _canChangeLicense, value);
        }

        #endregion

        #region Commands

        public ICommand ShowDashboardCommand { get; private set; } = null!;
        public ICommand ShowAbonadosCommand { get; private set; } = null!;
        public ICommand ShowTiposAbonoCommand { get; private set; } = null!;
        public ICommand ShowGestoresCommand { get; private set; } = null!;
        public ICommand ShowPeñasCommand { get; private set; } = null!;
        public ICommand ShowUsuariosCommand { get; private set; } = null!;
        public ICommand ShowHistorialCommand { get; private set; } = null!;
        public ICommand ShowConfiguracionCommand { get; private set; } = null!;
        public ICommand ShowCardDesignerCommand { get; private set; } = null!;
        public ICommand ChangeLicenseCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand ExitCommand { get; private set; } = null!;
        public ICommand ShowReportsCommand { get; private set; } = null!;
        public ICommand ShowExportCommand { get; private set; } = null!;
        public ICommand ShowNewSeasonCommand { get; private set; } = null!;
        public ICommand ShowBackupCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            ShowDashboardCommand = new RelayCommand(ShowDashboard);
            ShowAbonadosCommand = new RelayCommand(ShowAbonados);
            ShowTiposAbonoCommand = new RelayCommand(ShowTiposAbono);
            ShowGestoresCommand = new RelayCommand(ShowGestores);
            ShowPeñasCommand = new RelayCommand(ShowPeñas);
            ShowUsuariosCommand = new RelayCommand(ShowUsuarios);
            ShowHistorialCommand = new RelayCommand(ShowHistorial);
            ShowConfiguracionCommand = new RelayCommand(ShowConfiguracion);
            ShowCardDesignerCommand = new RelayCommand(ShowCardDesigner);
            ChangeLicenseCommand = new RelayCommand(ChangeLicense);
            LogoutCommand = new RelayCommand(Logout);
            ExitCommand = new RelayCommand(Exit);
            ShowReportsCommand = new RelayCommand(ShowReports);
            ShowExportCommand = new RelayCommand(ShowExport);
            ShowNewSeasonCommand = new RelayCommand(ShowNewSeason);
            ShowBackupCommand = new RelayCommand(ShowBackup);
        }

        #endregion

        #region Command Methods

        private void ShowDashboard()
        {
            CurrentView = new DashboardView();
            StatusMessage = "Dashboard cargado";
        }

        private void ShowAbonados()
        {
            CurrentView = new AbonadosView();
            StatusMessage = "Gestión de abonados";
        }

        private void ShowTiposAbono()
        {
            CurrentView = new TiposAbonoView();
            StatusMessage = "Tipos de abono";
        }

        private void ShowGestores()
        {
            CurrentView = new GestoresView();
            StatusMessage = "Gestión de gestores";
        }

        private void ShowPeñas()
        {
            CurrentView = new PeñasView();
            StatusMessage = "Gestión de peñas";
        }

        private void ShowUsuarios()
        {
            CurrentView = new UsuariosView();
            StatusMessage = "Gestión de usuarios";
        }

        private void ShowHistorial()
        {
            CurrentView = new HistorialView();
            StatusMessage = "Historial de acciones";
        }

        private void ShowConfiguracion()
        {
            CurrentView = new ConfiguracionView();
            StatusMessage = "Configuración";
        }

        #region Método para abrir diseñador (Agregar a la clase AbonadosViewModel)

        private void ShowCardDesigner()
        {
            try
            {
                var designerWindow = new CardDesignerWindow();
                designerWindow.Owner = Application.Current.MainWindow;
                designerWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir diseñador de tarjetas: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion


        private void ChangeLicense()
        {
            try
            {
                var licenseWindow = new LicenseWindow();
                licenseWindow.ShowDialog();
                StatusMessage = "Licencia actualizada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar licencia: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Logout()
        {
            var result = MessageBox.Show("¿Está seguro de cerrar sesión?", "Confirmar",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                UserSession.Instance.Logout();
                IsLoggedIn = false;
                ShowMainContent = Visibility.Collapsed;
                CurrentView = null;
                StatusMessage = "Sesión cerrada";
            }
        }

        private void Exit()
        {
            var result = MessageBox.Show("¿Está seguro de salir de la aplicación?", "Confirmar",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region Methods

        public async Task RefreshUserInterface()
        {
            if (UserSession.Instance.IsLoggedIn)
            {
                IsLoggedIn = true;
                ShowMainContent = Visibility.Visible;
                UpdatePermissionVisibility();
                ShowDashboard(); // Mostrar dashboard por defecto
                await UpdateStatistics();
                StatusMessage = $"Bienvenido, {UserSession.Instance.CurrentUser?.NombreCompleto ?? UserSession.Instance.CurrentUser?.NombreUsuario}";
            }
            else
            {
                IsLoggedIn = false;
                ShowMainContent = Visibility.Collapsed;
                HideAllMenus();
            }
        }

        private void UpdatePermissionVisibility()
        {
            var permissions = UserSession.Instance.CurrentPermissions;
            if (permissions == null)
            {
                HideAllMenus();
                return;
            }

            CanAccessDashboard = permissions.CanAccessDashboard ? Visibility.Visible : Visibility.Collapsed;
            CanAccessAbonados = permissions.CanAccessAbonados ? Visibility.Visible : Visibility.Collapsed;
            CanAccessTiposAbono = permissions.CanAccessTiposAbono ? Visibility.Visible : Visibility.Collapsed;
            CanAccessGestores = permissions.CanAccessGestores ? Visibility.Visible : Visibility.Collapsed;
            CanAccessPeñas = permissions.CanAccessPeñas ? Visibility.Visible : Visibility.Collapsed;
            CanAccessUsuarios = permissions.CanAccessUsuarios ? Visibility.Visible : Visibility.Collapsed;
            CanAccessHistorial = permissions.CanAccessHistorial ? Visibility.Visible : Visibility.Collapsed;
            CanAccessConfiguracion = permissions.CanAccessConfiguracion ? Visibility.Visible : Visibility.Collapsed;
            CanAccessTemplates = permissions.CanAccessTemplates ? Visibility.Visible : Visibility.Collapsed;
            CanChangeLicense = permissions.CanChangeLicense ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HideAllMenus()
        {
            CanAccessDashboard = Visibility.Collapsed;
            CanAccessAbonados = Visibility.Collapsed;
            CanAccessTiposAbono = Visibility.Collapsed;
            CanAccessGestores = Visibility.Collapsed;
            CanAccessPeñas = Visibility.Collapsed;
            CanAccessUsuarios = Visibility.Collapsed;
            CanAccessHistorial = Visibility.Collapsed;
            CanAccessConfiguracion = Visibility.Collapsed;
            CanAccessTemplates = Visibility.Collapsed;
            CanChangeLicense = Visibility.Collapsed;
        }

        private async Task UpdateStatistics()
        {
            try
            {
                TotalAbonados = await _dbContext.Abonados.CountAsync();
                AbonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
            }
            catch { }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _dbContext.Database.EnsureCreatedAsync();
                StatusMessage = "Base de datos inicializada";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error en base de datos: {ex.Message}";
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _timer.Start();
        }

        #endregion

        public void Dispose()
        {
            _timer?.Stop();
            _dbContext?.Dispose();
        }

        private void ShowReports()
        {
            try
            {
                var reportWindow = new ReportWindow();
                reportWindow.Show();
                StatusMessage = "Ventana de reportes abierta";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir reportes: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowExport()
        {
            try
            {
                var exportWindow = new ExportWindow();
                exportWindow.Show();
                StatusMessage = "Ventana de exportación abierta";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir exportación: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowNewSeason()
        {
            try
            {
                var seasonWindow = new NuevaTemporadaWindow();
                seasonWindow.ShowDialog();
                StatusMessage = "Proceso de nueva temporada completado";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar nueva temporada: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowBackup()
        {
            try
            {
                var exportService = new ExportService();
                _ = exportService.ExportCompleteBackup();
                StatusMessage = "Creando backup completo...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear backup: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}