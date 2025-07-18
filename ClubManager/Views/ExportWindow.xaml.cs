using System.Collections.Generic;
using System.Windows;
using ClubManager.Models;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class ExportWindow : Window
    {
        public ExportWindow(List<Abonado>? abonados = null)
        {
            InitializeComponent();
            DataContext = new ExportViewModel(abonados);

            // Suscribirse al evento de exportación completada
            if (DataContext is ExportViewModel viewModel)
            {
                viewModel.ExportCompleted += (s, success) =>
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