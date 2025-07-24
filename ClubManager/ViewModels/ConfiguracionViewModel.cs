using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ClubManager.Commands;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager.ViewModels
{
    public class ConfiguracionViewModel : BaseViewModel
    {
        private readonly IConfiguracionService _configuracionService;
        private readonly ILicenseService _licenseService;
        private Configuracion _configuracion;

        public ConfiguracionViewModel()
        {
            _configuracionService = new ConfiguracionService();
            _licenseService = new LicenseService();

            // Cargar configuración actual
            _configuracion = _configuracionService.GetConfiguracion() ?? new Configuracion();

            // Commands
            GuardarConfiguracionCommand = new RelayCommand(GuardarConfiguracion);
            RestaurarConfiguracionCommand = new RelayCommand(RestaurarConfiguracion);
            ExportarConfiguracionCommand = new RelayCommand(ExportarConfiguracion);
            ImportarDatosCommand = new RelayCommand(ImportarDatos); // ✅ NUEVO COMANDO
            SelectLogoCommand = new RelayCommand(SelectLogo);
            RemoveLogoCommand = new RelayCommand(RemoveLogo);
            SelectEscudoCommand = new RelayCommand(SelectEscudo);
            RemoveEscudoCommand = new RelayCommand(RemoveEscudo);
            ShowLicenseInfoCommand = new RelayCommand(ShowLicenseInfo);
            ChangeLicenseCommand = new RelayCommand(ChangeLicense);

            // Cargar información de licencia
            LoadLicenseInfo();
        }

        #region Properties

        public string NombreClub
        {
            get => _configuracion.NombreClub;
            set
            {
                if (_configuracion.NombreClub != value)
                {
                    _configuracion.NombreClub = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TemporadaActual
        {
            get => _configuracion.TemporadaActual;
            set
            {
                if (_configuracion.TemporadaActual != value)
                {
                    _configuracion.TemporadaActual = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DireccionClub
        {
            get => _configuracion.DireccionClub;
            set
            {
                if (_configuracion.DireccionClub != value)
                {
                    _configuracion.DireccionClub = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TelefonoClub
        {
            get => _configuracion.TelefonoClub;
            set
            {
                if (_configuracion.TelefonoClub != value)
                {
                    _configuracion.TelefonoClub = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EmailClub
        {
            get => _configuracion.EmailClub;
            set
            {
                if (_configuracion.EmailClub != value)
                {
                    _configuracion.EmailClub = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SitioWebClub
        {
            get => _configuracion.WebClub;
            set
            {
                if (_configuracion.WebClub != value)
                {
                    _configuracion.WebClub = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LogoClub
        {
            get => _configuracion.LogoClub;
            set
            {
                if (_configuracion.LogoClub != value)
                {
                    _configuracion.LogoClub = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasLogo));
                    OnPropertyChanged(nameof(NoLogo));
                    OnPropertyChanged(nameof(CanRemoveLogo));
                }
            }
        }

        public string RutaEscudo
        {
            get => _configuracion.RutaEscudo;
            set
            {
                if (_configuracion.RutaEscudo != value)
                {
                    _configuracion.RutaEscudo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasEscudo));
                    OnPropertyChanged(nameof(NoEscudo));
                    OnPropertyChanged(nameof(CanRemoveEscudo));
                }
            }
        }

        public bool AutoBackup
        {
            get => _configuracion.AutoBackup;
            set
            {
                if (_configuracion.AutoBackup != value)
                {
                    _configuracion.AutoBackup = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ConfirmarEliminaciones
        {
            get => _configuracion.ConfirmarEliminaciones;
            set
            {
                if (_configuracion.ConfirmarEliminaciones != value)
                {
                    _configuracion.ConfirmarEliminaciones = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool MostrarAyudas
        {
            get => _configuracion.MostrarAyudas;
            set
            {
                if (_configuracion.MostrarAyudas != value)
                {
                    _configuracion.MostrarAyudas = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool NumeracionAutomatica
        {
            get => _configuracion.NumeracionAutomatica;
            set
            {
                if (_configuracion.NumeracionAutomatica != value)
                {
                    _configuracion.NumeracionAutomatica = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormatoNumeroSocio
        {
            get => _configuracion.FormatoNumeroSocio;
            set
            {
                if (_configuracion.FormatoNumeroSocio != value)
                {
                    _configuracion.FormatoNumeroSocio = value;
                    OnPropertyChanged();
                }
            }
        }

        // Propiedades de UI
        public string HasLogo => !string.IsNullOrEmpty(LogoClub) ? "Visible" : "Collapsed";
        public string NoLogo => string.IsNullOrEmpty(LogoClub) ? "Visible" : "Collapsed";
        public bool CanRemoveLogo => !string.IsNullOrEmpty(LogoClub);

        public string HasEscudo => !string.IsNullOrEmpty(RutaEscudo) ? "Visible" : "Collapsed";
        public string NoEscudo => string.IsNullOrEmpty(RutaEscudo) ? "Visible" : "Collapsed";
        public bool CanRemoveEscudo => !string.IsNullOrEmpty(RutaEscudo);

        // Información de licencia
        private string _licenseStatus = "";
        private string _licenseExpiration = "";

        public string LicenseStatus
        {
            get => _licenseStatus;
            set => SetProperty(ref _licenseStatus, value);
        }

        public string LicenseExpiration
        {
            get => _licenseExpiration;
            set => SetProperty(ref _licenseExpiration, value);
        }

        #endregion

        #region Commands

        public ICommand GuardarConfiguracionCommand { get; }
        public ICommand RestaurarConfiguracionCommand { get; }
        public ICommand ExportarConfiguracionCommand { get; }
        public ICommand ImportarDatosCommand { get; } // ✅ NUEVO COMANDO
        public ICommand SelectLogoCommand { get; }
        public ICommand RemoveLogoCommand { get; }
        public ICommand SelectEscudoCommand { get; }
        public ICommand RemoveEscudoCommand { get; }
        public ICommand ShowLicenseInfoCommand { get; }
        public ICommand ChangeLicenseCommand { get; }

        #endregion

        #region Methods

        private void GuardarConfiguracion()
        {
            try
            {
                _configuracion.FechaModificacion = DateTime.Now;
                _configuracionService.SaveConfiguracion(_configuracion);

                MessageBox.Show("Configuración guardada correctamente.", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestaurarConfiguracion()
        {
            var result = MessageBox.Show("¿Está seguro de que desea restaurar la configuración predeterminada?\n" +
                                        "Se perderán todos los cambios actuales.", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _configuracion = new Configuracion();
                RefreshAllProperties();

                MessageBox.Show("Configuración restaurada a valores predeterminados.", "Información",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportarConfiguracion()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Archivos de Configuración (*.json)|*.json|Todos los archivos (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"ConfiguracionClub_{DateTime.Now:yyyyMMdd}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    _configuracionService.ExportConfiguracion(_configuracion, saveDialog.FileName);
                    MessageBox.Show("Configuración exportada correctamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar configuración: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ MÉTODO REAL: Importar Datos CSV (versión con debug)
        private void ImportarDatos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ImportarDatos iniciado");

                // Paso 1: Verificar ventana principal
                if (Application.Current?.MainWindow == null)
                {
                    MessageBox.Show("No se puede encontrar la ventana principal.", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                System.Diagnostics.Debug.WriteLine("DEBUG: Ventana principal OK");

                // Paso 2: Intentar crear la ventana real
                System.Diagnostics.Debug.WriteLine("DEBUG: Creando CsvImportWindow...");

                var importWindow = new CsvImportWindow();
                System.Diagnostics.Debug.WriteLine("DEBUG: CsvImportWindow creada exitosamente");

                importWindow.Owner = Application.Current.MainWindow;
                System.Diagnostics.Debug.WriteLine("DEBUG: Owner establecido");

                // Paso 3: Mostrar ventana
                System.Diagnostics.Debug.WriteLine("DEBUG: Mostrando ventana...");
                var result = importWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"DEBUG: Ventana cerrada con resultado: {result}");

                if (result == true)
                {
                    MessageBox.Show("Datos importados correctamente desde CSV.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }

                System.Diagnostics.Debug.WriteLine("DEBUG: ImportarDatos completado exitosamente");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error en ImportarDatos:\n\n" +
                              $"Mensaje: {ex.Message}\n\n" +
                              $"Tipo: {ex.GetType().Name}\n\n" +
                              $"InnerException: {ex.InnerException?.Message}\n\n" +
                              $"Stack Trace:\n{ex.StackTrace}";

                System.Diagnostics.Debug.WriteLine($"DEBUG ERROR: {errorMsg}");

                MessageBox.Show(errorMsg, "Error Detallado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectLogo()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Archivos de Imagen (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar Logo del Club"
            };

            if (openDialog.ShowDialog() == true)
            {
                LogoClub = openDialog.FileName;
            }
        }

        private void RemoveLogo()
        {
            var result = MessageBox.Show("¿Está seguro de que desea quitar el logo?", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LogoClub = "";
            }
        }

        private void SelectEscudo()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Archivos de Imagen (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar Escudo del Club"
            };

            if (openDialog.ShowDialog() == true)
            {
                RutaEscudo = openDialog.FileName;
            }
        }

        private void RemoveEscudo()
        {
            var result = MessageBox.Show("¿Está seguro de que desea quitar el escudo?", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RutaEscudo = "";
            }
        }

        private void ShowLicenseInfo()
        {
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();

            string message = $"Estado de la Licencia:\n\n" +
                           $"Estado: {licenseInfo.Status}\n" +
                           $"Club: {licenseInfo.ClubName}\n" +
                           $"Válida: {(licenseInfo.IsValid ? "Sí" : "No")}\n" +
                           $"Expiración: {licenseInfo.ExpirationText}\n" +
                           $"Tiempo restante: {licenseInfo.TimeRemaining}";

            if (!string.IsNullOrEmpty(licenseInfo.ErrorMessage))
            {
                message += $"\n\nError: {licenseInfo.ErrorMessage}";
            }

            MessageBox.Show(message, "Información de Licencia",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChangeLicense()
        {
            try
            {
                var licenseWindow = new Views.LicenseWindow();
                if (licenseWindow.ShowDialog() == true)
                {
                    LoadLicenseInfo();
                    MessageBox.Show("Licencia actualizada correctamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar la licencia: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLicenseInfo()
        {
            try
            {
                var licenseInfo = _licenseService.GetCurrentLicenseInfo();
                LicenseStatus = licenseInfo.Status;
                LicenseExpiration = licenseInfo.ExpirationText;
            }
            catch
            {
                LicenseStatus = "Error";
                LicenseExpiration = "No disponible";
            }
        }

        private void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(NombreClub));
            OnPropertyChanged(nameof(TemporadaActual));
            OnPropertyChanged(nameof(DireccionClub));
            OnPropertyChanged(nameof(TelefonoClub));
            OnPropertyChanged(nameof(EmailClub));
            OnPropertyChanged(nameof(SitioWebClub));
            OnPropertyChanged(nameof(LogoClub));
            OnPropertyChanged(nameof(RutaEscudo));
            OnPropertyChanged(nameof(AutoBackup));
            OnPropertyChanged(nameof(ConfirmarEliminaciones));
            OnPropertyChanged(nameof(MostrarAyudas));
            OnPropertyChanged(nameof(NumeracionAutomatica));
            OnPropertyChanged(nameof(FormatoNumeroSocio));
        }

        #endregion
    }
}