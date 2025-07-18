using System;
using System.Windows;
using ClubManager.ViewModels;

namespace ClubManager
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}