// TiposAbonoView.xaml.cs
using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class TiposAbonoView : UserControl
    {
        public TiposAbonoView()
        {
            InitializeComponent();
            DataContext = new TiposAbonoViewModel();
        }
    }
}