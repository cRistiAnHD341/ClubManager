using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ClubManager.Commands;
using ClubManager.Models;
using ClubManager.Services;

namespace ClubManager.Views
{
    public partial class CardPreviewWindow : Window
    {
        private readonly CardPreviewViewModel _viewModel;

        public CardPreviewWindow(Abonado abonado, PlantillaTarjeta plantilla, ICardPrintService printService)
        {
            InitializeComponent();

            _viewModel = new CardPreviewViewModel(abonado, plantilla, printService);
            DataContext = _viewModel;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Generar vista previa de la tarjeta
                var tarjetaElement = await _viewModel.GenerarVistaPreviaAsync();
                if (tarjetaElement != null)
                {
                    TarjetaContent.Content = tarjetaElement;

                    // Ajustar tamaño del contenedor
                    TarjetaContainer.Width = _viewModel.Plantilla.Ancho;
                    TarjetaContainer.Height = _viewModel.Plantilla.Alto;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar vista previa: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
    }

    public class CardPreviewViewModel
    {
        public Abonado Abonado { get; }
        public PlantillaTarjeta Plantilla { get; private set; }
        public ICardPrintService PrintService { get; }

        public string AbonadoNombre => Abonado.NombreCompleto;
        public string PlantillaNombre => Plantilla.Nombre;

        public ICommand CambiarPlantillaCommand { get; }
        public ICommand GuardarImagenCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand ImprimirCommand { get; }

        private Window? _window;

        public CardPreviewViewModel(Abonado abonado, PlantillaTarjeta plantilla, ICardPrintService printService)
        {
            Abonado = abonado;
            Plantilla = plantilla;
            PrintService = printService;

            CambiarPlantillaCommand = new RelayCommand(CambiarPlantilla);
            GuardarImagenCommand = new RelayCommand(GuardarImagen);
            CancelarCommand = new RelayCommand(Cancelar);
            ImprimirCommand = new RelayCommand(Imprimir);
        }

        public void SetWindow(Window window)
        {
            _window = window;
        }

        public async Task<FrameworkElement?> GenerarVistaPreviaAsync()
        {
            try
            {
                return await PrintService.GenerarVistaPreviaAsync(Abonado, Plantilla);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando vista previa: {ex.Message}");
                return null;
            }
        }

        private async void CambiarPlantilla()
        {
            try
            {
                var templateService = new TemplateService();
                var plantillas = await templateService.GetPlantillasAsync();

                if (plantillas.Count <= 1)
                {
                    MessageBox.Show("No hay otras plantillas disponibles. Abra el diseñador para crear más plantillas.",
                                  "Sin plantillas", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Aquí podrías abrir un diálogo de selección de plantilla
                // Por simplicidad, usamos la siguiente plantilla disponible
                var currentIndex = plantillas.FindIndex(p => p.Id == Plantilla.Id);
                var nextIndex = (currentIndex + 1) % plantillas.Count;

                Plantilla = plantillas[nextIndex];

                // Regenerar vista previa
                if (_window is CardPreviewWindow previewWindow)
                {
                    var nuevaTarjeta = await GenerarVistaPreviaAsync();
                    if (nuevaTarjeta != null)
                    {
                        previewWindow.TarjetaContent.Content = nuevaTarjeta;
                        previewWindow.TarjetaContainer.Width = Plantilla.Ancho;
                        previewWindow.TarjetaContainer.Height = Plantilla.Alto;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar plantilla: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GuardarImagen()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Imagen PNG (*.png)|*.png|Imagen JPEG (*.jpg)|*.jpg",
                    Title = "Guardar tarjeta como imagen",
                    FileName = $"Tarjeta_{Abonado.NombreCompleto.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var imagen = await PrintService.GenerarImagenTarjetaAsync(Abonado, Plantilla);
                    if (imagen != null)
                    {
                        BitmapEncoder encoder = saveDialog.FilterIndex == 1
                            ? new PngBitmapEncoder()
                            : new JpegBitmapEncoder();

                        encoder.Frames.Add(BitmapFrame.Create(imagen));

                        using (var stream = new FileStream(saveDialog.FileName, FileMode.Create))
                        {
                            encoder.Save(stream);
                        }

                        MessageBox.Show("Imagen guardada correctamente.", "Éxito",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar imagen: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar()
        {
            _window?.Close();
        }

        private async void Imprimir()
        {
            try
            {
                var result = MessageBox.Show(
                    $"¿Confirma que desea imprimir la tarjeta de {Abonado.NombreCompleto}?",
                    "Confirmar Impresión",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var exitoso = await PrintService.ImprimirTarjetaAsync(Abonado, Plantilla);

                    if (exitoso)
                    {
                        _window.DialogResult = true; // Indica que se imprimió correctamente
                        _window.Close();
                    }
                    else
                    {
                        MessageBox.Show("Error durante la impresión.", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}