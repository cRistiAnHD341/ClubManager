using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ClubManager.Commands;
using ClubManager.Models;
using Microsoft.Win32;

namespace ClubManager.ViewModels
{
    public class CardDesignerViewModel : BaseViewModel, IDisposable
    {
        private PlantillaTarjeta _plantillaActual;
        private ObservableCollection<ElementoTarjeta> _elementosActuales;
        private ElementoTarjeta? _elementoSeleccionado;
        private string _estadoActual = "Listo";
        private bool _mostrarGrilla = true;
        private bool _mostrarRegla = false;
        public bool TieneElementoSeleccionado => ElementoSeleccionado != null;
        public bool TieneElementos => ElementosActuales?.Count > 0;
        public bool EsElementoTexto => ElementoSeleccionado is ElementoTexto;
        public ElementoTexto ElementoTextoSeleccionado => ElementoSeleccionado as ElementoTexto;

        public CardDesignerViewModel()
        {
            _plantillaActual = new PlantillaTarjeta
            {
                Nombre = "Nueva Plantilla",
                Descripcion = "Plantilla de tarjeta personalizada",
                Ancho = 350,
                Alto = 220
            };

            _elementosActuales = new ObservableCollection<ElementoTarjeta>();

            InitializeCommands();
        }

        #region Properties

        public PlantillaTarjeta PlantillaActual
        {
            get => _plantillaActual;
            set => SetProperty(ref _plantillaActual, value);
        }

        public ObservableCollection<ElementoTarjeta> ElementosActuales
        {
            get => _elementosActuales;
            set => SetProperty(ref _elementosActuales, value);
        }

        public ElementoTarjeta? ElementoSeleccionado
        {
            get => _elementoSeleccionado;
            set => SetProperty(ref _elementoSeleccionado, value);
        }

        public string EstadoActual
        {
            get => _estadoActual;
            set => SetProperty(ref _estadoActual, value);
        }

        public bool MostrarGrilla
        {
            get => _mostrarGrilla;
            set => SetProperty(ref _mostrarGrilla, value);
        }

        public bool MostrarRegla
        {
            get => _mostrarRegla;
            set => SetProperty(ref _mostrarRegla, value);
        }

        #endregion

        #region Commands

        public ICommand AgregarTextoCommand { get; private set; } = null!;
        public ICommand AgregarImagenCommand { get; private set; } = null!;
        public ICommand AgregarCodigoBarrasCommand { get; private set; } = null!;
        public ICommand AgregarCampoDinamicoCommand { get; private set; } = null!;
        public ICommand EliminarElementoCommand { get; private set; } = null!;
        public ICommand NuevaPlantillaCommand { get; private set; } = null!;
        public ICommand GuardarPlantillaCommand { get; private set; } = null!;
        public ICommand CargarPlantillaCommand { get; private set; } = null!;
        public ICommand ExportarPlantillaCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AgregarTextoCommand = new RelayCommand(AgregarTexto);
            AgregarImagenCommand = new RelayCommand(AgregarImagen);
            AgregarCodigoBarrasCommand = new RelayCommand(AgregarCodigoBarras);
            AgregarCampoDinamicoCommand = new RelayCommand(AgregarCampoDinamico);
            EliminarElementoCommand = new RelayCommand(EliminarElemento, () => ElementoSeleccionado != null);
            NuevaPlantillaCommand = new RelayCommand(NuevaPlantilla);
            GuardarPlantillaCommand = new RelayCommand(GuardarPlantilla);
            CargarPlantillaCommand = new RelayCommand(CargarPlantilla);
            ExportarPlantillaCommand = new RelayCommand(ExportarPlantilla);
        }

        #endregion

        #region Command Methods

        private void AgregarTexto()
        {
            try
            {
                var elemento = new ElementoTexto
                {
                    X = 50,
                    Y = 50,
                    Ancho = 150,
                    Alto = 30,
                    Texto = "Texto de ejemplo",
                    FontFamily = "Arial",
                    FontSize = 14,
                    Color = Colors.Black,
                    ZIndex = ElementosActuales.Count
                };

                ElementosActuales.Add(elemento);
                ElementoSeleccionado = elemento;
                PlantillaActual.Elementos.Add(elemento);
                EstadoActual = "Elemento de texto agregado";
                ActualizarVistaPreviaElementos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar texto: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al agregar texto";
            }
        }

        private void AgregarImagen()
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "Archivos de Imagen (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
                    Title = "Seleccionar Imagen"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var elemento = new ElementoImagen
                    {
                        X = 50,
                        Y = 100,
                        Ancho = 100,
                        Alto = 100,
                        RutaImagen = openDialog.FileName,
                        Stretch = "Uniform",
                        ZIndex = ElementosActuales.Count
                    };

