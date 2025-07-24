using System.Windows;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class CsvImportWindow : Window
    {
        public CsvImportWindow()
        {
            InitializeComponent();

            // Crear y asignar el ViewModel
            var viewModel = new CsvImportViewModel();
            DataContext = viewModel;

            // Asegurar que el ViewModel se dispose cuando se cierre la ventana
            Closed += (s, e) => viewModel?.Dispose();
        }

        // Constructor que acepta un ViewModel (para testing o inyección de dependencias)
        public CsvImportWindow(CsvImportViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Asegurar que el ViewModel se dispose cuando se cierre la ventana
            Closed += (s, e) => viewModel?.Dispose();
        }
    }
}