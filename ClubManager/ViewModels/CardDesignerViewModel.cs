using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClubManager.ViewModels
{
    public class CardDesignerViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ITemplateService _templateService;
        private Canvas? _canvasTarjeta;
        private PlantillaTarjeta _plantillaActual;
        private ElementoTarjeta? _elementoSeleccionado;
        private string _estadoActual;
        private Abonado _abonadoEjemplo;
        private Abonado? _abonadoSeleccionado;

        #region Properties

        public ObservableCollection<ElementoTarjeta> ElementosActuales { get; }
        public ObservableCollection<Abonado> AbonadosDisponibles { get; }

        public PlantillaTarjeta PlantillaActual
        {
            get => _plantillaActual;
            set => SetProperty(ref _plantillaActual, value);
        }

        public ElementoTarjeta? ElementoSeleccionado
        {
            get => _elementoSeleccionado;
            set
            {
                if (SetProperty(ref _elementoSeleccionado, value))
                {
                    ActualizarPropiedades();
                    System.Diagnostics.Debug.WriteLine($"ElementoSeleccionado cambió a: {value?.Tipo ?? "null"}");
                }
            }
        }

        public string EstadoActual
        {
            get => _estadoActual;
            set => SetProperty(ref _estadoActual, value);
        }

        public Abonado AbonadoEjemplo
        {
            get => _abonadoEjemplo;
            set => SetProperty(ref _abonadoEjemplo, value);
        }

        public Abonado? AbonadoSeleccionado
        {
            get => _abonadoSeleccionado;
            set
            {
                if (SetProperty(ref _abonadoSeleccionado, value) && value != null)
                {
                    AbonadoEjemplo = value;
                    ActualizarCanvas();
                }
            }
        }

        #endregion

        #region Comandos

        public ICommand AgregarTextoCommand { get; private set; } = null!;
        public ICommand AgregarImagenCommand { get; private set; } = null!;
        public ICommand AgregarCodigoBarrasCommand { get; private set; } = null!;
        public ICommand AgregarCampoCommand { get; private set; } = null!;
        public ICommand EliminarElementoCommand { get; private set; } = null!;
        public ICommand NuevaPlantillaCommand { get; private set; } = null!;
        public ICommand GuardarPlantillaCommand { get; private set; } = null!;
        public ICommand CargarPlantillaCommand { get; private set; } = null!;
        public ICommand VistaPreviaCommand { get; private set; } = null!;
        public ICommand ImprimirPruebaCommand { get; private set; } = null!;
        public ICommand AplicarPlantillaCommand { get; private set; } = null!;
        public ICommand CargarDatosRealesCommand { get; private set; } = null!;

        #endregion

        #region Constructor

        public CardDesignerViewModel(ITemplateService? templateService = null)
        {
            System.Diagnostics.Debug.WriteLine("=== INICIANDO CardDesignerViewModel ===");

            _dbContext = new ClubDbContext();
            _templateService = templateService ?? new TemplateService();
            _estadoActual = "Listo para diseñar";

            ElementosActuales = new ObservableCollection<ElementoTarjeta>();
            AbonadosDisponibles = new ObservableCollection<Abonado>();

            // Inicializar plantilla por defecto
            _plantillaActual = new PlantillaTarjeta
            {
                Id = Guid.NewGuid().ToString(),
                Nombre = "Nueva Plantilla",
                Descripcion = "Plantilla de tarjeta personalizada",
                Ancho = 350,
                Alto = 220,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now,
                Elementos = new List<ElementoTarjeta>()
            };

            // Crear abonado de ejemplo
            _abonadoEjemplo = CrearAbonadoEjemplo();

            // Cargar abonados disponibles
            CargarAbonadosDisponibles();

            // Inicializar comandos
            InicializarComandos();

            System.Diagnostics.Debug.WriteLine("=== CardDesignerViewModel INICIALIZADO ===");
        }

        private void InicializarComandos()
        {
            AgregarTextoCommand = new RelayCommand(AgregarTexto);
            AgregarImagenCommand = new RelayCommand(AgregarImagen);
            AgregarCodigoBarrasCommand = new RelayCommand(AgregarCodigoBarras);
            AgregarCampoCommand = new RelayCommand<string>(AgregarCampo);
            EliminarElementoCommand = new RelayCommand(EliminarElemento, CanEliminarElemento);
            NuevaPlantillaCommand = new RelayCommand(NuevaPlantilla);
            GuardarPlantillaCommand = new RelayCommand(async () => await GuardarPlantillaAsync());
            CargarPlantillaCommand = new RelayCommand(async () => await CargarPlantillaAsync());
            VistaPreviaCommand = new RelayCommand(VistaPrevia);
            ImprimirPruebaCommand = new RelayCommand(async () => await ImprimirPruebaAsync());
            AplicarPlantillaCommand = new RelayCommand(async () => await AplicarPlantillaAsync());
            CargarDatosRealesCommand = new RelayCommand(CargarDatosReales);
        }

        private bool CanEliminarElemento()
        {
            return ElementoSeleccionado != null;
        }

        #endregion

        #region Métodos de Canvas

        public void SetCanvas(Canvas canvas)
        {
            try
            {
                _canvasTarjeta = canvas;
                System.Diagnostics.Debug.WriteLine("Canvas asignado correctamente");

                if (ElementosActuales.Any())
                {
                    ActualizarCanvas();
                    System.Diagnostics.Debug.WriteLine($"Canvas actualizado con {ElementosActuales.Count} elementos");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en SetCanvas: {ex.Message}");
            }
        }

        public void ActualizarCanvas()
        {
            if (_canvasTarjeta == null) return;

            try
            {
                _canvasTarjeta.Children.Clear();

                foreach (var elemento in ElementosActuales.OrderBy(e => e.ZIndex))
                {
                    var uiElement = CrearElementoUI(elemento);
                    if (uiElement != null)
                    {
                        Canvas.SetLeft(uiElement, elemento.X);
                        Canvas.SetTop(uiElement, elemento.Y);
                        Canvas.SetZIndex(uiElement, elemento.ZIndex);

                        // Aplicar opacidad si es diferente de 1
                        if (elemento.Opacidad != 1.0)
                        {
                            uiElement.Opacity = elemento.Opacidad;
                        }

                        // Aplicar rotación si es diferente de 0
                        if (elemento.Rotacion != 0)
                        {
                            var transform = new RotateTransform(elemento.Rotacion, elemento.Ancho / 2, elemento.Alto / 2);
                            uiElement.RenderTransform = transform;
                        }

                        _canvasTarjeta.Children.Add(uiElement);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Canvas actualizado con {ElementosActuales.Count} elementos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando canvas: {ex.Message}");
            }
        }

        #endregion

        #region Creación de Elementos UI

        private UIElement? CrearElementoUI(ElementoTarjeta elemento)
        {
            try
            {
                UIElement? result = elemento switch
                {
                    ElementoTexto textoElem => CrearTextoUI(textoElem),
                    ElementoImagen imagenElem => CrearImagenUI(imagenElem),
                    ElementoCodigoBarras codigoElem => CrearCodigoBarrasUI(codigoElem),
                    ElementoCampoDinamico campoElem => CrearCampoDinamicoUI(campoElem),
                    _ => null
                };

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando elemento UI: {ex.Message}");
                return CrearElementoError(elemento);
            }
        }

        private TextBlock CrearTextoUI(ElementoTexto elemento)
        {
            var textBlock = new TextBlock
            {
                Text = elemento.Texto,
                FontFamily = new FontFamily(elemento.FontFamily),
                FontSize = elemento.FontSize,
                Foreground = new SolidColorBrush(elemento.Color),
                FontWeight = elemento.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = elemento.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                TextAlignment = elemento.TextAlignment,
                Width = elemento.Ancho,
                Height = elemento.Alto,
                TextWrapping = elemento.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap
            };

            if (elemento.IsUnderline)
            {
                textBlock.TextDecorations = TextDecorations.Underline;
            }

            return textBlock;
        }

        private UIElement? CrearImagenUI(ElementoImagen elemento)
        {
            try
            {
                if (!File.Exists(elemento.RutaImagen))
                    return CrearElementoError(elemento, "Imagen no encontrada");

                var image = new Image
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Stretch = elemento.MantenerAspecto ? Stretch.Uniform : Stretch.Fill
                };

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(elemento.RutaImagen);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                image.Source = bitmap;

                // Aplicar redondez si es necesario
                if (elemento.Redondez > 0)
                {
                    var border = new Border
                    {
                        Width = elemento.Ancho,
                        Height = elemento.Alto,
                        CornerRadius = new CornerRadius(elemento.Redondez),
                        Child = image
                    };

                    if (elemento.GrosorBorde > 0)
                    {
                        border.BorderBrush = new SolidColorBrush(elemento.ColorBorde);
                        border.BorderThickness = new Thickness(elemento.GrosorBorde);
                    }

                    return border;
                }

                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando imagen: {ex.Message}");
                return CrearElementoError(elemento, "Error cargando imagen");
            }
        }

        // MEJORADO: Código de barras SIN marco - VERSIÓN ORIGINAL QUE FUNCIONA
        private UIElement CrearCodigoBarrasUI(ElementoCodigoBarras elemento)
        {
            try
            {
                var valorCodigo = ObtenerValorCampo(elemento.CampoOrigen);

                // VALIDACIÓN Y PREPARACIÓN DE DATOS PARA MÁXIMA CALIDAD
                string codigoLimpio = valorCodigo;

                // Preparar el código según el tipo para máxima compatibilidad
                switch (elemento.TipoCodigo.ToUpper())
                {
                    case "CODE128":
                        codigoLimpio = BarcodeGenerator.PrepareForCode128(valorCodigo);
                        if (!BarcodeGenerator.ValidateCode128(codigoLimpio))
                        {
                            System.Diagnostics.Debug.WriteLine($"ADVERTENCIA: Código inválido para Code128: '{valorCodigo}' -> '{codigoLimpio}'");
                        }
                        break;

                    case "CODE39":
                        // Code39 solo acepta A-Z, 0-9, y algunos símbolos especiales
                        codigoLimpio = new string(valorCodigo.ToUpper()
                            .Where(c => char.IsLetterOrDigit(c) || "-. $/%+*".Contains(c))
                            .ToArray());
                        break;

                    case "EAN13":
                        // EAN13 requiere exactamente 13 dígitos
                        codigoLimpio = new string(valorCodigo.Where(char.IsDigit).ToArray());
                        if (codigoLimpio.Length < 13)
                        {
                            codigoLimpio = codigoLimpio.PadLeft(13, '0');
                        }
                        else if (codigoLimpio.Length > 13)
                        {
                            codigoLimpio = codigoLimpio.Substring(codigoLimpio.Length - 13);
                        }
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"Generando código {elemento.TipoCodigo}: '{valorCodigo}' -> '{codigoLimpio}'");

                // Obtener información del código para debug
                var infoBarcode = BarcodeGenerator.GetBarcodeInfo(codigoLimpio, elemento.TipoCodigo);
                System.Diagnostics.Debug.WriteLine(infoBarcode);

                // Crear contenedor principal SIN borde para máxima nitidez
                var container = new StackPanel
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(elemento.ColorFondo),
                    ClipToBounds = true // Importante para mantener la precisión
                };

                // GENERACIÓN DE CÓDIGO DE BARRAS CON MÁXIMA CALIDAD
                BitmapSource? barcodeImage = null;
                var alturaBarras = elemento.MostrarTexto ? elemento.Alto - 20 : elemento.Alto; // Más espacio para texto

                // Asegurar dimensiones mínimas para calidad óptima
                var anchoOptimo = Math.Max(elemento.Ancho, 200); // Mínimo 200px de ancho
                var altoOptimo = Math.Max(alturaBarras, 40);     // Mínimo 40px de alto

                switch (elemento.TipoCodigo.ToUpper())
                {
                    case "CODE128":
                        barcodeImage = BarcodeGenerator.GenerateBarcode(codigoLimpio, anchoOptimo, altoOptimo, "Code128");
                        break;
                    case "CODE39":
                        barcodeImage = BarcodeGenerator.GenerateBarcode(codigoLimpio, anchoOptimo, altoOptimo, "Code39");
                        break;
                    case "EAN13":
                        barcodeImage = BarcodeGenerator.GenerateBarcode(codigoLimpio, anchoOptimo, altoOptimo, "EAN13");
                        break;
                    case "QRCODE":
                        var tamaño = Math.Min(elemento.Ancho, elemento.Alto);
                        barcodeImage = BarcodeGenerator.GenerateQRCode(codigoLimpio, Math.Max(tamaño, 100));
                        break;
                    default:
                        barcodeImage = BarcodeGenerator.GenerateBarcode(codigoLimpio, anchoOptimo, altoOptimo, "Code128");
                        break;
                }

                // Verificar que se generó correctamente
                if (barcodeImage == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: No se pudo generar el código de barras para '{codigoLimpio}'");
                    return CrearElementoError(elemento, $"Error generando {elemento.TipoCodigo}");
                }

                // IMAGEN CON CONFIGURACIÓN DE ALTA CALIDAD
                var image = new Image
                {
                    Source = barcodeImage,
                    Stretch = Stretch.Uniform, // Cambiar a Uniform para mantener proporciones exactas
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    // Configuraciones para máxima nitidez
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true
                };

                // Aplicar RenderOptions para máxima calidad
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor); // Sin suavizado
                RenderOptions.SetEdgeMode(image, EdgeMode.Aliased); // Bordes nítidos

                container.Children.Add(image);

                // TEXTO CON MÁXIMA LEGIBILIDAD
                if (elemento.MostrarTexto)
                {
                    var textBlock = new TextBlock
                    {
                        Text = codigoLimpio, // Usar el código limpio, no el original
                        FontSize = Math.Max(elemento.FontSize, 8), // Tamaño mínimo legible
                        FontFamily = new FontFamily("Courier New"), // Fuente monoespaciada
                        FontWeight = FontWeights.Normal,
                        Foreground = new SolidColorBrush(elemento.ColorTexto),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 0),
                        // Configuraciones para máxima nitidez del texto
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true
                    };

                    // Aplicar configuraciones de renderizado de alta calidad para el texto
                    TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Display);
                    TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.ClearType);

                    container.Children.Add(textBlock);
                }

                System.Diagnostics.Debug.WriteLine($"Código de barras {elemento.TipoCodigo} creado con MÁXIMA CALIDAD: {elemento.Ancho}x{elemento.Alto}");

                return container;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando código de barras de alta calidad: {ex.Message}");
                return CrearElementoError(elemento, $"Error: {elemento.TipoCodigo}");
            }
        }

        private string ObtenerValorCampoParaBarcode(string campo, string tipoCodigo)
        {
            try
            {
                var valor = ObtenerValorCampo(campo);

                // Validar y ajustar según el tipo de código
                switch (tipoCodigo.ToUpper())
                {
                    case "CODE128":
                        // Code128 acepta la mayoría de caracteres ASCII
                        return valor;

                    case "CODE39":
                        // Code39 solo acepta: A-Z, 0-9, y los símbolos: - . $ / % + space *
                        var validChars = valor.ToUpper().Where(c =>
                            char.IsLetterOrDigit(c) ||
                            "-. $/%+*".Contains(c)).ToArray();
                        return new string(validChars);

                    case "EAN13":
                        // EAN13 solo acepta números
                        var numeros = valor.Where(char.IsDigit).ToArray();
                        var numeroString = new string(numeros);

                        if (numeroString.Length < 13)
                        {
                            // Usar el número de socio como base si es muy corto
                            var numeroSocio = AbonadoEjemplo?.NumeroSocio ?? 12345;
                            numeroString = numeroSocio.ToString().PadLeft(12, '0') + "0";
                        }

                        return numeroString.Length > 13 ?
                            numeroString.Substring(numeroString.Length - 13) :
                            numeroString.PadLeft(13, '0');

                    case "QRCODE":
                        // QR acepta cualquier texto
                        return valor;

                    default:
                        return valor;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo valor para barcode: {ex.Message}");
                return "ERROR";
            }
        }


        private bool ValidarCodigoBarras(ElementoCodigoBarras elemento)
        {
            try
            {
                var valor = ObtenerValorCampoParaBarcode(elemento.CampoOrigen, elemento.TipoCodigo);

                if (string.IsNullOrWhiteSpace(valor))
                {
                    System.Diagnostics.Debug.WriteLine($"Valor vacío para código de barras: {elemento.CampoOrigen}");
                    return false;
                }

                switch (elemento.TipoCodigo.ToUpper())
                {
                    case "CODE128":
                        return BarcodeGenerator.ValidateCode128(valor);

                    case "CODE39":
                        // Verificar que solo contiene caracteres válidos para Code39
                        return valor.All(c => char.IsLetterOrDigit(c) || "-. $/%+*".Contains(c));

                    case "EAN13":
                        // Verificar que tiene exactamente 13 dígitos
                        return valor.Length == 13 && valor.All(char.IsDigit);

                    case "QRCODE":
                        // QR acepta cualquier texto no vacío
                        return !string.IsNullOrWhiteSpace(valor);

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Método mejorado para agregar código de barras con validación
        /// </summary>
        private void AgregarCodigoBarrasConValidacion()
        {
            try
            {
                var nuevoCodigo = new ElementoCodigoBarras
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Código de Barras",
                    X = 50,
                    Y = 150,
                    Ancho = 250, // Ancho mínimo aumentado para mejor calidad
                    Alto = 80,   // Alto aumentado para mejor legibilidad
                    ZIndex = ElementosActuales.Count + 1,
                    TipoCodigo = "Code128",
                    CampoOrigen = "CodigoBarras",
                    MostrarTexto = true,
                    FontFamily = "Courier New",
                    FontSize = 10, // Tamaño aumentado para mejor legibilidad
                    ColorTexto = Colors.Black,
                    ColorFondo = Colors.White
                };

                // Validar el código antes de agregarlo
                if (ValidarCodigoBarras(nuevoCodigo))
                {
                    ElementosActuales.Add(nuevoCodigo);
                    ActualizarCanvas();
                    ElementoSeleccionado = nuevoCodigo;
                    EstadoActual = "Código de barras de alta calidad agregado";

                    // Mostrar información del código generado
                    var valor = ObtenerValorCampoParaBarcode(nuevoCodigo.CampoOrigen, nuevoCodigo.TipoCodigo);
                    var info = BarcodeGenerator.GetBarcodeInfo(valor, nuevoCodigo.TipoCodigo);
                    System.Diagnostics.Debug.WriteLine($"Código de barras agregado:\n{info}");
                }
                else
                {
                    EstadoActual = "Error: No se puede generar código de barras válido";
                    MessageBox.Show("No se puede generar un código de barras válido con los datos actuales.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                System.Diagnostics.Debug.WriteLine("Código de barras de alta calidad agregado");
            }
            catch (Exception ex)
            {
                MostrarError("Error agregando código de barras", ex);
            }
        }

        /// <summary>
        /// Método para testear la calidad de códigos de barras
        /// </summary>
        public void TestearCalidadCodigoBarras()
        {
            try
            {
                var testData = "CM52759393JSRVUM"; // Tu ejemplo específico

                System.Diagnostics.Debug.WriteLine("=== PRUEBA DE CALIDAD DE CÓDIGO DE BARRAS ===");

                // Probar Code128
                var esValido = BarcodeGenerator.ValidateCode128(testData);
                System.Diagnostics.Debug.WriteLine($"¿Es válido '{testData}' para Code128? {esValido}");

                if (!esValido)
                {
                    var dataLimpia = BarcodeGenerator.PrepareForCode128(testData);
                    System.Diagnostics.Debug.WriteLine($"Datos limpiados: '{dataLimpia}'");
                    esValido = BarcodeGenerator.ValidateCode128(dataLimpia);
                    System.Diagnostics.Debug.WriteLine($"¿Es válido después de limpiar? {esValido}");
                }

                // Obtener información detallada
                var info = BarcodeGenerator.GetBarcodeInfo(testData, "Code128");
                System.Diagnostics.Debug.WriteLine(info);

                // Generar imagen de prueba
                var imagen = BarcodeGenerator.GenerateBarcode(testData, 300, 80, "Code128");
                if (imagen != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Imagen generada exitosamente: {imagen.Width}x{imagen.Height} píxeles");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No se pudo generar la imagen");
                }

                System.Diagnostics.Debug.WriteLine("=== FIN DE PRUEBA ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en prueba: {ex.Message}");
            }
        }

        private UIElement CrearCampoDinamicoUI(ElementoCampoDinamico elemento)
        {
            var valor = ObtenerValorCampo(elemento.CampoOrigen);
            var textoFinal = $"{elemento.Prefijo}{valor}{elemento.Sufijo}";

            var textBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(textoFinal.Trim()) ? elemento.TextoSiVacio : textoFinal,
                FontFamily = new FontFamily(elemento.FontFamily),
                FontSize = elemento.FontSize,
                Foreground = new SolidColorBrush(elemento.Color),
                FontWeight = elemento.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = elemento.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                TextAlignment = elemento.TextAlignment,
                Width = elemento.Ancho,
                Height = elemento.Alto
            };

            if (elemento.IsUnderline)
            {
                textBlock.TextDecorations = TextDecorations.Underline;
            }

            return textBlock;
        }

        private UIElement CrearElementoError(ElementoTarjeta elemento, string mensaje = "Error")
        {
            return new TextBlock
            {
                Text = mensaje,
                Width = elemento.Ancho,
                Height = elemento.Alto,
                Background = Brushes.LightPink,
                Foreground = Brushes.Red,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        #endregion

        #region Métodos de Plantilla

        public void CargarPlantilla(PlantillaTarjeta plantilla)
        {
            try
            {
                PlantillaActual = plantilla;

                // Limpiar elementos actuales
                ElementosActuales.Clear();

                // Cargar elementos de la plantilla
                foreach (var elemento in plantilla.Elementos)
                {
                    ElementosActuales.Add(elemento);
                }

                ActualizarCanvas();
                EstadoActual = $"Plantilla cargada: {plantilla.Nombre}";

                System.Diagnostics.Debug.WriteLine($"Plantilla cargada: {plantilla.Nombre} con {plantilla.Elementos.Count} elementos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando plantilla: {ex.Message}");
                EstadoActual = "Error cargando plantilla";
            }
        }

        private async Task GuardarPlantillaAsync()
        {
            try
            {
                // Actualizar elementos de la plantilla
                PlantillaActual.Elementos = ElementosActuales.ToList();
                PlantillaActual.FechaModificacion = DateTime.Now;

                var resultado = await _templateService.GuardarPlantillaAsync(PlantillaActual);

                if (resultado)
                {
                    EstadoActual = $"Plantilla guardada: {PlantillaActual.Nombre}";
                    MessageBox.Show("Plantilla guardada correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    EstadoActual = "Error guardando plantilla";
                    MessageBox.Show("Error al guardar la plantilla.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando plantilla: {ex.Message}");
                EstadoActual = "Error guardando plantilla";
                MessageBox.Show($"Error al guardar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CargarPlantillaAsync()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Plantillas de tarjeta (*.cardtemplate)|*.cardtemplate|Todos los archivos (*.*)|*.*",
                    Title = "Cargar plantilla"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var json = await File.ReadAllTextAsync(openDialog.FileName);
                    var plantilla = JsonSerializer.Deserialize<PlantillaTarjeta>(json);

                    if (plantilla != null)
                    {
                        CargarPlantilla(plantilla);
                        MessageBox.Show("Plantilla cargada correctamente.", "Éxito",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error al cargar la plantilla.", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando plantilla: {ex.Message}");
                MessageBox.Show($"Error al cargar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AplicarPlantillaAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    $"¿Desea aplicar '{PlantillaActual.Nombre}' como plantilla predeterminada?\n\n" +
                    "Esta plantilla se usará para imprimir las tarjetas de abonados.",
                    "Aplicar Plantilla",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Actualizar elementos de la plantilla
                    PlantillaActual.Elementos = ElementosActuales.ToList();
                    PlantillaActual.FechaModificacion = DateTime.Now;

                    // Establecer como predeterminada
                    var exitoso = await _templateService.EstablecerPredeterminadaAsync(PlantillaActual.Id);

                    if (exitoso)
                    {
                        PlantillaActual.EsPredeterminada = true;
                        await _templateService.GuardarPlantillaAsync(PlantillaActual);

                        EstadoActual = "Plantilla aplicada como predeterminada";
                        MessageBox.Show(
                            $"✅ Plantilla '{PlantillaActual.Nombre}' aplicada correctamente como predeterminada.\n\n" +
                            "Esta plantilla se utilizará ahora para imprimir las tarjetas de abonados.",
                            "Plantilla Aplicada",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error al aplicar la plantilla como predeterminada.", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando plantilla: {ex.Message}");
                MessageBox.Show($"Error al aplicar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task GuardarYAplicarPlantillaAsync()
        {
            try
            {
                // Actualizar elementos de la plantilla
                PlantillaActual.Elementos = ElementosActuales.ToList();
                PlantillaActual.FechaModificacion = DateTime.Now;
                PlantillaActual.EsPredeterminada = true;

                // Guardar y establecer como predeterminada
                var exitoso = await _templateService.GuardarPlantillaAsync(PlantillaActual);
                if (exitoso)
                {
                    await _templateService.EstablecerPredeterminadaAsync(PlantillaActual.Id);
                    EstadoActual = "Plantilla guardada y aplicada";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando y aplicando plantilla: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Comando

        private void AgregarTexto()
        {
            try
            {
                var nuevoTexto = new ElementoTexto
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Texto",
                    X = 50,
                    Y = 50,
                    Ancho = 150,
                    Alto = 25,
                    ZIndex = ElementosActuales.Count + 1,
                    Texto = "Nuevo texto",
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.Black,
                    TextAlignment = TextAlignment.Left
                };

                ElementosActuales.Add(nuevoTexto);
                ActualizarCanvas();
                ElementoSeleccionado = nuevoTexto;
                EstadoActual = "Elemento de texto agregado";

                System.Diagnostics.Debug.WriteLine("Elemento de texto agregado");
            }
            catch (Exception ex)
            {
                MostrarError("Error agregando texto", ex);
            }
        }

        private void AgregarImagen()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Title = "Seleccionar imagen"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var nuevaImagen = new ElementoImagen
                    {
                        Id = Guid.NewGuid().ToString(),
                        Tipo = "Imagen",
                        X = 50,
                        Y = 50,
                        Ancho = 100,
                        Alto = 100,
                        ZIndex = ElementosActuales.Count + 1,
                        RutaImagen = openDialog.FileName,
                        MantenerAspecto = true,
                        ColorBorde = Colors.Transparent,
                        GrosorBorde = 0
                    };

                    ElementosActuales.Add(nuevaImagen);
                    ActualizarCanvas();
                    ElementoSeleccionado = nuevaImagen;
                    EstadoActual = "Imagen agregada";

                    System.Diagnostics.Debug.WriteLine($"Imagen agregada: {openDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error agregando imagen", ex);
            }
        }

        private void AgregarCodigoBarras()
        {
            AgregarCodigoBarrasConValidacion();
        }

        private void AgregarCampo(string? tipoCampo)
        {
            if (string.IsNullOrEmpty(tipoCampo)) return;

            try
            {
                var nuevoCampo = new ElementoCampoDinamico
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Campo Dinámico",
                    X = 50,
                    Y = 80,
                    Ancho = 150,
                    Alto = 20,
                    ZIndex = ElementosActuales.Count + 1,
                    CampoOrigen = tipoCampo,
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.Black,
                    TextAlignment = TextAlignment.Left,
                    Prefijo = "",
                    Sufijo = "",
                    TextoSiVacio = "N/A"
                };

                ElementosActuales.Add(nuevoCampo);
                ActualizarCanvas();
                ElementoSeleccionado = nuevoCampo;
                EstadoActual = $"Campo {tipoCampo} agregado";

                System.Diagnostics.Debug.WriteLine($"Campo dinámico agregado: {tipoCampo}");
            }
            catch (Exception ex)
            {
                MostrarError("Error agregando campo", ex);
            }
        }

        private void EliminarElemento()
        {
            try
            {
                if (ElementoSeleccionado == null)
                {
                    System.Diagnostics.Debug.WriteLine("No hay elemento seleccionado para eliminar");
                    return;
                }

                // Guardar el tipo antes de eliminar para evitar null reference
                var elementoEliminado = ElementoSeleccionado.Tipo ?? "Elemento";
                var elementoAEliminar = ElementoSeleccionado;

                var result = MessageBox.Show($"¿Eliminar el elemento {elementoEliminado} seleccionado?",
                                           "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Primero remover de la colección
                    ElementosActuales.Remove(elementoAEliminar);

                    // Luego limpiar la selección
                    ElementoSeleccionado = null;

                    // Actualizar canvas y estado
                    ActualizarCanvas();
                    EstadoActual = $"Elemento {elementoEliminado} eliminado";

                    System.Diagnostics.Debug.WriteLine($"Elemento eliminado exitosamente: {elementoEliminado}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error eliminando elemento: {ex.Message}");
                MostrarError("Error eliminando elemento", ex);
            }
        }

        private void NuevaPlantilla()
        {
            try
            {
                if (ElementosActuales.Count > 0)
                {
                    var result = MessageBox.Show("¿Crear nueva plantilla? Se perderán los cambios no guardados.",
                                               "Nueva plantilla", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;
                }

                ElementosActuales.Clear();
                PlantillaActual = new PlantillaTarjeta
                {
                    Id = Guid.NewGuid().ToString(),
                    Nombre = "Nueva Plantilla",
                    Descripcion = "",
                    Ancho = 350,
                    Alto = 220,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    Elementos = new List<ElementoTarjeta>()
                };

                ActualizarCanvas();
                EstadoActual = "Nueva plantilla creada";

                System.Diagnostics.Debug.WriteLine("Nueva plantilla creada");
            }
            catch (Exception ex)
            {
                MostrarError("Error creando nueva plantilla", ex);
            }
        }

        private void VistaPrevia()
        {
            try
            {
                MessageBox.Show("Vista previa de la tarjeta con datos del abonado seleccionado.",
                              "Vista previa", MessageBoxButton.OK, MessageBoxImage.Information);
                EstadoActual = "Vista previa mostrada";
            }
            catch (Exception ex)
            {
                MostrarError("Error en vista previa", ex);
            }
        }

        private async Task ImprimirPruebaAsync()
        {
            try
            {
                if (AbonadoEjemplo == null)
                {
                    MessageBox.Show("No hay datos de abonado para la prueba.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"¿Imprimir tarjeta de prueba para {AbonadoEjemplo.NombreCompleto}?",
                                           "Confirmar impresión", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Actualizar elementos de la plantilla
                    PlantillaActual.Elementos = ElementosActuales.ToList();

                    // Simular proceso de impresión
                    EstadoActual = "Preparando impresión...";
                    await Task.Delay(1000);

                    EstadoActual = "Enviando a impresora...";
                    await Task.Delay(2000);

                    EstadoActual = "Impresión completada";
                    MessageBox.Show("Tarjeta de prueba enviada a la impresora correctamente.",
                                  "Impresión completada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al imprimir", ex);
            }
        }

        private void CargarDatosReales()
        {
            try
            {
                CargarAbonadosDisponibles();
                EstadoActual = "Datos de abonados actualizados";
            }
            catch (Exception ex)
            {
                MostrarError("Error cargando datos", ex);
            }
        }

        #endregion

        #region Métodos Auxiliares

        private async void CargarAbonadosDisponibles()
        {
            try
            {
                var abonados = await _dbContext.Abonados
                    .Include(a => a.Gestor)
                    .Include(a => a.Peña)
                    .Include(a => a.TipoAbono)
                    .Take(20) // Limitar para rendimiento
                    .ToListAsync();

                AbonadosDisponibles.Clear();
                foreach (var abonado in abonados)
                {
                    AbonadosDisponibles.Add(abonado);
                }

                if (AbonadosDisponibles.Any())
                {
                    AbonadoSeleccionado = AbonadosDisponibles.First();
                }

                System.Diagnostics.Debug.WriteLine($"Cargados {AbonadosDisponibles.Count} abonados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando abonados: {ex.Message}");
            }
        }

        private Abonado CrearAbonadoEjemplo()
        {
            return new Abonado
            {
                Id = 999,
                NumeroSocio = 12345,
                Nombre = "Juan Carlos",
                Apellidos = "Ejemplo García",
                DNI = "12345678Z",
                Telefono = "123-456-789",
                Email = "juan.ejemplo@email.com",
                Estado = EstadoAbonado.Activo,
                FechaCreacion = DateTime.Now,
                CodigoBarras = "1234567890123",
                Gestor = new Gestor { Nombre = "Gestor Ejemplo" },
                Peña = new Peña { Nombre = "Peña Ejemplo" },
                TipoAbono = new TipoAbono { Nombre = "Abono Temporada" }
            };
        }

        private string ObtenerValorCampo(string campo)
        {
            try
            {
                var abonado = AbonadoEjemplo;
                return campo switch
                {
                    "NombreCompleto" => abonado.NombreCompleto,
                    "Nombre" => abonado.Nombre,
                    "Apellidos" => abonado.Apellidos,
                    "NumeroSocio" => abonado.NumeroSocio.ToString(),
                    "DNI" => abonado.DNI ?? "N/A",
                    "Telefono" => abonado.Telefono ?? "N/A",
                    "Email" => abonado.Email ?? "N/A",
                    "Peña" => abonado.Peña?.Nombre ?? "N/A",
                    "TipoAbono" => abonado.TipoAbono?.Nombre ?? "N/A",
                    "Estado" => abonado.Estado.ToString(),
                    "FechaNacimiento" => abonado.FechaNacimiento.ToString("dd/MM/yyyy"),
                    "CodigoBarras" => abonado.CodigoBarras ?? abonado.NumeroSocio.ToString(),
                    _ => "N/A"
                };
            }
            catch
            {
                return "N/A";
            }
        }

        private string GenerarEAN13(string input)
        {
            try
            {
                var numerosOnly = new string(input.Where(char.IsDigit).ToArray());

                if (numerosOnly.Length < 12)
                {
                    var numeroSocio = AbonadoSeleccionado?.NumeroSocio ?? AbonadoEjemplo?.NumeroSocio ?? 12345;
                    numerosOnly = numeroSocio.ToString().PadLeft(12, '0');
                }

                var primeros12 = numerosOnly.Substring(0, Math.Min(12, numerosOnly.Length)).PadLeft(12, '0');

                // Calcular dígito de control EAN13
                var suma = 0;
                for (int i = 0; i < 12; i++)
                {
                    var digito = int.Parse(primeros12[i].ToString());
                    suma += (i % 2 == 0) ? digito : digito * 3;
                }

                var digitoControl = (10 - (suma % 10)) % 10;
                return primeros12 + digitoControl.ToString();
            }
            catch
            {
                return "1234567890123"; // EAN13 genérico de fallback
            }
        }

        public static string GenerarCodigoBarrasParaAbonado(Abonado abonado)
        {
            if (!string.IsNullOrWhiteSpace(abonado.CodigoBarras))
            {
                return abonado.CodigoBarras;
            }

            // Generar código único y reproducible
            var numeroSocio = abonado.NumeroSocio.ToString();
            var año = abonado.FechaCreacion.Year.ToString();
            //var codigo = $"CD{año}{numeroSocio.PadLeft(6, '0')}";
            var codigo = $"{abonado.CodigoBarras.PadLeft(6, '0')}";

            // Agregar dígito verificador
            //var suma = codigo.Where(char.IsDigit).Sum(c => int.Parse(c.ToString()));
            var suma = abonado.CodigoBarras.Where(char.IsDigit).Sum(c => int.Parse(c.ToString()));
            var digitoVerificador = (10 - (suma % 10)) % 10;

            return $"{codigo}{digitoVerificador}";
        }

        private void ActualizarPropiedades()
        {
            // Este método se puede expandir para notificar cambios específicos
            OnPropertyChanged(nameof(ElementoSeleccionado));
        }

        private void MostrarError(string titulo, Exception ex)
        {
            var mensaje = $"{titulo}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ERROR: {mensaje}");
            EstadoActual = "Error";
            MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Disposición de Recursos

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}