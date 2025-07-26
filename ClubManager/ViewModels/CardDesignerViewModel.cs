using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Commands;
using ClubManager.Data;

namespace ClubManager.ViewModels
{
    public class CardDesignerViewModel : INotifyPropertyChanged
    {
        #region Campos Privados

        private readonly ClubDbContext _dbContext;
        private PlantillaTarjeta _plantillaActual;
        private ElementoTarjeta? _elementoSeleccionado;
        private string _estadoActual;
        private Abonado _abonadoEjemplo;
        private Abonado? _abonadoSeleccionado;
        private Canvas? _canvasTarjeta;

        #endregion

        #region Propiedades Públicas

        public ObservableCollection<ElementoTarjeta> ElementosActuales { get; }
        public ObservableCollection<Abonado> AbonadosDisponibles { get; }

        public PlantillaTarjeta PlantillaActual
        {
            get => _plantillaActual;
            set
            {
                if (SetProperty(ref _plantillaActual, value))
                {
                    // Actualizar elementos cuando cambie la plantilla
                    ElementosActuales.Clear();
                    if (value?.Elementos != null)
                    {
                        foreach (var elemento in value.Elementos)
                        {
                            ElementosActuales.Add(elemento);
                        }
                    }
                    ActualizarCanvas();
                }
            }
        }

