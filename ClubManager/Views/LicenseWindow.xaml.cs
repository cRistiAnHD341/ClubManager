using System.Windows;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
            DataContext = new LicenseViewModel();

            // Suscribirse al evento de licencia activada
            if (DataContext is LicenseViewModel viewModel)
            {
                viewModel.LicenseActivated += (s, e) =>
                {
                    DialogResult = true;
                    Close();
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