using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    /// <summary>
    /// Lógica de interacción para ConfiguracionView.xaml
    /// </summary>
    public partial class ConfiguracionView : UserControl
    {
        public ConfiguracionView()
        {
            InitializeComponent();
            DataContext = new ConfiguracionViewModel();
        }
    }
}