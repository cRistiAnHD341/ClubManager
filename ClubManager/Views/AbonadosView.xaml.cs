using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class AbonadosView : UserControl
    {
        public AbonadosView()
        {
            InitializeComponent();
            DataContext = new AbonadosViewModel();
        }
    }
}