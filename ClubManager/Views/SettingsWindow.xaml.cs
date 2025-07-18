using System.Windows;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();

            // Suscribirse al evento de configuración guardada
            if (DataContext is SettingsViewModel viewModel)
            {
                viewModel.SettingsSaved += (s, success) =>
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