                    ElementosActuales.Add(elemento);
                    ElementoSeleccionado = elemento;
                    PlantillaActual.Elementos.Add(elemento);
                    EstadoActual = "Elemento de imagen agregado";
                    ActualizarVistaPreviaElementos();
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
                var elemento = new ElementoCodigo
                {
                    X = 50,
                    Y = 150,
                    Ancho = 150,
                    Alto = 50,
                    TipoCodigo = "CodigoBarras",
                    CampoOrigen = "CodigoBarras",
                    ZIndex = ElementosActuales.Count
                };

                ElementosActuales.Add(elemento);
                ElementoSeleccionado = elemento;
                PlantillaActual.Elementos.Add(elemento);
                EstadoActual = "Elemento de código de barras agregado";
                ActualizarVistaPreviaElementos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar código de barras: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al agregar código de barras";
            }
        }

        private void AgregarCampoDinamico()
        {
            try
            {
                var elemento = new ElementoCampoDinamico
                {
                    X = 200,
                    Y = 50,
                    Ancho = 120,
                    Alto = 25,
                    CampoOrigen = "NombreCompleto",
                    Texto = "{NombreCompleto}",
                    FontFamily = "Arial",
                    FontSize = 12,
                    Color = Colors.Black,
                    ZIndex = ElementosActuales.Count
                };

                ElementosActuales.Add(elemento);
                ElementoSeleccionado = elemento;
                PlantillaActual.Elementos.Add(elemento);
                EstadoActual = "Campo dinámico agregado";
                ActualizarVistaPreviaElementos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar campo dinámico: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al agregar campo dinámico";
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
                    ActualizarVistaPreviaElementos();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar elemento: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                EstadoActual = "Error al eliminar elemento";
            }
        }

        private void NuevaPlantilla()
        {
            try
            {
                var result = MessageBox.Show("¿Está seguro de crear una nueva plantilla? Se perderán los cambios no guardados.",
                                           "Nueva plantilla", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    PlantillaActual = new PlantillaTarjeta
                    {
                        Nombre = "Nueva Plantilla",
                        Descripcion = "Plantilla de tarjeta personalizada",
                        Ancho = 350,
                        Alto = 220
                    };

                    ElementosActuales.Clear();
                    ElementoSeleccionado = null;
                    EstadoActual = "Nueva plantilla creada";
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
                        ActualizarVistaPreviaElementos();
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

        private void ExportarPlantilla()
        {
            try
            {
                // Aquí se abriría el gestor de plantillas
                MessageBox.Show("Funcionalidad del gestor de plantillas disponible próximamente.\n\n" +
                               "Permitirá:\n" +
                               "• Ver todas las plantillas guardadas\n" +
                               "• Organizar plantillas por categorías\n" +
                               "• Compartir plantillas entre usuarios\n" +
                               "• Importar/Exportar plantillas",
                               "Gestor de Plantillas", MessageBoxButton.OK, MessageBoxImage.Information);
                EstadoActual = "Gestor de plantillas - En desarrollo";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir gestor: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private void ActualizarVistaPreviaElementos()
        {
            try
            {
                // Aquí se actualizaría la vista previa en el canvas
                // En una implementación completa, se renderizarían los elementos visualmente

                // Por ahora solo notificamos el cambio
                OnPropertyChanged(nameof(ElementosActuales));
                OnPropertyChanged(nameof(PlantillaActual));

                // Actualizar índices Z
                for (int i = 0; i < ElementosActuales.Count; i++)
                {
                    ElementosActuales[i].ZIndex = i;
                }
            }
            catch (Exception ex)
            {
                EstadoActual = $"Error actualizando vista previa: {ex.Message}";
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                ElementosActuales?.Clear();
                ElementoSeleccionado = null;
            }
            catch
            {
                // Ignorar errores al limpiar
            }
        }

        #endregion
    }

    #region Helper Classes

    // Enum para tipos de elementos de tarjeta
    public enum TipoElementoTarjeta
    {
        Texto,
        Imagen,
        CodigoBarras,
        QR,
        Rectangulo,
        Linea,
        CampoDinamico
    }

    // Clase para elementos de campo dinámico
    public class ElementoCampoDinamico : ElementoTarjeta
    {
        public string Texto { get; set; } = "";
        public string CampoOrigen { get; set; } = "";
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public System.Windows.Media.Color Color { get; set; } = System.Windows.Media.Colors.Black;
        public string TextAlignment { get; set; } = "Left";

        public override string Tipo => "Campo Dinámico";
    }

    #endregion
}