using System.Windows;
using ClubManager.Models;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class EditarPeñaWindow : Window
    {
        public EditarPeñaWindow()
        {
            InitializeComponent();
            // Para nuevas peñas
            DataContext = new EditarPeñaViewModel();
        }

        public EditarPeñaWindow(Peña peña) : this()
        {
            // Para editar peñas existentes
            DataContext = new EditarPeñaViewModel(peña);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Enfocar el campo de nombre al cargar
            NombreTextBox.Focus();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            // Limpiar recursos al cerrar
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}