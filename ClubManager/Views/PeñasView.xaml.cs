// PeñasView.xaml.cs
using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class PeñasView : UserControl
    {
        public PeñasView()
        {
            InitializeComponent();
            DataContext = new PeñasViewModel();
        }
    }
}
