using System.Windows;
using ClubManager.Models;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class AbonadoEditWindow : Window
    {
        public AbonadoEditWindow(Abonado? abonado = null)
        {
            InitializeComponent();
            DataContext = new AbonadoEditViewModel(abonado);

            // Suscribirse al evento de guardado exitoso
            if (DataContext is AbonadoEditViewModel viewModel)
            {
                viewModel.SaveCompleted += (s, success) =>
                {
                    if (success)
                    {
                        DialogResult = true;
                        Close();
                    }
                };
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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