using System.Windows.Controls;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class CardDesignerView : UserControl
    {
        public CardDesignerView()
        {
            InitializeComponent();
            DataContext = new CardDesignerViewModel();
        }
    }
}