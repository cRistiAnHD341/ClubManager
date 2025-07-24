using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Models;
using ClubManager.Views;
using Microsoft.Win32;

namespace ClubManager.ViewModels
{
    public class PlantillasManagerViewModel : BaseViewModel
    {
        private readonly string _carpetaPlantillas;

        // Colecciones
        private ObservableCollection<PlantillaTarjeta> _plantillas;
        private ObservableCollection<PlantillaTarjeta> _plantillasFiltradas;

        // Propiedades de filtros y búsqueda
        private string _textoBusqueda = "";
        private PlantillaTarjeta? _plantillaSeleccionada;
        private string _statusMessage = "Listo";

        public PlantillasManagerViewModel()
        {
            // Configurar carpeta de plantillas
            _carpetaPlantillas = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ClubManager",
                "Plantillas");

            // Asegurar que existe la carpeta
            try
            {
                Directory.CreateDirectory(_carpetaPlantillas);
            }
            catch { }

            // Inicializar colecciones
            _plantillas = new ObservableCollection<PlantillaTarjeta>();
            _plantillasFiltradas = new ObservableCollection<PlantillaTarjeta>();

            InitializeCommands();
            CargarPlantillas();
        }

        #region Properties

        public ObservableCollection<PlantillaTarjeta> PlantillasFiltradas => _plantillasFiltradas;

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                SetProperty(ref _textoBusqueda, value);
                AplicarFiltros();
            }
        }

        public PlantillaTarjeta? PlantillaSeleccionada
        {
            get => _plantillaSeleccionada;
            set => SetProperty(ref _plantillaSeleccionada, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Commands

        public ICommand ImportarPlantillaCommand { get; private set; } = null!;
        public ICommand ExportarPlantillaCommand { get; private set; } = null!;
        public ICommand NuevaPlantillaCommand { get; private set; } = null!;
        public ICommand EditarPlantillaCommand { get; private set; } = null!;
        public ICommand DuplicarPlantillaCommand { get; private set; } = null!;
        public ICommand EliminarPlantillaCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            ImportarPlantillaCommand = new RelayCommand(ImportarPlantilla);
            ExportarPlantillaCommand = new RelayCommand(ExportarPlantilla, () => PlantillaSeleccionada != null);
            NuevaPlantillaCommand = new RelayCommand(NuevaPlantilla);
            EditarPlantillaCommand = new RelayCommand<PlantillaTarjeta>(EditarPlantilla);
            DuplicarPlantillaCommand = new RelayCommand<PlantillaTarjeta>(DuplicarPlantilla);
            EliminarPlantillaCommand = new RelayCommand<PlantillaTarjeta>(EliminarPlantilla);
        }

        #endregion

        #region Command Methods

        private void ImportarPlantilla()
        {
            try
            {
                var dialogo = new OpenFileDialog
                {
                    Filter = "Plantillas de tarjeta (*.cardtemplate)|*.cardtemplate",
                    Title = "Importar plantilla"
                };

                if (dialogo.ShowDialog() == true)
                {
                    var contenido = File.ReadAllText(dialogo.FileName);
                    var plantilla = JsonSerializer.Deserialize<PlantillaTarjeta>(contenido);

                    if (plantilla != null)
                    {
                        // Generar nuevo ID y modificar nombre
                        plantilla.Id = Guid.NewGuid().ToString();
                        plantilla.Nombre = $"{plantilla.Nombre} (Importada)";
                        plantilla.FechaModificacion = DateTime.Now;

                        GuardarPlantilla(plantilla);
                        _plantillas.Add(plantilla);
                        AplicarFiltros();

                        StatusMessage = "Plantilla importada correctamente";
                        MessageBox.Show("Plantilla importada correctamente.", "Éxito",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al importar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error al importar plantilla";
            }
        }

        private void ExportarPlantilla()
        {
            if (PlantillaSeleccionada == null) return;

            try
            {
                var dialogo = new SaveFileDialog
                {
                    Filter = "Plantillas de tarjeta (*.cardtemplate)|*.cardtemplate",
                    Title = "Exportar plantilla",
                    FileName = PlantillaSeleccionada.Nombre.Replace(" ", "_")
                };

                if (dialogo.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(PlantillaSeleccionada, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(dialogo.FileName, json);
                    StatusMessage = $"Plantilla exportada: {Path.GetFileName(dialogo.FileName)}";

                    MessageBox.Show("Plantilla exportada correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error al exportar plantilla";
            }
        }

        private void NuevaPlantilla()
        {
            try
            {
                //var cardDesigner = new CardDesignerWindow();
                //cardDesigner.ShowDialog();

                // Recargar plantillas por si se guardó una nueva
                CargarPlantillas();
                StatusMessage = "Diseñador de tarjetas abierto";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir diseñador: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditarPlantilla(PlantillaTarjeta? plantilla)
        {
            if (plantilla == null) return;

            try
            {
                // Cargar plantilla en el diseñador
                var cardDesigner = new CardDesignerView();
                // TODO: Pasar la plantilla al diseñador para editarla
                //cardDesigner.ShowDialog();

                CargarPlantillas();
                StatusMessage = $"Editando plantilla: {plantilla.Nombre}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DuplicarPlantilla(PlantillaTarjeta? plantilla)
        {
            if (plantilla == null) return;

            try
            {
                var nuevaPlantilla = new PlantillaTarjeta
                {
                    Id = Guid.NewGuid().ToString(),
                    Nombre = $"{plantilla.Nombre} (Copia)",
                    Descripcion = plantilla.Descripcion,
                    Ancho = plantilla.Ancho,
                    Alto = plantilla.Alto,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    Elementos = plantilla.Elementos.Select(e => CloneElement(e)).ToList()
                };

                GuardarPlantilla(nuevaPlantilla);
                _plantillas.Add(nuevaPlantilla);
                AplicarFiltros();

                StatusMessage = $"Plantilla duplicada: {nuevaPlantilla.Nombre}";
                MessageBox.Show("Plantilla duplicada correctamente.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al duplicar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EliminarPlantilla(PlantillaTarjeta? plantilla)
        {
            if (plantilla == null) return;

            var result = MessageBox.Show($"¿Está seguro de eliminar la plantilla '{plantilla.Nombre}'?\n\nEsta acción no se puede deshacer.",
                                       "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var archivo = Path.Combine(_carpetaPlantillas, $"{plantilla.Id}.cardtemplate");
                    if (File.Exists(archivo))
                    {
                        File.Delete(archivo);
                    }

                    _plantillas.Remove(plantilla);
                    AplicarFiltros();

                    StatusMessage = $"Plantilla eliminada: {plantilla.Nombre}";
                    MessageBox.Show("Plantilla eliminada correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar plantilla: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Methods

        private void CargarPlantillas()
        {
            try
            {
                _plantillas.Clear();

                if (!Directory.Exists(_carpetaPlantillas)) return;

                var archivos = Directory.GetFiles(_carpetaPlantillas, "*.cardtemplate");

                foreach (var archivo in archivos)
                {
                    try
                    {
                        var json = File.ReadAllText(archivo);
                        var plantilla = JsonSerializer.Deserialize<PlantillaTarjeta>(json);
                        if (plantilla != null)
                        {
                            _plantillas.Add(plantilla);
                        }
                    }
                    catch
                    {
                        // Ignorar archivos corruptos
                    }
                }

                AplicarFiltros();
                StatusMessage = $"{_plantillas.Count} plantillas cargadas";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cargando plantillas: {ex.Message}";
            }
        }

        public void AplicarFiltros()
        {
            try
            {
                var plantillasFiltradas = _plantillas.AsEnumerable();

                // Filtro de búsqueda
                if (!string.IsNullOrWhiteSpace(TextoBusqueda))
                {
                    var busqueda = TextoBusqueda.ToLower();
                    plantillasFiltradas = plantillasFiltradas.Where(p =>
                        p.Nombre.ToLower().Contains(busqueda) ||
                        (p.Descripcion?.ToLower().Contains(busqueda) ?? false));
                }

                // Ordenar por fecha de modificación (más recientes primero)
                plantillasFiltradas = plantillasFiltradas.OrderByDescending(p => p.FechaModificacion);

                _plantillasFiltradas.Clear();
                foreach (var plantilla in plantillasFiltradas)
                {
                    _plantillasFiltradas.Add(plantilla);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error aplicando filtros: {ex.Message}";
            }
        }

        public void GuardarPlantilla(PlantillaTarjeta plantilla)
        {
            try
            {
                var archivo = Path.Combine(_carpetaPlantillas, $"{plantilla.Id}.cardtemplate");
                var json = JsonSerializer.Serialize(plantilla, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(archivo, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar plantilla: {ex.Message}");
            }
        }

        public ElementoTarjeta CloneElement(ElementoTarjeta original)
        {
            return original switch
            {
                ElementoTexto texto => new ElementoTexto
                {
                    Id = Guid.NewGuid().ToString(),
                    X = texto.X,
                    Y = texto.Y,
                    Ancho = texto.Ancho,
                    Alto = texto.Alto,
                    ZIndex = texto.ZIndex,
                    Texto = texto.Texto,
                    FontFamily = texto.FontFamily,
                    FontSize = texto.FontSize,
                    Color = texto.Color,
                    IsBold = texto.IsBold,
                    IsItalic = texto.IsItalic,
                    TextAlignment = texto.TextAlignment
                },
                ElementoImagen imagen => new ElementoImagen
                {
                    Id = Guid.NewGuid().ToString(),
                    X = imagen.X,
                    Y = imagen.Y,
                    Ancho = imagen.Ancho,
                    Alto = imagen.Alto,
                    ZIndex = imagen.ZIndex,
                    RutaImagen = imagen.RutaImagen,
                    Stretch = imagen.Stretch
                },
                ElementoCodigo codigo => new ElementoCodigo
                {
                    Id = Guid.NewGuid().ToString(),
                    X = codigo.X,
                    Y = codigo.Y,
                    Ancho = codigo.Ancho,
                    Alto = codigo.Alto,
                    ZIndex = codigo.ZIndex,
                    TipoCodigo = codigo.TipoCodigo,
                    CampoOrigen = codigo.CampoOrigen
                },
                _ => original
            };
        }

        #endregion
    }
}