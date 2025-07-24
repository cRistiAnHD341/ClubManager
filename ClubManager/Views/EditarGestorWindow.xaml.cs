using System.Windows;
using ClubManager.Models;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class EditarGestorWindow : Window
    {
        public EditarGestorWindow()
        {
            InitializeComponent();
            // Para nuevos gestores
            DataContext = new EditarGestorViewModel();
        }

        public EditarGestorWindow(Gestor gestor) : this()
        {
            // Para editar gestores existentes
            DataContext = new EditarGestorViewModel(gestor);
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

        public void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditarGestorViewModel vm)
            {
                MessageBox.Show($"DataContext OK!\n" +
                              $"Nombre: '{vm.Nombre}'\n" +
                              $"CanGuardar: {vm.CanGuardar()}\n" +
                              $"WindowTitle: {vm.WindowTitle}",
                              "Test DataContext");
            }
            else
            {
                MessageBox.Show("DataContext is NULL!", "Error");
            }
        }
    }
}