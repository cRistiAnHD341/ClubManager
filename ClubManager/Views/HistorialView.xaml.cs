// HistorialView.xaml.cs
using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class HistorialView : UserControl
    {
        public HistorialView()
        {
            InitializeComponent();
            DataContext = new HistorialViewModel();
        }
    }
}