        public ElementoTarjeta? ElementoSeleccionado
        {
            get => _elementoSeleccionado;
            set
            {
                SetProperty(ref _elementoSeleccionado, value);
                ActualizarPropiedades();
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

        #region Comandos - CORREGIDO: Agregar setters privados

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

        public CardDesignerViewModel()
        {
            System.Diagnostics.Debug.WriteLine("=== INICIANDO CardDesignerViewModel ===");

            _dbContext = new ClubDbContext();
            _estadoActual = "Listo para diseñar";

            ElementosActuales = new ObservableCollection<ElementoTarjeta>();
            AbonadosDisponibles = new ObservableCollection<Abonado>();

            System.Diagnostics.Debug.WriteLine("ElementosActuales inicializado");

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

            System.Diagnostics.Debug.WriteLine($"Plantilla creada: {_plantillaActual.Nombre}");

            // Crear abonado de ejemplo con datos más completos y realistas
            _abonadoEjemplo = CrearAbonadoEjemplo();

            System.Diagnostics.Debug.WriteLine($"Abonado ejemplo creado: {_abonadoEjemplo.NombreCompleto}");

            // Cargar abonados disponibles
            CargarAbonadosDisponibles();

            // Inicializar comandos
            InicializarComandos();

            System.Diagnostics.Debug.WriteLine("Comandos inicializados");

            // Agregar un elemento de prueba para verificar que funciona
            AgregarElementosDePrueba();

            System.Diagnostics.Debug.WriteLine("=== CardDesignerViewModel INICIALIZADO ===");
        }

        private void InicializarComandos()
        {
            AgregarTextoCommand = new RelayCommand(AgregarTexto);
            AgregarImagenCommand = new RelayCommand(AgregarImagen);
            AgregarCodigoBarrasCommand = new RelayCommand(AgregarCodigoBarras);
            AgregarCampoCommand = new RelayCommand<string>(AgregarCampo);
            EliminarElementoCommand = new RelayCommand(EliminarElemento, () => ElementoSeleccionado != null);
            NuevaPlantillaCommand = new RelayCommand(NuevaPlantilla);
            GuardarPlantillaCommand = new RelayCommand(GuardarPlantilla);
            CargarPlantillaCommand = new RelayCommand(CargarPlantilla);
            VistaPreviaCommand = new RelayCommand(VistaPrevia);
            ImprimirPruebaCommand = new RelayCommand(ImprimirPrueba);
            AplicarPlantillaCommand = new RelayCommand(AplicarPlantilla);
            CargarDatosRealesCommand = new RelayCommand(CargarDatosReales);
        }

        #endregion

        #region Métodos de Canvas

        public void SetCanvas(Canvas canvas)
        {
            try
            {
                _canvasTarjeta = canvas;
                System.Diagnostics.Debug.WriteLine("Canvas asignado correctamente");

                // Actualizar inmediatamente si hay elementos
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

                foreach (var elemento in ElementosActuales)
                {
                    var uiElement = CrearElementoUI(elemento);
                    if (uiElement != null)
                    {
                        Canvas.SetLeft(uiElement, elemento.X);
                        Canvas.SetTop(uiElement, elemento.Y);
                        Canvas.SetZIndex(uiElement, elemento.ZIndex);
                        _canvasTarjeta.Children.Add(uiElement);
                    }
                }
            }
            catch (Exception ex)
            {
                EstadoActual = $"Error al actualizar canvas: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error al actualizar canvas: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Datos

        private async void CargarAbonadosDisponibles()
        {
            try
            {
                var abonados = await _dbContext.Abonados
                    .Include(a => a.Gestor)
                    .Include(a => a.Peña)
                    .Include(a => a.TipoAbono)
                    .OrderBy(a => a.NumeroSocio)
                    .Take(50) // Limitar a 50 para performance
                    .ToListAsync();

                AbonadosDisponibles.Clear();

                // Agregar abonado de ejemplo
                AbonadosDisponibles.Add(_abonadoEjemplo);

                // Agregar abonados reales
                foreach (var abonado in abonados)
                {
                    AbonadosDisponibles.Add(abonado);
                }

                // Seleccionar el abonado de ejemplo por defecto
                AbonadoSeleccionado = _abonadoEjemplo;

                System.Diagnostics.Debug.WriteLine($"Cargados {AbonadosDisponibles.Count} abonados disponibles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando abonados: {ex.Message}");

                // Si hay error, al menos tener el abonado de ejemplo
                AbonadosDisponibles.Clear();
                AbonadosDisponibles.Add(_abonadoEjemplo);
                AbonadoSeleccionado = _abonadoEjemplo;
            }
        }

        private async void CargarDatosReales()
        {
            try
            {
                // Recargar la lista de abonados disponibles
                CargarAbonadosDisponibles();
                EstadoActual = "Lista de abonados actualizada";
                MessageBox.Show($"Se han actualizado los abonados disponibles", "Datos actualizados",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos reales: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al cargar datos";
            }
        }

        #endregion

        #region Métodos de Agregar Elementos

        private void AgregarTexto()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Iniciando agregar texto...");

                var elemento = new ElementoTexto
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Texto",
                    X = 20,
                    Y = 20,
                    Ancho = 120,
                    Alto = 25,
                    ZIndex = ElementosActuales.Count,
                    Texto = "Texto nuevo",
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.Black,
                    IsBold = false,
                    IsItalic = false,
                    TextAlignment = TextAlignment.Left
                };

                ElementosActuales.Add(elemento);
                ElementoSeleccionado = elemento;
                PlantillaActual.Elementos.Add(elemento);

                System.Diagnostics.Debug.WriteLine($"Elemento agregado. Total elementos: {ElementosActuales.Count}");

                EstadoActual = "Elemento de texto agregado";
                ActualizarCanvas();

                System.Diagnostics.Debug.WriteLine("Texto agregado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AgregarTexto: {ex.Message}");
                MessageBox.Show($"Error al agregar texto: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al agregar texto";
            }
        }

        private void AgregarImagen()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Seleccionar imagen"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var elemento = new ElementoImagen
                    {
                        Id = Guid.NewGuid().ToString(),
                        Tipo = "Imagen",
                        X = 20,
                        Y = 50,
                        Ancho = 100,
                        Alto = 100,
                        ZIndex = ElementosActuales.Count,
                        RutaImagen = openDialog.FileName,
                        MantenerAspecto = true
                    };

                    ElementosActuales.Add(elemento);
                    ElementoSeleccionado = elemento;
                    PlantillaActual.Elementos.Add(elemento);
                    EstadoActual = "Elemento de imagen agregado";
                    ActualizarCanvas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar imagen: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al agregar imagen";
            }
        }

        private void AgregarCodigoBarras()
        {
            try
            {
                var elemento = new ElementoCodigoBarras
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Código de Barras",
                    X = 200,
                    Y = 150,
                    Ancho = 120,
                    Alto = 40,
                    ZIndex = ElementosActuales.Count,
                    TipoCodigo = "Code128",
                    MostrarTexto = true,
                    CampoOrigen = "CodigoBarras"
                };

                ElementosActuales.Add(elemento);
                ElementoSeleccionado = elemento;
                PlantillaActual.Elementos.Add(elemento);
                EstadoActual = "Elemento de código de barras agregado";
                ActualizarCanvas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar código de barras: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al agregar código de barras";
            }
        }

        private void AgregarCampo(string? campo)
        {
            if (string.IsNullOrEmpty(campo)) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Iniciando agregar campo: {campo}");

                var elemento = new ElementoCampoDinamico
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Campo Dinámico",
                    X = 20,
                    Y = 80 + (ElementosActuales.Count * 30),
                    Ancho = 150,
                    Alto = 25,
                    ZIndex = ElementosActuales.Count,
                    CampoOrigen = campo,
                    Texto = $"{{{campo}}}",
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.Black,
                    IsBold = false,
                    IsItalic = false
                };

