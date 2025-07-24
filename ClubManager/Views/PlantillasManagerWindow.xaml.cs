// PlantillasManagerWindow.xaml.cs - Code-behind simplificado
using System.Windows;
using ClubManager.ViewModels;
using ClubManager.Models;

namespace ClubManager.Views
{
    public partial class PlantillasManagerWindow : Window
    {
        public PlantillaTarjeta? PlantillaSeleccionada { get; private set; }

        public PlantillasManagerWindow()
        {
            InitializeComponent();

            // Crear ViewModel sin parámetros
            var viewModel = new PlantillasManagerViewModel();
            DataContext = viewModel;

            // Suscribirse a cambios de selección
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlantillasManagerViewModel.PlantillaSeleccionada) &&
                DataContext is PlantillasManagerViewModel viewModel)
            {
                PlantillaSeleccionada = viewModel.PlantillaSeleccionada;
            }
        }

        private void PlantillaCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element &&
                element.Tag is PlantillaTarjeta plantilla &&
                DataContext is PlantillasManagerViewModel viewModel)
            {
                // Selección simple con clic
                if (e.ClickCount == 1)
                {
                    viewModel.PlantillaSeleccionada = plantilla;
                }
                // Doble clic para seleccionar y cerrar
                else if (e.ClickCount == 2)
                {
                    viewModel.PlantillaSeleccionada = plantilla;
                    //viewModel.SeleccionarCommand.Execute(null);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Configurar focus para recibir eventos de teclado
            this.Focusable = true;
            this.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Limpiar suscripciones
            if (DataContext is PlantillasManagerViewModel viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }
    }
}