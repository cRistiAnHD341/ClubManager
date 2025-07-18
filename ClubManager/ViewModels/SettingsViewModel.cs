using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        // Información del Club
        private string _nombreClub = "";
        private string _temporadaActual = "";
        private string _direccionClub = "";
        private string _telefonoClub = "";
        private string _emailClub = "";
        private string _webClub = "";
        private string _logoClub = "";

        // Estadísticas
        private int _totalAbonados = 0;
        private int _abonadosActivos = 0;
        private int _totalPeñas = 0;

        // Configuraciones del Sistema
        private bool _autoBackup = true;
        private bool _confirmarEliminaciones = true;
        private bool _mostrarAyudas = true;
        private bool _numeracionAutomatica = true;
        private string _formatoNumeroSocioSeleccionado = "simple";

        // Información del Sistema
        private string _versionSistema = "1.0.0";
        private string _rutaBaseDatos = "";
        private string _tamañoBaseDatos = "";
        private string _ultimoBackup = "";
        private string _totalRegistros = "";

        // Información de Licencia
        private string _licenseStatusText = "";
        private string _licenseStatusColor = "";
        private string _licenseDetails = "";
        private string _licenseState = "";
        private string _licenseClubName = "";
        private string _licenseExpiration = "";
        private string _licenseMode = "";

        // Collections
        private ObservableCollection<FormatoNumeroSocio> _formatosNumeroSocio;

        public event EventHandler<bool>? SettingsSaved;

        public SettingsViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();
            _formatosNumeroSocio = new ObservableCollection<FormatoNumeroSocio>();

            InitializeCommands();
            InitializeFormatosNumeroSocio();
            LoadSettings();
            LoadStatistics();
            LoadSystemInfo();
            LoadLicenseInfo();
        }

        #region Properties

        // Información del Club
        public string NombreClub
        {
            get => _nombreClub;
            set { _nombreClub = value; OnPropertyChanged(); }
        }

        public string TemporadaActual
        {
            get => _temporadaActual;
            set { _temporadaActual = value; OnPropertyChanged(); }
        }

        public string DireccionClub
        {
            get => _direccionClub;
            set { _direccionClub = value; OnPropertyChanged(); }
        }

        public string TelefonoClub
        {
            get => _telefonoClub;
            set { _telefonoClub = value; OnPropertyChanged(); }
        }

        public string EmailClub
        {
            get => _emailClub;
            set { _emailClub = value; OnPropertyChanged(); }
        }

        public string WebClub
        {
            get => _webClub;
            set { _webClub = value; OnPropertyChanged(); }
        }

        public string LogoClub
        {
            get => _logoClub;
            set
            {
                _logoClub = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLogo));
                OnPropertyChanged(nameof(NoLogo));
                OnPropertyChanged(nameof(CanRemoveLogo));
            }
        }

        // Estadísticas
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

        public int TotalPeñas
        {
            get => _totalPeñas;
            set { _totalPeñas = value; OnPropertyChanged(); }
        }

        // Configuraciones del Sistema
        public bool AutoBackup
        {
            get => _autoBackup;
            set { _autoBackup = value; OnPropertyChanged(); }
        }

        public bool ConfirmarEliminaciones
        {
            get => _confirmarEliminaciones;
            set { _confirmarEliminaciones = value; OnPropertyChanged(); }
        }

        public bool MostrarAyudas
        {
            get => _mostrarAyudas;
            set { _mostrarAyudas = value; OnPropertyChanged(); }
        }

        public bool NumeracionAutomatica
        {
            get => _numeracionAutomatica;
            set { _numeracionAutomatica = value; OnPropertyChanged(); }
        }

        public string FormatoNumeroSocioSeleccionado
        {
            get => _formatoNumeroSocioSeleccionado;
            set { _formatoNumeroSocioSeleccionado = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FormatoNumeroSocio> FormatosNumeroSocio
        {
            get => _formatosNumeroSocio;
            set { _formatosNumeroSocio = value; OnPropertyChanged(); }
        }

        // Información del Sistema
        public string VersionSistema
        {
            get => _versionSistema;
            set { _versionSistema = value; OnPropertyChanged(); }
        }

        public string RutaBaseDatos
        {
            get => _rutaBaseDatos;
            set { _rutaBaseDatos = value; OnPropertyChanged(); }
        }

        public string TamañoBaseDatos
        {
            get => _tamañoBaseDatos;
            set { _tamañoBaseDatos = value; OnPropertyChanged(); }
        }

        public string UltimoBackup
        {
            get => _ultimoBackup;
            set { _ultimoBackup = value; OnPropertyChanged(); }
        }

        public string TotalRegistros
        {
            get => _totalRegistros;
            set { _totalRegistros = value; OnPropertyChanged(); }
        }

        // Información de Licencia
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

        public string LicenseDetails
        {
            get => _licenseDetails;
            set { _licenseDetails = value; OnPropertyChanged(); }
        }

        public string LicenseState
        {
            get => _licenseState;
            set { _licenseState = value; OnPropertyChanged(); }
        }

        public string LicenseClubName
        {
            get => _licenseClubName;
            set { _licenseClubName = value; OnPropertyChanged(); }
        }

        public string LicenseExpiration
        {
            get => _licenseExpiration;
            set { _licenseExpiration = value; OnPropertyChanged(); }
        }

        public string LicenseMode
        {
            get => _licenseMode;
            set { _licenseMode = value; OnPropertyChanged(); }
        }

        // UI Properties
        public Visibility HasLogo => !string.IsNullOrEmpty(LogoClub) && File.Exists(LogoClub) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NoLogo => !string.IsNullOrEmpty(LogoClub) && File.Exists(LogoClub) ? Visibility.Collapsed : Visibility.Visible;
        public bool CanRemoveLogo => !string.IsNullOrEmpty(LogoClub);

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand RestoreDefaultsCommand { get; private set; }
        public ICommand SelectLogoCommand { get; private set; }
        public ICommand RemoveLogoCommand { get; private set; }
        public ICommand CreateBackupCommand { get; private set; }
        public ICommand OpenBackupsFolderCommand { get; private set; }
        public ICommand OptimizeDatabaseCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand ChangeLicenseCommand { get; private set; }
        public ICommand ShowLicenseInfoCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(SaveSettings, CanSaveSettings);
            RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
            SelectLogoCommand = new RelayCommand(SelectLogo);
            RemoveLogoCommand = new RelayCommand(RemoveLogo);
            CreateBackupCommand = new RelayCommand(CreateBackup);
            OpenBackupsFolderCommand = new RelayCommand(OpenBackupsFolder);
            OptimizeDatabaseCommand = new RelayCommand(OptimizeDatabase);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            ChangeLicenseCommand = new RelayCommand(ChangeLicense);
            ShowLicenseInfoCommand = new RelayCommand(ShowLicenseInfo);
        }

        private void InitializeFormatosNumeroSocio()
        {
            FormatosNumeroSocio.Add(new FormatoNumeroSocio { Value = "simple", Display = "Simple (1, 2, 3...)" });
            FormatosNumeroSocio.Add(new FormatoNumeroSocio { Value = "padded", Display = "Con ceros (001, 002, 003...)" });
            FormatosNumeroSocio.Add(new FormatoNumeroSocio { Value = "prefix", Display = "Con prefijo (CLB001, CLB002...)" });
            FormatosNumeroSocio.Add(new FormatoNumeroSocio { Value = "year", Display = "Con año (2024001, 2024002...)" });
        }

        private async void LoadSettings()
        {
            try
            {
                var config = await _dbContext.Configuracion.FirstOrDefaultAsync();
                if (config != null)
                {
                    NombreClub = config.NombreClub;
                    TemporadaActual = config.TemporadaActual ?? "2024-2025";
                    LogoClub = config.RutaEscudo ?? "";

                    // Cargar configuraciones adicionales desde una tabla de configuración extendida
                    // Por ahora usar valores por defecto
                    DireccionClub = "";
                    TelefonoClub = "";
                    EmailClub = "";
                    WebClub = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuración: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadStatistics()
        {
            try
            {
                TotalAbonados = await _dbContext.Abonados.CountAsync();
                AbonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
                TotalPeñas = await _dbContext.Peñas.CountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
            }
        }

        private void LoadSystemInfo()
        {
            try
            {
                VersionSistema = "1.0.0 Beta";
                RutaBaseDatos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClubManager.db");

                if (File.Exists(RutaBaseDatos))
                {
                    var fileInfo = new FileInfo(RutaBaseDatos);
                    TamañoBaseDatos = $"{fileInfo.Length / 1024:N0} KB";
                }
                else
                {
                    TamañoBaseDatos = "No encontrada";
                }

                // Buscar último backup
                string backupsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (Directory.Exists(backupsPath))
                {
                    var backupFiles = Directory.GetFiles(backupsPath, "*.db")
                        .OrderByDescending(f => File.GetCreationTime(f))
                        .FirstOrDefault();

                    if (backupFiles != null)
                    {
                        UltimoBackup = File.GetCreationTime(backupFiles).ToString("dd/MM/yyyy HH:mm");
                    }
                    else
                    {
                        UltimoBackup = "Nunca";
                    }
                }
                else
                {
                    UltimoBackup = "Nunca";
                }

                TotalRegistros = $"{TotalAbonados + TotalPeñas}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading system info: {ex.Message}");
            }
        }

        private void LoadLicenseInfo()
        {
            try
            {
                _licenseService.LoadSavedLicense();
                var licenseInfo = _licenseService.GetCurrentLicenseInfo();

                if (licenseInfo.IsValid)
                {
                    if (licenseInfo.IsExpired)
                    {
                        LicenseStatusText = "⚠️ LICENCIA EXPIRADA";
                        LicenseStatusColor = "#FFFF9800";
                        LicenseState = "Expirada";
                        LicenseMode = "Solo Lectura";
                        LicenseDetails = "La licencia ha expirado. El sistema funciona en modo solo lectura.";
                    }
                    else
                    {
                        LicenseStatusText = "✅ LICENCIA VÁLIDA";
                        LicenseStatusColor = "#FF4CAF50";
                        LicenseState = "Válida";
                        LicenseMode = "Completo";
                        LicenseDetails = "La licencia está activa y funcionando correctamente.";
                    }

                    LicenseClubName = licenseInfo.ClubName;
                    LicenseExpiration = licenseInfo.ExpirationDate?.ToString("dd/MM/yyyy HH:mm") ?? "No disponible";
                }
                else
                {
                    LicenseStatusText = "❌ SIN LICENCIA";
                    LicenseStatusColor = "#FFF44336";
                    LicenseState = "Inválida";
                    LicenseMode = "Solo Lectura";
                    LicenseDetails = "No hay una licencia válida configurada.";
                    LicenseClubName = "No disponible";
                    LicenseExpiration = "No disponible";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading license info: {ex.Message}");
            }
        }

        private bool CanSaveSettings()
        {
            return !string.IsNullOrWhiteSpace(NombreClub) && !string.IsNullOrWhiteSpace(TemporadaActual);
        }

        #endregion

        #region Command Methods

        private async void SaveSettings()
        {
            try
            {
                var config = await _dbContext.Configuracion.FirstOrDefaultAsync();
                if (config != null)
                {
                    config.NombreClub = NombreClub;
                    config.TemporadaActual = TemporadaActual;
                    config.RutaEscudo = LogoClub;
                    config.FechaModificacion = DateTime.Now;
                }
                else
                {
                    config = new Configuracion
                    {
                        Id = 1,
                        NombreClub = NombreClub,
                        TemporadaActual = TemporadaActual,
                        RutaEscudo = LogoClub,
                        FechaModificacion = DateTime.Now
                    };
                    _dbContext.Configuracion.Add(config);
                }

                await _dbContext.SaveChangesAsync();
                await LogAction("Actualizó configuración del club");

                MessageBox.Show("Configuración guardada correctamente.", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                SettingsSaved?.Invoke(this, true);
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                SettingsSaved?.Invoke(this, false);
            }
        }

        private void RestoreDefaults()
        {
            var result = MessageBox.Show("¿Estás seguro de que quieres restaurar la configuración por defecto?",
                                       "Restaurar Configuración", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NombreClub = "Mi Club Deportivo";
                TemporadaActual = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";
                DireccionClub = "";
                TelefonoClub = "";
                EmailClub = "";
                WebClub = "";
                LogoClub = "";
                AutoBackup = true;
                ConfirmarEliminaciones = true;
                MostrarAyudas = true;
                NumeracionAutomatica = true;
                FormatoNumeroSocioSeleccionado = "simple";
            }
        }

        private void SelectLogo()
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "Archivos de Imagen (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
                    Title = "Seleccionar Logo del Club"
                };

                if (openDialog.ShowDialog() == true)
                {
                    // Copiar archivo a carpeta del programa
                    string logoFileName = $"club_logo{Path.GetExtension(openDialog.FileName)}";
                    string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logoFileName);

                    File.Copy(openDialog.FileName, logoPath, true);
                    LogoClub = logoPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al seleccionar logo: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveLogo()
        {
            var result = MessageBox.Show("¿Estás seguro de que quieres quitar el logo del club?",
                                       "Quitar Logo", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LogoClub = "";
            }
        }

        private void CreateBackup()
        {
            try
            {
                // Implementar creación de backup manual
                MessageBox.Show("Funcionalidad de backup manual - Próximamente", "Información",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear backup: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenBackupsFolder()
        {
            try
            {
                string backupsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(backupsPath))
                {
                    Directory.CreateDirectory(backupsPath);
                }

                System.Diagnostics.Process.Start("explorer.exe", backupsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir carpeta de backups: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OptimizeDatabase()
        {
            MessageBox.Show("Optimización de base de datos - Funcionalidad próximamente", "Información",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GenerateReport()
        {
            MessageBox.Show("Generación de reportes - Funcionalidad próximamente", "Información",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChangeLicense()
        {
            var licenseWindow = new LicenseWindow();
            if (licenseWindow.ShowDialog() == true)
            {
                LoadLicenseInfo();
                MessageBox.Show("Licencia actualizada correctamente.", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowLicenseInfo()
        {
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            string info = $"Estado: {LicenseState}\n" +
                         $"Club: {LicenseClubName}\n" +
                         $"Expira: {LicenseExpiration}\n" +
                         $"Modo: {LicenseMode}\n\n" +
                         $"Detalles: {LicenseDetails}";

            MessageBox.Show(info, "Información de Licencia",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task LogAction(string accion)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = 1,
                    Accion = accion,
                    FechaHora = DateTime.Now
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch
            {
                // Si falla el log, no es crítico
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(NombreClub) || propertyName == nameof(TemporadaActual))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #endregion
    }

    #region Helper Classes

    public class FormatoNumeroSocio
    {
        public string Value { get; set; } = "";
        public string Display { get; set; } = "";
    }

    #endregion
}