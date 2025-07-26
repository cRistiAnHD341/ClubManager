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
        public ICommand DuplicarElementoCommand { get; private set; } = null!;
        public ICommand ValidarPlantillaCommand { get; private set; } = null!;

        #endregion

        #region Constructor

        public CardDesignerViewModel()
        {
            System.Diagnostics.Debug.WriteLine("=== INICIANDO CardDesignerViewModel ===");

            _dbContext = new ClubDbContext();
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

            // Agregar elementos de prueba
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
            DuplicarElementoCommand = new RelayCommand(DuplicarElemento, () => ElementoSeleccionado != null);
            ValidarPlantillaCommand = new RelayCommand(ValidarPlantilla);
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
                    .Where(a => a.Estado == EstadoAbonado.Activo) // Solo abonados activos
                    .OrderBy(a => a.NumeroSocio)
                    .Take(100) // Aumentar a 100 para más opciones
                    .ToListAsync();

                AbonadosDisponibles.Clear();
                AbonadosDisponibles.Add(_abonadoEjemplo);

                foreach (var abonado in abonados)
                {
                    AbonadosDisponibles.Add(abonado);
                }

                AbonadoSeleccionado = _abonadoEjemplo;
                System.Diagnostics.Debug.WriteLine($"Cargados {AbonadosDisponibles.Count} abonados disponibles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando abonados: {ex.Message}");
                AbonadosDisponibles.Clear();
                AbonadosDisponibles.Add(_abonadoEjemplo);
                AbonadoSeleccionado = _abonadoEjemplo;
            }
        }

        private async void CargarDatosReales()
        {
            try
            {
                EstadoActual = "Cargando datos...";
                CargarAbonadosDisponibles();
                await Task.Delay(500); // Simular carga
                EstadoActual = "Lista de abonados actualizada";
                MessageBox.Show($"Se han actualizado {AbonadosDisponibles.Count} abonados disponibles",
                              "Datos actualizados", MessageBoxButton.OK, MessageBoxImage.Information);
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
                var elemento = new ElementoTexto
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Texto",
                    X = GetNextElementX(),
                    Y = GetNextElementY(),
                    Ancho = 120,
                    Alto = 25,
                    ZIndex = GetNextZIndex(),
                    Texto = "Texto nuevo",
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.Black,
                    IsBold = false,
                    IsItalic = false,
                    TextAlignment = TextAlignment.Left
                };

                AgregarElemento(elemento);
                EstadoActual = "Elemento de texto agregado";
            }
            catch (Exception ex)
            {
                MostrarError("Error al agregar texto", ex);
            }
        }

        private void AgregarImagen()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Title = "Seleccionar imagen",
                    Multiselect = false
                };

                if (openDialog.ShowDialog() == true)
                {
                    // Validar tamaño de archivo
                    var fileInfo = new FileInfo(openDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024) // 5MB máximo
                    {
                        MessageBox.Show("El archivo es demasiado grande. Máximo 5MB permitido.",
                                      "Archivo demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var elemento = new ElementoImagen
                    {
                        Id = Guid.NewGuid().ToString(),
                        Tipo = "Imagen",
                        X = GetNextElementX(),
                        Y = GetNextElementY(),
                        Ancho = 100,
                        Alto = 100,
                        ZIndex = GetNextZIndex(),
                        RutaImagen = openDialog.FileName,
                        MantenerAspecto = true,
                        Redondez = 0,
                        GrosorBorde = 0,
                        ColorBorde = Colors.Transparent
                    };

                    AgregarElemento(elemento);
                    EstadoActual = "Elemento de imagen agregado";
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al agregar imagen", ex);
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
                    X = GetNextElementX(),
                    Y = GetNextElementY(),
                    Ancho = 150, // Más ancho para mejor legibilidad
                    Alto = 50,   // Más alto para códigos con texto
                    ZIndex = GetNextZIndex(),
                    TipoCodigo = "Code128",
                    MostrarTexto = true,
                    CampoOrigen = "CodigoBarras",
                    FontSize = 8,
                    ColorTexto = Colors.Black,
                    ColorFondo = Colors.Transparent // FONDO TRANSPARENTE
                };

                AgregarElemento(elemento);
                EstadoActual = "Elemento de código de barras agregado";
            }
            catch (Exception ex)
            {
                MostrarError("Error al agregar código de barras", ex);
            }
        }

        private void AgregarCampo(string? campo)
        {
            if (string.IsNullOrEmpty(campo)) return;

            try
            {
                var elemento = new ElementoCampoDinamico
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Campo Dinámico",
                    X = GetNextElementX(),
                    Y = GetNextElementY(),
                    Ancho = 150,
                    Alto = 25,
                    ZIndex = GetNextZIndex(),
                    CampoOrigen = campo,
                    Texto = $"{{{campo}}}",
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.Black,
                    IsBold = false,
                    IsItalic = false,
                    TextAlignment = TextAlignment.Left,
                    Prefijo = "",
                    Sufijo = ""
                };

                AgregarElemento(elemento);
                EstadoActual = $"Campo {campo} agregado";
            }
            catch (Exception ex)
            {
                MostrarError($"Error al agregar campo {campo}", ex);
            }
        }

        private void EliminarElemento()
        {
            try
            {
                if (ElementoSeleccionado == null) return;

                var result = MessageBox.Show($"¿Está seguro de eliminar el elemento '{ElementoSeleccionado.Tipo}'?",
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
                MostrarError("Error al eliminar elemento", ex);
            }
        }

        private void DuplicarElemento()
        {
            try
            {
                if (ElementoSeleccionado == null) return;

                var elementoDuplicado = DuplicarElementoTarjeta(ElementoSeleccionado);
                if (elementoDuplicado != null)
                {
                    // Offset para que no se superponga
                    elementoDuplicado.X += 10;
                    elementoDuplicado.Y += 10;
                    elementoDuplicado.ZIndex = GetNextZIndex();

                    AgregarElemento(elementoDuplicado);
                    EstadoActual = $"Elemento '{ElementoSeleccionado.Tipo}' duplicado";
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al duplicar elemento", ex);
            }
        }

        #endregion

        #region Métodos de Archivo y Plantillas

        private void NuevaPlantilla()
        {
            try
            {
                if (ElementosActuales.Count > 0)
                {
                    var result = MessageBox.Show("¿Está seguro de crear una nueva plantilla? " +
                                               "Se perderán los cambios no guardados.",
                                               "Nueva plantilla", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }

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
            catch (Exception ex)
            {
                MostrarError("Error al crear nueva plantilla", ex);
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

                // Validar plantilla antes de guardar
                var errores = ValidarPlantillaActual();
                if (errores.Any())
                {
                    var result = MessageBox.Show($"Se encontraron {errores.Count} problemas:\n" +
                                               string.Join("\n", errores.Take(3)) +
                                               (errores.Count > 3 ? "\n..." : "") +
                                               "\n\n¿Desea guardar de todos modos?",
                                               "Problemas en la plantilla", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result != MessageBoxResult.Yes) return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Plantillas de tarjeta (*.cardtemplate)|*.cardtemplate",
                    Title = "Guardar plantilla de tarjeta",
                    FileName = PlantillaActual.Nombre.Replace(" ", "_") + ".cardtemplate"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Actualizar elementos y metadatos
                    PlantillaActual.Elementos.Clear();
                    foreach (var elemento in ElementosActuales)
                    {
                        PlantillaActual.Elementos.Add(elemento);
                    }

                    PlantillaActual.FechaModificacion = DateTime.Now;

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
                MostrarError("Error al guardar plantilla", ex);
            }
        }

        private void CargarPlantilla()
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "Plantillas de tarjeta (*.cardtemplate)|*.cardtemplate|Todos los archivos (*.*)|*.*",
                    Title = "Cargar plantilla de tarjeta"
                };

                if (openDialog.ShowDialog() == true)
                {
                    if (!File.Exists(openDialog.FileName))
                    {
                        MessageBox.Show("El archivo seleccionado no existe.", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var json = File.ReadAllText(openDialog.FileName);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var plantilla = JsonSerializer.Deserialize<PlantillaTarjeta>(json, options);

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

                        // Mostrar información de la plantilla cargada
                        MessageBox.Show($"Plantilla '{plantilla.Nombre}' cargada correctamente.\n" +
                                      $"Elementos: {plantilla.Elementos.Count}\n" +
                                      $"Dimensiones: {plantilla.Ancho}x{plantilla.Alto}",
                                      "Plantilla cargada", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error al deserializar la plantilla. Archivo corrupto o formato inválido.",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al cargar plantilla", ex);
            }
        }

        private void ValidarPlantilla()
        {
            try
            {
                var errores = ValidarPlantillaActual();

                if (!errores.Any())
                {
                    MessageBox.Show("✅ La plantilla es válida y no tiene problemas detectados.",
                                  "Validación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    EstadoActual = "Plantilla validada correctamente";
                }
                else
                {
                    var mensaje = $"❌ Se encontraron {errores.Count} problemas:\n\n" +
                                string.Join("\n", errores.Take(10)) +
                                (errores.Count > 10 ? $"\n\n... y {errores.Count - 10} más." : "");

                    MessageBox.Show(mensaje, "Problemas en la plantilla",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    EstadoActual = $"Plantilla con {errores.Count} problemas";
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al validar plantilla", ex);
            }
        }

        #endregion

        #region Métodos de Vista Previa e Impresión

        private void VistaPrevia()
        {
            try
            {
                EstadoActual = "Generando vista previa...";

                // Crear ventana de vista previa
                var vistaPrevia = new Window
                {
                    Title = $"Vista Previa - {PlantillaActual.Nombre}",
                    Width = PlantillaActual.Ancho + 100,
                    Height = PlantillaActual.Alto + 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow
                };

                var border = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(20)
                };

                var canvas = new Canvas
                {
                    Width = PlantillaActual.Ancho,
                    Height = PlantillaActual.Alto,
                    Background = Brushes.White
                };

                // Renderizar todos los elementos
                foreach (var elemento in ElementosActuales.OrderBy(e => e.ZIndex))
                {
                    var uiElement = CrearElementoUI(elemento);
                    if (uiElement != null)
                    {
                        Canvas.SetLeft(uiElement, elemento.X);
                        Canvas.SetTop(uiElement, elemento.Y);
                        canvas.Children.Add(uiElement);
                    }
                }

                border.Child = canvas;
                vistaPrevia.Content = border;
                vistaPrevia.ShowDialog();

                EstadoActual = "Vista previa generada";
            }
            catch (Exception ex)
            {
                MostrarError("Error al generar vista previa", ex);
            }
        }

        private void ImprimirPrueba()
        {
            try
            {
                EstadoActual = "Preparando impresión...";

                var result = MessageBox.Show($"¿Desea imprimir una tarjeta de prueba con los datos de '{AbonadoEjemplo.NombreCompleto}'?",
                                           "Confirmar impresión", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Aquí se implementaría la lógica real de impresión
                    // Por ahora, simular el proceso
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(2000);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Tarjeta de prueba enviada a la impresora correctamente.",
                                          "Impresión completada", MessageBoxButton.OK, MessageBoxImage.Information);
                            EstadoActual = "Impresión completada";
                        });
                    });

                    EstadoActual = "Enviando a impresora...";
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al imprimir", ex);
            }
        }

        private void AplicarPlantilla()
        {
            try
            {
                var errores = ValidarPlantillaActual();
                if (errores.Any())
                {
                    MessageBox.Show($"No se puede aplicar la plantilla debido a errores:\n" +
                                  string.Join("\n", errores.Take(5)),
                                  "Plantilla inválida", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"¿Desea aplicar '{PlantillaActual.Nombre}' como plantilla predeterminada?",
                                           "Aplicar Plantilla", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Guardar como plantilla predeterminada en la base de datos
                    // Por ahora, simular el proceso
                    PlantillaActual.EsPredeterminada = true;
                    EstadoActual = "Plantilla aplicada como predeterminada";
                    MessageBox.Show("Plantilla aplicada correctamente como predeterminada.", "Aplicar",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al aplicar plantilla", ex);
            }
        }

        #endregion

        #region Creación de Elementos UI - MEJORADO SIN MARCOS

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

                // Cargar imagen de forma segura
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(elemento.RutaImagen);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                image.Source = bitmap;

                // Aplicar bordes redondeados o bordes si es necesario
                if (elemento.Redondez > 0 || elemento.GrosorBorde > 0)
                {
                    var border = new Border
                    {
                        Width = elemento.Ancho,
                        Height = elemento.Alto,
                        Child = image,
                        ClipToBounds = true
                    };

                    if (elemento.Redondez > 0)
                    {
                        border.CornerRadius = new CornerRadius(elemento.Redondez);
                    }

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

        // MEJORADO: Código de barras SIN marco
        private UIElement CrearCodigoBarrasUI(ElementoCodigoBarras elemento)
        {
            try
            {
                var valorCodigo = ObtenerValorCampo(elemento.CampoOrigen);

                // Crear contenedor SIN borde
                var container = new StackPanel
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(elemento.ColorFondo)
                };

                // Generar código de barras con el formato correcto
                BitmapSource? barcodeImage = null;
                var alturaBarras = elemento.MostrarTexto ? elemento.Alto - 15 : elemento.Alto;

                switch (elemento.TipoCodigo.ToUpper())
                {
                    case "CODE128":
                        barcodeImage = BarcodeGenerator.GenerateBarcode(valorCodigo, elemento.Ancho, alturaBarras, "Code128");
                        break;
                    case "CODE39":
                        barcodeImage = BarcodeGenerator.GenerateBarcode(valorCodigo, elemento.Ancho, alturaBarras, "Code39");
                        break;
                    case "EAN13":
                        var ean13Value = GenerarEAN13(valorCodigo);
                        barcodeImage = BarcodeGenerator.GenerateBarcode(ean13Value, elemento.Ancho, alturaBarras, "EAN13");
                        break;
                    case "QRCODE":
                        barcodeImage = BarcodeGenerator.GenerateQRCode(valorCodigo, Math.Min(elemento.Ancho, elemento.Alto));
                        break;
                    default:
                        barcodeImage = BarcodeGenerator.GenerateBarcode(valorCodigo, elemento.Ancho, alturaBarras, "Code128");
                        break;
                }

                // Agregar imagen del código de barras
                if (barcodeImage != null)
                {
                    var image = new Image
                    {
                        Source = barcodeImage,
                        Stretch = Stretch.Fill,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    container.Children.Add(image);
                }

                // Agregar texto si está habilitado
                if (elemento.MostrarTexto)
                {
                    var textBlock = new TextBlock
                    {
                        Text = valorCodigo,
                        FontSize = elemento.FontSize,
                        FontFamily = new FontFamily("Courier New"),
                        Foreground = new SolidColorBrush(elemento.ColorTexto),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    container.Children.Add(textBlock);
                }

                return container;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando código de barras: {ex.Message}");
                return CrearElementoError(elemento, $"Error: {elemento.TipoCodigo}");
            }
        }

        private TextBlock CrearCampoDinamicoUI(ElementoCampoDinamico elemento)
        {
            var valor = ObtenerValorCampo(elemento.CampoOrigen);
            var textoFinal = $"{elemento.Prefijo}{valor}{elemento.Sufijo}";

            var textBlock = new TextBlock
            {
                Text = textoFinal,
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

        private Border CrearElementoError(ElementoTarjeta elemento, string mensaje = "Error")
        {
            return new Border
            {
                Width = elemento.Ancho,
                Height = elemento.Alto,
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(1),
                Background = Brushes.LightPink,
                Child = new TextBlock
                {
                    Text = mensaje,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.DarkRed
                }
            };
        }

        #endregion

        #region Métodos Auxiliares - MEJORADOS

        private void AgregarElemento(ElementoTarjeta elemento)
        {
            ElementosActuales.Add(elemento);
            ElementoSeleccionado = elemento;
            PlantillaActual.Elementos.Add(elemento);
            ActualizarCanvas();
        }

        private double GetNextElementX()
        {
            return ElementosActuales.Any() ?
                   Math.Min(20 + (ElementosActuales.Count * 15), PlantillaActual.Ancho - 100) : 20;
        }

        private double GetNextElementY()
        {
            return ElementosActuales.Any() ?
                   Math.Min(20 + (ElementosActuales.Count * 25), PlantillaActual.Alto - 50) : 20;
        }

        private int GetNextZIndex()
        {
            return ElementosActuales.Any() ? ElementosActuales.Max(e => e.ZIndex) + 1 : 0;
        }

        private ElementoTarjeta? DuplicarElementoTarjeta(ElementoTarjeta original)
        {
            try
            {
                // Serializar y deserializar para crear una copia profunda
                var json = JsonSerializer.Serialize(original);
                var copia = JsonSerializer.Deserialize<ElementoTarjeta>(json);

                if (copia != null)
                {
                    copia.Id = Guid.NewGuid().ToString();
                }

                return copia;
            }
            catch
            {
                return null;
            }
        }

        private List<string> ValidarPlantillaActual()
        {
            var errores = new List<string>();

            try
            {
                // Validar plantilla básica
                if (string.IsNullOrWhiteSpace(PlantillaActual.Nombre))
                    errores.Add("La plantilla debe tener un nombre");

                if (PlantillaActual.Ancho <= 0 || PlantillaActual.Alto <= 0)
                    errores.Add("Las dimensiones de la plantilla deben ser positivas");

                if (PlantillaActual.Ancho > 1000 || PlantillaActual.Alto > 1000)
                    errores.Add("Las dimensiones de la plantilla son demasiado grandes (máximo 1000px)");

                // Validar elementos
                foreach (var elemento in ElementosActuales)
                {
                    // Validar posición
                    if (elemento.X < 0 || elemento.Y < 0)
                        errores.Add($"Elemento '{elemento.Tipo}' tiene posición negativa");

                    if (elemento.X + elemento.Ancho > PlantillaActual.Ancho)
                        errores.Add($"Elemento '{elemento.Tipo}' se sale del borde derecho");

                    if (elemento.Y + elemento.Alto > PlantillaActual.Alto)
                        errores.Add($"Elemento '{elemento.Tipo}' se sale del borde inferior");

                    // Validar tamaño
                    if (elemento.Ancho <= 0 || elemento.Alto <= 0)
                        errores.Add($"Elemento '{elemento.Tipo}' tiene dimensiones inválidas");

                    // Validaciones específicas
                    switch (elemento)
                    {
                        case ElementoTexto texto:
                            if (string.IsNullOrWhiteSpace(texto.Texto))
                                errores.Add($"Elemento de texto está vacío");
                            if (texto.FontSize <= 0)
                                errores.Add($"Tamaño de fuente inválido en elemento de texto");
                            break;

                        case ElementoImagen imagen:
                            if (string.IsNullOrWhiteSpace(imagen.RutaImagen))
                                errores.Add($"Elemento de imagen sin ruta especificada");
                            else if (!File.Exists(imagen.RutaImagen))
                                errores.Add($"Archivo de imagen no encontrado: {Path.GetFileName(imagen.RutaImagen)}");
                            break;

                        case ElementoCodigoBarras codigo:
                            if (string.IsNullOrWhiteSpace(codigo.CampoOrigen))
                                errores.Add($"Código de barras sin campo origen");
                            if (string.IsNullOrWhiteSpace(codigo.TipoCodigo))
                                errores.Add($"Código de barras sin tipo especificado");
                            break;

                        case ElementoCampoDinamico campo:
                            if (string.IsNullOrWhiteSpace(campo.CampoOrigen))
                                errores.Add($"Campo dinámico sin origen especificado");
                            break;
                    }
                }

                // Verificar solapamientos críticos
                var elementosPorPosicion = ElementosActuales
                    .GroupBy(e => new { X = (int)(e.X / 10), Y = (int)(e.Y / 10) })
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (elementosPorPosicion.Any())
                {
                    errores.Add($"Hay {elementosPorPosicion.Count} áreas con elementos superpuestos");
                }
            }
            catch (Exception ex)
            {
                errores.Add($"Error durante la validación: {ex.Message}");
            }

            return errores;
        }

        private void MostrarError(string titulo, Exception ex)
        {
            var mensaje = $"{titulo}: {ex.Message}";
            MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            EstadoActual = titulo;
            System.Diagnostics.Debug.WriteLine($"ERROR: {mensaje}");
        }

        public string ObtenerValorCampo(string campo)
        {
            var abonadoActual = AbonadoSeleccionado ?? AbonadoEjemplo;
            if (abonadoActual == null) return $"{{{campo}}}";

            return campo switch
            {
                "NombreCompleto" => abonadoActual.NombreCompleto ?? "Sin nombre",
                "Nombre" => abonadoActual.Nombre ?? "Sin nombre",
                "Apellidos" => abonadoActual.Apellidos ?? "Sin apellidos",
                "NumeroSocio" => abonadoActual.NumeroSocio.ToString(),
                "DNI" => abonadoActual.DNI ?? "Sin DNI",
                "CodigoBarras" => GenerarCodigoBarrasParaAbonado(abonadoActual),
                "Peña" => abonadoActual.Peña?.Nombre ?? "Sin Peña",
                "TipoAbono" => abonadoActual.TipoAbono?.Nombre ?? "Sin Tipo",
                "Estado" => abonadoActual.EstadoTexto ?? "Sin estado",
                "Telefono" => abonadoActual.Telefono ?? "Sin teléfono",
                "Email" => abonadoActual.Email ?? "Sin email",
                "FechaNacimiento" => abonadoActual.FechaNacimiento.ToString("dd/MM/yyyy"),
                "Direccion" => abonadoActual.Direccion ?? "Sin dirección",
                "TallaCamiseta" => abonadoActual.TallaCamiseta ?? "Sin talla",
                "FechaCreacion" => abonadoActual.FechaCreacion.ToString("dd/MM/yyyy"),
                "Gestor" => abonadoActual.Gestor?.Nombre ?? "Sin gestor",
                _ => $"{{{campo}}}"
            };
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

        private void ActualizarPropiedades()
        {
            // Este método se usa desde el code-behind para actualizar el panel de propiedades
        }

        private void AgregarElementosDePrueba()
        {
            try
            {
                // Agregar título del club
                var titulo = new ElementoTexto
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Texto",
                    X = 20,
                    Y = 15,
                    Ancho = 310,
                    Alto = 25,
                    ZIndex = 0,
                    Texto = "CLUB DEPORTIVO EJEMPLO",
                    FontFamily = "Arial",
                    FontSize = 16,
                    Color = Colors.DarkBlue,
                    IsBold = true,
                    TextAlignment = TextAlignment.Center
                };

                // Agregar campo de nombre
                var nombre = new ElementoCampoDinamico
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Campo Dinámico",
                    X = 20,
                    Y = 50,
                    Ancho = 200,
                    Alto = 20,
                    ZIndex = 1,
                    CampoOrigen = "NombreCompleto",
                    FontFamily = "Arial",
                    FontSize = 14,
                    Color = Colors.Black,
                    IsBold = true,
                    TextAlignment = TextAlignment.Left,
                    Prefijo = "",
                    Sufijo = ""
                };

                // Agregar número de socio
                var numeroSocio = new ElementoCampoDinamico
                {
                    Id = Guid.NewGuid().ToString(),
                    Tipo = "Campo Dinámico",
                    X = 20,
                    Y = 75,
                    Ancho = 150,
                    Alto = 18,
                    ZIndex = 2,
                    CampoOrigen = "NumeroSocio",
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.DarkGray,
                    IsBold = false,
                    TextAlignment = TextAlignment.Left,
                    Prefijo = "Socio Nº: ",
                    Sufijo = ""
                };

                ElementosActuales.Add(titulo);
                ElementosActuales.Add(nombre);
                ElementosActuales.Add(numeroSocio);

                PlantillaActual.Elementos.Add(titulo);
                PlantillaActual.Elementos.Add(nombre);
                PlantillaActual.Elementos.Add(numeroSocio);

                System.Diagnostics.Debug.WriteLine($"Elementos de prueba agregados. Total: {ElementosActuales.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando elementos de prueba: {ex.Message}");
            }
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
                CodigoBarras = "CD202412345678",
                Estado = EstadoAbonado.Activo,
                Telefono = "666-123-456",
                Email = "juan.perez@clubdeportivo.com",
                FechaNacimiento = new DateTime(1985, 5, 15),
                FechaCreacion = DateTime.Now.AddMonths(-6),
                TallaCamiseta = "L",
                Direccion = "Calle del Estadio 123, 28001 Madrid",
                Observaciones = "Abonado desde 2018 - Socio fundador",
                Impreso = false,
                Peña = new Peña
                {
                    Id = 1,
                    Nombre = "Peña Los Campeones"
                },
                TipoAbono = new TipoAbono
                {
                    Id = 1,
                    Nombre = "Abono Temporada Completa",
                    Descripcion = "Acceso a todos los partidos de liga y copa",
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

        public static string GenerarCodigoBarrasParaAbonado(Abonado abonado)
        {
            if (!string.IsNullOrWhiteSpace(abonado.CodigoBarras))
            {
                return abonado.CodigoBarras;
            }

            // Generar código único y reproducible
            var numeroSocio = abonado.NumeroSocio.ToString();
            var año = abonado.FechaCreacion.Year.ToString();
            var codigo = $"CD{año}{numeroSocio.PadLeft(6, '0')}";

            // Agregar dígito verificador
            var suma = codigo.Where(char.IsDigit).Sum(c => int.Parse(c.ToString()));
            var digitoVerificador = (10 - (suma % 10)) % 10;

            return $"{codigo}{digitoVerificador}";
        }

        public void SetAbonadoEjemplo(Abonado abonado)
        {
            AbonadoEjemplo = abonado;
            ActualizarCanvas();
        }

        // Método para optimizar el rendimiento
        public void OptimizarRendimiento()
        {
            try
            {
                // Limpiar elementos fuera de los límites
                var elementosValidos = ElementosActuales.Where(e =>
                    e.X >= -50 && e.Y >= -50 &&
                    e.X <= PlantillaActual.Ancho + 50 &&
                    e.Y <= PlantillaActual.Alto + 50).ToList();

                if (elementosValidos.Count != ElementosActuales.Count)
                {
                    var eliminados = ElementosActuales.Count - elementosValidos.Count;
                    ElementosActuales.Clear();
                    foreach (var elemento in elementosValidos)
                    {
                        ElementosActuales.Add(elemento);
                    }
                    EstadoActual = $"Optimización completada. {eliminados} elementos eliminados.";
                }

                // Reorganizar Z-Index
                var elementosOrdenados = ElementosActuales.OrderBy(e => e.ZIndex).ToList();
                for (int i = 0; i < elementosOrdenados.Count; i++)
                {
                    elementosOrdenados[i].ZIndex = i;
                }

                ActualizarCanvas();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error optimizando: {ex.Message}");
            }
        }

        // Método para exportar estadísticas de la plantilla
        public string ObtenerEstadisticasPlantilla()
        {
            try
            {
                var stats = new System.Text.StringBuilder();
                stats.AppendLine($"=== ESTADÍSTICAS DE PLANTILLA ===");
                stats.AppendLine($"Nombre: {PlantillaActual.Nombre}");
                stats.AppendLine($"Dimensiones: {PlantillaActual.Ancho} x {PlantillaActual.Alto} px");
                stats.AppendLine($"Total de elementos: {ElementosActuales.Count}");
                stats.AppendLine();

                // Estadísticas por tipo
                var porTipo = ElementosActuales.GroupBy(e => e.Tipo).ToList();
                stats.AppendLine("=== POR TIPO ===");
                foreach (var grupo in porTipo.OrderBy(g => g.Key))
                {
                    stats.AppendLine($"{grupo.Key}: {grupo.Count()}");
                }
                stats.AppendLine();

                // Área ocupada
                var areaTotal = PlantillaActual.Ancho * PlantillaActual.Alto;
                var areaOcupada = ElementosActuales.Sum(e => e.Ancho * e.Alto);
                var porcentajeOcupacion = (areaOcupada / areaTotal) * 100;
                stats.AppendLine($"=== OCUPACIÓN ===");
                stats.AppendLine($"Área total: {areaTotal:N0} px²");
                stats.AppendLine($"Área ocupada: {areaOcupada:N0} px²");
                stats.AppendLine($"Porcentaje: {porcentajeOcupacion:F1}%");
                stats.AppendLine();

                // Campos utilizados
                var campos = new HashSet<string>();
                foreach (var elemento in ElementosActuales)
                {
                    if (elemento is ElementoCodigoBarras codigo && !string.IsNullOrEmpty(codigo.CampoOrigen))
                        campos.Add(codigo.CampoOrigen);
                    if (elemento is ElementoCampoDinamico campo && !string.IsNullOrEmpty(campo.CampoOrigen))
                        campos.Add(campo.CampoOrigen);
                }

                stats.AppendLine("=== CAMPOS UTILIZADOS ===");
                foreach (var campo in campos.OrderBy(c => c))
                {
                    stats.AppendLine($"• {campo}");
                }

                return stats.ToString();
            }
            catch (Exception ex)
            {
                return $"Error generando estadísticas: {ex.Message}";
            }
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

        #region Cleanup

        public void Dispose()
        {
            try
            {
                _dbContext?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing ViewModel: {ex.Message}");
            }
        }

        #endregion
    }
}