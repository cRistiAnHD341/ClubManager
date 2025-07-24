using System;
using System.Windows;
using ClubManager.ViewModels;
using ClubManager.Views;
using ClubManager.Models;

namespace ClubManager.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            ConfigureForLogin();
            Loaded += MainWindow_Loaded;
        }

        private void ConfigureForLogin()
        {
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            _viewModel.IsLoggedIn = false;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await System.Threading.Tasks.Task.Delay(500);
            ShowLoginDialog();
        }

        private async void ShowLoginDialog()
        {
            while (!UserSession.Instance.IsLoggedIn)
            {
                var loginWindow = new LoginWindow()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var result = loginWindow.ShowDialog();

                if (result == true && UserSession.Instance.IsLoggedIn)
                {
                    if (_viewModel != null)
                    {
                        await _viewModel.RefreshUserInterface();
                    }
                    break;
                }
                else
                {
                    var exitResult = MessageBox.Show(
                        "¿Deseas salir de la aplicación?",
                        "Confirmar Salida",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (exitResult == MessageBoxResult.Yes)
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}