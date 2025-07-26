using ClubManager.Commands;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClubManager.ViewModels
{
    public class ConfiguracionViewModel : BaseViewModel
    {
        private readonly IConfiguracionService _configuracionService;
        private readonly ILicenseService _licenseService;
        private readonly ICardPrintService _printService;
        private Configuracion _configuracion;

        // Propiedades para impresión
        private ObservableCollection<string> _impresorasDisponibles;
        private ObservableCollection<TamañoPapelItem> _tamañosPapel;
        private ObservableCollection<CalidadImpresionItem> _calidadesImpresion;
        private ObservableCollection<FormatoNumeroSocioItem> _formatosNumeroSocio;

        public ConfiguracionViewModel()
        {
            _configuracionService = new ConfiguracionService();
            _licenseService = new LicenseService();
            _printService = new CardPrintService(new TemplateService(), _configuracionService);

            // Cargar configuración actual
            _configuracion = _configuracionService.GetConfiguracion() ?? new Configuracion { Id = 1 };

            // Inicializar colecciones
            _impresorasDisponibles = new ObservableCollection<string>();
            _tamañosPapel = new ObservableCollection<TamañoPapelItem>(ConfiguracionImpresionTarjetas.TamañosPapelDisponibles);
            _calidadesImpresion = new ObservableCollection<CalidadImpresionItem>(ConfiguracionImpresionTarjetas.CalidadesDisponibles);
            _formatosNumeroSocio = new ObservableCollection<FormatoNumeroSocioItem>(Configuracion.FormatosDisponibles);

            // Cargar impresoras disponibles
            CargarImpresorasDisponibles();

            // Commands
            InicializarComandos();

            // Cargar información de licencia
            LoadLicenseInfo();
        }

        private void InicializarComandos()
        {
            GuardarConfiguracionCommand = new RelayCommand(GuardarConfiguracion);
            RestaurarConfiguracionCommand = new RelayCommand(RestaurarConfiguracion);
            ExportarConfiguracionCommand = new RelayCommand(ExportarConfiguracion);
            ImportarDatosCommand = new RelayCommand(ImportarDatos);
            SelectLogoCommand = new RelayCommand(SelectLogo);
            RemoveLogoCommand = new RelayCommand(RemoveLogo);
            SelectEscudoCommand = new RelayCommand(SelectEscudo);
            RemoveEscudoCommand = new RelayCommand(RemoveEscudo);
            ShowLicenseInfoCommand = new RelayCommand(ShowLicenseInfo);
            ChangeLicenseCommand = new RelayCommand(ChangeLicense);

            // Nuevos comandos para impresión
            ProbarImpresoraCommand = new RelayCommand(ProbarImpresora, () => !string.IsNullOrEmpty(ImpresoraPredeterminada));
            RefrescarImpresorasCommand = new RelayCommand(CargarImpresorasDisponibles);
            ConfigurarRutaCopiasCommand = new RelayCommand(ConfigurarRutaCopias);
            TestImpresionCommand = new RelayCommand(TestImpresion, () => !string.IsNullOrEmpty(ImpresoraPredeterminada));
        }

        #region Properties Básicas

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

        #endregion

        #region Properties de Impresión

        public ObservableCollection<string> ImpresorasDisponibles => _impresorasDisponibles;
        public ObservableCollection<TamañoPapelItem> TamañosPapel => _tamañosPapel;
        public ObservableCollection<CalidadImpresionItem> CalidadesImpresion => _calidadesImpresion;
        public ObservableCollection<FormatoNumeroSocioItem> FormatosNumeroSocio => _formatosNumeroSocio;

        public string ImpresoraPredeterminada
        {
            get => _configuracion.ConfiguracionImpresion.ImpresoraPredeterminada;
            set
            {
                if (_configuracion.ConfiguracionImpresion.ImpresoraPredeterminada != value)
                {
                    _configuracion.ConfiguracionImpresion.ImpresoraPredeterminada = value;
                    OnPropertyChanged();
                }
            }
        }

        public TamañoPapelItem TamañoPapelSeleccionado
        {
            get => _tamañosPapel.FirstOrDefault(t => t.Value == _configuracion.ConfiguracionImpresion.TamañoPapel) ?? _tamañosPapel.First();
            set
            {
                if (value != null && _configuracion.ConfiguracionImpresion.TamañoPapel != value.Value)
                {
                    _configuracion.ConfiguracionImpresion.TamañoPapel = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public CalidadImpresionItem CalidadSeleccionada
        {
            get => _calidadesImpresion.FirstOrDefault(c => c.Value == _configuracion.ConfiguracionImpresion.Calidad) ?? _calidadesImpresion.First();
            set
            {
                if (value != null && _configuracion.ConfiguracionImpresion.Calidad != value.Value)
                {
                    _configuracion.ConfiguracionImpresion.Calidad = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public FormatoNumeroSocioItem FormatoNumeroSocioSeleccionado
        {
            get => _formatosNumeroSocio.FirstOrDefault(f => f.Value == _configuracion.FormatoNumeroSocio) ?? _formatosNumeroSocio.First();
            set
            {
                if (value != null && _configuracion.FormatoNumeroSocio != value.Value)
                {
                    _configuracion.FormatoNumeroSocio = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ImpresionColor
        {
            get => _configuracion.ConfiguracionImpresion.ImpresionColor;
            set
            {
                if (_configuracion.ConfiguracionImpresion.ImpresionColor != value)
                {
                    _configuracion.ConfiguracionImpresion.ImpresionColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TarjetasPorPagina
        {
            get => _configuracion.ConfiguracionImpresion.TarjetasPorPagina;
            set
            {
                if (_configuracion.ConfiguracionImpresion.TarjetasPorPagina != value)
                {
                    _configuracion.ConfiguracionImpresion.TarjetasPorPagina = Math.Max(1, Math.Min(20, value));
                    OnPropertyChanged();
                }
            }
        }

        public double EspaciadoHorizontal
        {
            get => _configuracion.ConfiguracionImpresion.EspaciadoHorizontal;
            set
            {
                if (Math.Abs(_configuracion.ConfiguracionImpresion.EspaciadoHorizontal - value) > 0.1)
                {
                    _configuracion.ConfiguracionImpresion.EspaciadoHorizontal = Math.Max(0, Math.Min(50, value));
                    OnPropertyChanged();
                }
            }
        }

        public double EspaciadoVertical
        {
            get => _configuracion.ConfiguracionImpresion.EspaciadoVertical;
            set
            {
                if (Math.Abs(_configuracion.ConfiguracionImpresion.EspaciadoVertical - value) > 0.1)
                {
                    _configuracion.ConfiguracionImpresion.EspaciadoVertical = Math.Max(0, Math.Min(50, value));
                    OnPropertyChanged();
                }
            }
        }

        public bool MarcarComoImpresoAutomaticamente
        {
            get => _configuracion.ConfiguracionImpresion.MarcarComoImpresoAutomaticamente;
            set
            {
                if (_configuracion.ConfiguracionImpresion.MarcarComoImpresoAutomaticamente != value)
                {
                    _configuracion.ConfiguracionImpresion.MarcarComoImpresoAutomaticamente = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool MostrarVistaPrevia
        {
            get => _configuracion.ConfiguracionImpresion.MostrarVistaPrevia;
            set
            {
                if (_configuracion.ConfiguracionImpresion.MostrarVistaPrevia != value)
                {
                    _configuracion.ConfiguracionImpresion.MostrarVistaPrevia = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool GuardarCopiaDigital
        {
            get => _configuracion.ConfiguracionImpresion.GuardarCopiaDigital;
            set
            {
                if (_configuracion.ConfiguracionImpresion.GuardarCopiaDigital != value)
                {
                    _configuracion.ConfiguracionImpresion.GuardarCopiaDigital = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MostrarRutaCopias));
                }
            }
        }

        public string RutaCopiasDigitales
        {
            get => _configuracion.ConfiguracionImpresion.RutaCopiasDigitales;
            set
            {
                if (_configuracion.ConfiguracionImpresion.RutaCopiasDigitales != value)
                {
                    _configuracion.ConfiguracionImpresion.RutaCopiasDigitales = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Properties de UI

        public string HasLogo => !string.IsNullOrEmpty(LogoClub) ? "Visible" : "Collapsed";
        public string NoLogo => string.IsNullOrEmpty(LogoClub) ? "Visible" : "Collapsed";
        public bool CanRemoveLogo => !string.IsNullOrEmpty(LogoClub);

        public string HasEscudo => !string.IsNullOrEmpty(RutaEscudo) ? "Visible" : "Collapsed";
        public string NoEscudo => string.IsNullOrEmpty(RutaEscudo) ? "Visible" : "Collapsed";
        public bool CanRemoveEscudo => !string.IsNullOrEmpty(RutaEscudo);

        public bool MostrarRutaCopias => GuardarCopiaDigital;

        // Información de licencia
        private string _licenseState = "";
        private string _licenseClubName = "";
        private string _licenseExpiration = "";

        public string LicenseState
        {
            get => _licenseState;
            set => SetProperty(ref _licenseState, value);
        }

        public string LicenseClubName
        {
            get => _licenseClubName;
            set => SetProperty(ref _licenseClubName, value);
        }

        public string LicenseExpiration
        {
            get => _licenseExpiration;
            set => SetProperty(ref _licenseExpiration, value);
        }

        #endregion

        #region Commands

        public ICommand GuardarConfiguracionCommand { get; private set; }
        public ICommand RestaurarConfiguracionCommand { get; private set; }
        public ICommand ExportarConfiguracionCommand { get; private set; }
        public ICommand ImportarDatosCommand { get; private set; }
        public ICommand SelectLogoCommand { get; private set; }
        public ICommand RemoveLogoCommand { get; private set; }
        public ICommand SelectEscudoCommand { get; private set; }
        public ICommand RemoveEscudoCommand { get; private set; }
        public ICommand ShowLicenseInfoCommand { get; private set; }
        public ICommand ChangeLicenseCommand { get; private set; }

        // Comandos de impresión
        public ICommand ProbarImpresoraCommand { get; private set; }
        public ICommand RefrescarImpresorasCommand { get; private set; }
        public ICommand ConfigurarRutaCopiasCommand { get; private set; }
        public ICommand TestImpresionCommand { get; private set; }

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
                _configuracion = new Configuracion { Id = 1 };
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

        private void ImportarDatos()
        {
            try
            {
                var importWindow = new CsvImportWindow();
                importWindow.Owner = Application.Current.MainWindow;

                var result = importWindow.ShowDialog();

                if (result == true)
                {
                    MessageBox.Show("Datos importados correctamente desde CSV.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al importar datos: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
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

        #endregion

        #region Métodos de Impresión

        private void CargarImpresorasDisponibles()
        {
            try
            {
                var impresoras = _printService.GetImpresorasDisponibles();
                _impresorasDisponibles.Clear();

                if (!impresoras.Any())
                {
                    _impresorasDisponibles.Add("No se encontraron impresoras");
                }
                else
                {
                    foreach (var impresora in impresoras)
                    {
                        _impresorasDisponibles.Add(impresora);
                    }
                }

                // Actualizar la configuración con impresoras disponibles
                _configuracion.ConfiguracionImpresion.ImpresorasDisponibles = impresoras;

                OnPropertyChanged(nameof(ImpresorasDisponibles));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar impresoras: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ProbarImpresora()
        {
            try
            {
                if (string.IsNullOrEmpty(ImpresoraPredeterminada))
                {
                    MessageBox.Show("Seleccione una impresora primero.", "Información",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var esValida = await _printService.ValidarImpresoraAsync(ImpresoraPredeterminada);

                if (esValida)
                {
                    MessageBox.Show($"✅ La impresora '{ImpresoraPredeterminada}' está disponible y funcionando correctamente.",
                                   "Impresora Válida", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ La impresora '{ImpresoraPredeterminada}' no está disponible o tiene problemas.\n\n" +
                                   "Verifique que esté encendida, conectada y sin errores.",
                                   "Problema con Impresora", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al probar impresora: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurarRutaCopias()
        {
            try
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Seleccionar carpeta para guardar copias digitales de tarjetas",
                    SelectedPath = RutaCopiasDigitales
                };

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    RutaCopiasDigitales = folderDialog.SelectedPath;
                    MessageBox.Show($"Ruta configurada: {RutaCopiasDigitales}", "Ruta Configurada",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al configurar ruta: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestImpresion()
        {
            try
            {
                if (string.IsNullOrEmpty(ImpresoraPredeterminada))
                {
                    MessageBox.Show("Seleccione una impresora primero.", "Información",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"¿Desea imprimir una tarjeta de prueba en '{ImpresoraPredeterminada}'?\n\n" +
                    "Se imprimirá una tarjeta con datos de ejemplo para verificar la configuración.",
                    "Confirmar Impresión de Prueba",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                // Crear abonado de prueba
                var abonadoPrueba = new Abonado
                {
                    NumeroSocio = 99999,
                    Nombre = "PRUEBA",
                    Apellidos = "IMPRESIÓN",
                    DNI = "00000000T",
                    Estado = EstadoAbonado.Activo,
                    FechaCreacion = DateTime.Now,
                    CodigoBarras = "TEST123456789"
                };

                // Crear plantilla de prueba básica
                var plantillaPrueba = new PlantillaTarjeta
                {
                    Nombre = "Plantilla de Prueba",
                    Ancho = 350,
                    Alto = 220,
                    Elementos = new List<ElementoTarjeta>
                    {
                        new ElementoTexto
                        {
                            Texto = "TARJETA DE PRUEBA",
                            X = 50, Y = 20, Ancho = 250, Alto = 30,
                            FontSize = 16, IsBold = true,
                            Color = Colors.DarkBlue
                        },
                        new ElementoTexto
                        {
                            Texto = $"Impresora: {ImpresoraPredeterminada}",
                            X = 20, Y = 60, Ancho = 310, Alto = 20,
                            FontSize = 12
                        },
                        new ElementoTexto
                        {
                            Texto = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}",
                            X = 20, Y = 85, Ancho = 310, Alto = 20,
                            FontSize = 12
                        }
                    }
                };

                // Configuración de impresión
                var configuracionImpresion = new ConfiguracionImpresionEspecifica
                {
                    NombreImpresora = ImpresoraPredeterminada,
                    TamañoPapel = TamañoPapelSeleccionado.Value,
                    ImpresionColor = ImpresionColor,
                    Calidad = CalidadSeleccionada.Value,
                    Copias = 1,
                    MostrarDialogoImpresion = MostrarVistaPrevia,
                    GuardarCopia = GuardarCopiaDigital,
                    RutaCopia = RutaCopiasDigitales
                };

                // Imprimir
                var resultado = await _printService.ImprimirTarjetaAsync(abonadoPrueba, plantillaPrueba, configuracionImpresion);

                if (resultado.Exitoso)
                {
                    MessageBox.Show($"✅ Prueba de impresión exitosa!\n\n" +
                                   $"• Impresora: {resultado.ImpresoraUtilizada}\n" +
                                   $"• Tiempo: {resultado.TiempoTranscurrido.TotalSeconds:F1} segundos\n" +
                                   $"• Tarjetas impresas: {resultado.TarjetasImpresas}",
                                   "Prueba Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var errores = string.Join("\n• ", resultado.Errores);
                    MessageBox.Show($"❌ Error en la prueba de impresión:\n\n• {errores}",
                                   "Error en Prueba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la prueba de impresión: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Métodos de Licencia

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
                var licenseWindow = new LicenseWindow();
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
                LicenseState = licenseInfo.Status;
                LicenseClubName = licenseInfo.ClubName;
                LicenseExpiration = licenseInfo.ExpirationText;
            }
            catch
            {
                LicenseState = "Error";
                LicenseClubName = "No disponible";
                LicenseExpiration = "No disponible";
            }
        }

        #endregion

        #region Métodos Auxiliares

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

            // Propiedades de impresión
            OnPropertyChanged(nameof(ImpresoraPredeterminada));
            OnPropertyChanged(nameof(TamañoPapelSeleccionado));
            OnPropertyChanged(nameof(CalidadSeleccionada));
            OnPropertyChanged(nameof(ImpresionColor));
            OnPropertyChanged(nameof(TarjetasPorPagina));
            OnPropertyChanged(nameof(EspaciadoHorizontal));
            OnPropertyChanged(nameof(EspaciadoVertical));
            OnPropertyChanged(nameof(MarcarComoImpresoAutomaticamente));
            OnPropertyChanged(nameof(MostrarVistaPrevia));
            OnPropertyChanged(nameof(GuardarCopiaDigital));
            OnPropertyChanged(nameof(RutaCopiasDigitales));
        }

        #endregion
    }
}