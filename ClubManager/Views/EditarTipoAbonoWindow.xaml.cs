using System.Windows;
using ClubManager.Models;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    /// <summary>
    /// Lógica de interacción para EditarTipoAbonoWindow.xaml
    /// </summary>
    public partial class EditarTipoAbonoWindow : Window
    {
        private readonly EditarTipoAbonoViewModel _viewModel;

        public EditarTipoAbonoWindow(TipoAbono? tipoAbono = null)
        {
            InitializeComponent();

            _viewModel = new EditarTipoAbonoViewModel(tipoAbono);
            DataContext = _viewModel;

            // Suscribirse al evento de guardado completado
            _viewModel.SaveCompleted += OnSaveCompleted;

            // Enfocar el campo de nombre al abrir
            Loaded += (s, e) => NombreTextBox.Focus();
            // Configurar ventana
            if (tipoAbono != null)
            {
                Title = $"Editar Tipo de Abono - {tipoAbono.Nombre}";
            }
            else
            {
                Title = "Nuevo Tipo de Abono - ClubManager";
            }
        }

        private void OnSaveCompleted(object? sender, bool success)
        {
            if (success)
            {
                DialogResult = true;
                Close();
            }
            // Si no tuvo éxito, mantener la ventana abierta para correcciones
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Está seguro de que desea cancelar?\nSe perderán todos los cambios no guardados.",
                "Confirmar Cancelación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // Limpiar recursos del ViewModel
            _viewModel.SaveCompleted -= OnSaveCompleted;
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}