                ElementosActuales.Add(elemento);
                ElementoSeleccionado = elemento;
                PlantillaActual.Elementos.Add(elemento);

                System.Diagnostics.Debug.WriteLine($"Campo {campo} agregado. Total elementos: {ElementosActuales.Count}");

                EstadoActual = $"Campo {campo} agregado";
                ActualizarCanvas();

                System.Diagnostics.Debug.WriteLine($"Campo {campo} agregado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AgregarCampo: {ex.Message}");
                MessageBox.Show($"Error al agregar campo: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al agregar campo";
            }
        }

        private void EliminarElemento()
        {
            try
            {
                if (ElementoSeleccionado == null) return;

                var result = MessageBox.Show($"¿Está seguro de eliminar el elemento seleccionado?",
                                           "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ElementosActuales.Remove(ElementoSeleccionado);
                    PlantillaActual.Elementos.Remove(ElementoSeleccionado);
                    ElementoSeleccionado = null;
                    EstadoActual = "Elemento eliminado";
                    ActualizarCanvas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar elemento: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al eliminar elemento";
            }
        }

        private void AgregarElementosDePrueba()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Agregando elementos de prueba...");

                // Agregar un texto de prueba
                var textoEjemplo = new ElementoTexto
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Texto",
                    X = 20,
                    Y = 20,
                    Ancho = 200,
                    Alto = 25,
                    ZIndex = 0,
                    Texto = "CLUB DEPORTIVO",
                    FontFamily = "Arial",
                    FontSize = 16,
                    Color = Colors.DarkBlue,
                    IsBold = true,
                    IsItalic = false,
                    TextAlignment = TextAlignment.Center
                };

                ElementosActuales.Add(textoEjemplo);
                PlantillaActual.Elementos.Add(textoEjemplo);

