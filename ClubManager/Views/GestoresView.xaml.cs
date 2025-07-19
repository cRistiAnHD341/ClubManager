using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class GestoresView : UserControl
    {
        public GestoresView()
        {
            InitializeComponent();
            DataContext = new GestoresViewModel();
        }
    }
}