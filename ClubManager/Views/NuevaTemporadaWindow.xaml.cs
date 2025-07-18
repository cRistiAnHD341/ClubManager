using System.Windows;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class NuevaTemporadaWindow : Window
    {
        public NuevaTemporadaWindow()
        {
            InitializeComponent();
            DataContext = new NuevaTemporadaViewModel();

            // Suscribirse al evento de temporada creada
            if (DataContext is NuevaTemporadaViewModel viewModel)
            {
                viewModel.SeasonCreated += (s, success) =>
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
    }
}