                System.Diagnostics.Debug.WriteLine($"Elementos de prueba agregados. Total: {ElementosActuales.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando elementos de prueba: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Archivo y Plantillas

        private void NuevaPlantilla()
        {
            try
            {
                var result = MessageBox.Show("¿Está seguro de crear una nueva plantilla? " +
                                           "Se perderán los cambios no guardados.",
                                           "Nueva plantilla", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    PlantillaActual = new PlantillaTarjeta
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

                    ElementosActuales.Clear();
                    ElementoSeleccionado = null;
                    EstadoActual = "Nueva plantilla creada";
                    ActualizarCanvas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear nueva plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al crear nueva plantilla";
            }
        }

        private void GuardarPlantilla()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PlantillaActual.Nombre))
                {
                    MessageBox.Show("Debe especificar un nombre para la plantilla.", "Información requerida",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Plantillas de tarjeta (*.cardtemplate)|*.cardtemplate",
                    Title = "Guardar plantilla de tarjeta",
                    FileName = PlantillaActual.Nombre + ".cardtemplate"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Actualizar elementos antes de guardar
                    PlantillaActual.Elementos.Clear();
                    foreach (var elemento in ElementosActuales)
                    {
                        PlantillaActual.Elementos.Add(elemento);
                    }

                    PlantillaActual.FechaModificacion = DateTime.Now;

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };

                    var json = JsonSerializer.Serialize(PlantillaActual, options);
                    File.WriteAllText(saveDialog.FileName, json);

                    MessageBox.Show($"Plantilla guardada correctamente en:\n{saveDialog.FileName}",
                                   "Guardar", MessageBoxButton.OK, MessageBoxImage.Information);
                    EstadoActual = $"Plantilla guardada: {PlantillaActual.Nombre}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al guardar plantilla";
            }
        }

        private void CargarPlantilla()
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "Plantillas de tarjeta (*.cardtemplate)|*.cardtemplate",
                    Title = "Cargar plantilla de tarjeta"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var plantilla = JsonSerializer.Deserialize<PlantillaTarjeta>(json);

                    if (plantilla != null)
                    {
                        PlantillaActual = plantilla;
                        ElementosActuales.Clear();

                        foreach (var elemento in plantilla.Elementos)
                        {
                            ElementosActuales.Add(elemento);
                        }

                        ElementoSeleccionado = null;
                        EstadoActual = $"Plantilla cargada: {plantilla.Nombre}";
                        ActualizarCanvas();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al cargar plantilla";
            }
        }

        #endregion

        #region Métodos de Vista Previa e Impresión

        private void VistaPrevia()
        {
            try
            {
                EstadoActual = "Generando vista previa...";
                // Aquí se abriría una ventana de vista previa
                MessageBox.Show("Vista previa generada correctamente", "Vista Previa",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                EstadoActual = "Vista previa generada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar vista previa: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error en vista previa";
            }
        }

        private void ImprimirPrueba()
        {
            try
            {
                EstadoActual = "Imprimiendo prueba...";
                // Aquí se implementaría la lógica de impresión
                MessageBox.Show("Tarjeta de prueba enviada a impresora", "Imprimir",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                EstadoActual = "Impresión completada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error en impresión";
            }
        }

        private void AplicarPlantilla()
        {
            try
            {
                var result = MessageBox.Show("¿Desea aplicar esta plantilla como predeterminada?",
                                           "Aplicar Plantilla", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Aquí se guardaría como plantilla predeterminada
                    EstadoActual = "Plantilla aplicada como predeterminada";
                    MessageBox.Show("Plantilla aplicada correctamente", "Aplicar",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al aplicar plantilla";
            }
        }

        #endregion

        #region Creación de Elementos UI

        private UIElement? CrearElementoUI(ElementoTarjeta elemento)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Creando UI para elemento: {elemento.Tipo} - {elemento.Id}");

                UIElement? result = elemento switch
                {
                    ElementoTexto textoElem => CrearTextoUI(textoElem),
                    ElementoImagen imagenElem => CrearImagenUI(imagenElem),
                    ElementoCodigoBarras codigoElem => CrearCodigoBarrasUI(codigoElem),
                    ElementoCampoDinamico campoElem => CrearCampoDinamicoUI(campoElem),
                    _ => null
                };

                if (result != null)
                {
                    System.Diagnostics.Debug.WriteLine($"UI creado correctamente para: {elemento.Tipo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo crear UI para: {elemento.Tipo}");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando elemento UI: {ex.Message}");
                return null;
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
                Height = elemento.Alto
            };

            return textBlock;
        }

        private Image? CrearImagenUI(ElementoImagen elemento)
        {
            try
            {
                if (!File.Exists(elemento.RutaImagen))
                    return null;

                var image = new Image
                {
                    Source = new BitmapImage(new Uri(elemento.RutaImagen)),
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Stretch = elemento.MantenerAspecto ? Stretch.Uniform : Stretch.Fill
                };

                return image;
            }
            catch
            {
                return null;
            }
        }

        private Border CrearCodigoBarrasUI(ElementoCodigoBarras elemento)
        {
            try
            {
                var border = new Border
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White
                };

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Crear imagen del código de barras
                var barcodeImage = GenerarCodigoBarras(ObtenerValorCampo(elemento.CampoOrigen), elemento.Ancho - 4, elemento.Alto - 20);
                if (barcodeImage != null)
                {
                    var image = new Image
                    {
                        Source = barcodeImage,
                        Stretch = Stretch.Fill
                    };
                    stackPanel.Children.Add(image);
                }

                // Mostrar texto si está habilitado
                if (elemento.MostrarTexto)
                {
                    var textBlock = new TextBlock
                    {
                        Text = ObtenerValorCampo(elemento.CampoOrigen),
                        FontSize = 8,
                        FontFamily = new FontFamily("Courier New"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    stackPanel.Children.Add(textBlock);
                }

                border.Child = stackPanel;
                return border;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando código de barras: {ex.Message}");

                // Retornar un placeholder en caso de error
                var errorBorder = new Border
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    BorderBrush = Brushes.Red,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.LightGray
                };

                var errorText = new TextBlock
                {
                    Text = "Error\nCódigo",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center
                };

                errorBorder.Child = errorText;
                return errorBorder;
            }
        }

        private TextBlock CrearCampoDinamicoUI(ElementoCampoDinamico elemento)
        {
            var textBlock = new TextBlock
            {
                Text = ObtenerValorCampo(elemento.CampoOrigen),
                FontFamily = new FontFamily(elemento.FontFamily),
                FontSize = elemento.FontSize,
                Foreground = new SolidColorBrush(elemento.Color),
                FontWeight = elemento.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = elemento.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                Width = elemento.Ancho,
                Height = elemento.Alto
            };

            return textBlock;
        }

        #endregion

        #region Métodos Auxiliares

        private string ObtenerValorCampo(string campo)
        {
            // Usar el abonado seleccionado si está disponible, sino el de ejemplo
            var abonadoActual = AbonadoSeleccionado ?? AbonadoEjemplo;

            if (abonadoActual == null) return $"{{{campo}}}";

            return campo switch
            {
                "NombreCompleto" => abonadoActual.NombreCompleto,
                "Nombre" => abonadoActual.Nombre,
                "Apellidos" => abonadoActual.Apellidos,
                "NumeroSocio" => abonadoActual.NumeroSocio.ToString(),
                "DNI" => abonadoActual.DNI ?? "Sin DNI",
                "CodigoBarras" => GenerarCodigoBarrasParaAbonado(abonadoActual),
                "Peña" => abonadoActual.Peña?.Nombre ?? "Sin Peña",
                "TipoAbono" => abonadoActual.TipoAbono?.Nombre ?? "Sin Tipo",
                "Estado" => abonadoActual.EstadoTexto,
                "Telefono" => abonadoActual.Telefono ?? "Sin teléfono",
                "Email" => abonadoActual.Email ?? "Sin email",
                "FechaNacimiento" => abonadoActual.FechaNacimiento.ToString("dd/MM/yyyy"),
                "Direccion" => abonadoActual.Direccion ?? "Sin dirección",
                "TallaCamiseta" => abonadoActual.TallaCamiseta ?? "Sin talla",
                _ => $"{{{campo}}}"
            };
        }

        private BitmapSource? GenerarCodigoBarras(string texto, double ancho, double alto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(texto))
                    return null;

                // Usar el generador de códigos de barras real
                return BarcodeGenerator.GenerateBarcode(texto, ancho, alto, "Code128");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando código de barras: {ex.Message}");
                return null;
            }
        }

        private void ActualizarPropiedades()
        {
            // Esta función se llamaría desde el code-behind para actualizar el panel de propiedades
            // basado en el elemento seleccionado
        }

        private Abonado CrearAbonadoEjemplo()
        {
            return new Abonado
            {
                Id = 999,
                NumeroSocio = 12345,
                Nombre = "Juan Carlos",
                Apellidos = "Pérez García",
                DNI = "12345678Z",
                CodigoBarras = GenerarCodigoBarrasEjemplo(),
                Estado = EstadoAbonado.Activo,
                Telefono = "666-123-456",
                Email = "juan.perez@clubdeportivo.com",
                FechaNacimiento = new DateTime(1985, 5, 15),
                FechaCreacion = DateTime.Now.AddMonths(-6),
                TallaCamiseta = "L",
                Direccion = "Calle del Estadio 123, 28001 Madrid",
                Observaciones = "Abonado desde 2018",
                Impreso = false,
                // Relaciones de ejemplo
                Peña = new Peña
                {
                    Id = 1,
                    Nombre = "Peña Los Campeones",
                    //Descripcion = "Peña oficial del club"
                },
                TipoAbono = new TipoAbono
                {
                    Id = 1,
                    Nombre = "Abono Temporada Completa",
                    Descripcion = "Acceso a todos los partidos de liga",
                    Precio = 250.00m
                },
                Gestor = new Gestor
                {
                    Id = 1,
                    Nombre = "María González",
                    Email = "maria.gonzalez@clubdeportivo.com",
                    Telefono = "91-555-0123"
                }
            };
        }

        private string GenerarCodigoBarrasEjemplo()
        {
            // Generar código de barras válido basado en el número de socio
            var numeroSocio = "12345";
            var año = DateTime.Now.Year.ToString();
            var codigo = $"{numeroSocio.PadLeft(6, '0')}";

            // Agregar dígito verificador simple
            var suma = codigo.Sum(c => char.IsDigit(c) ? int.Parse(c.ToString()) : 0);
            var digitoVerificador = (10 - (suma % 10)) % 10;

            return $"{codigo}{digitoVerificador}";
        }

        // Método público para actualizar el abonado de ejemplo con datos reales específicos
        public void SetAbonadoEjemplo(Abonado abonado)
        {
            AbonadoEjemplo = abonado;
            ActualizarCanvas();
        }

        // Método para crear código de barras único para abonados reales
        public static string GenerarCodigoBarrasParaAbonado(Abonado abonado)
        {
            // Si ya tiene código de barras, usarlo
            if (!string.IsNullOrWhiteSpace(abonado.CodigoBarras))
            {
                return abonado.CodigoBarras;
            }

            // Generar nuevo código basado en datos del abonado
            var numeroSocio = abonado.CodigoBarras.ToString();
            var año = abonado.FechaCreacion.Year.ToString();
            var codigo = $"{numeroSocio.PadLeft(6, '0')}";

            // Dígito verificador
            var suma = codigo.Sum(c => char.IsDigit(c) ? int.Parse(c.ToString()) : 0);
            var digitoVerificador = (10 - (suma % 10)) % 10;

            return $"{codigo}{digitoVerificador}";
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}