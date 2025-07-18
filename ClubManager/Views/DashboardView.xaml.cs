using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }
    }
}