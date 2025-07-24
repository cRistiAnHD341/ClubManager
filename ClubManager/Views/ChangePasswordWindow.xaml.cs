// ChangePasswordWindow.xaml.cs
using System.Windows;
using ClubManager.Models;
using ClubManager.ViewModels;

namespace ClubManager.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private ChangePasswordViewModel? _viewModel;

        public ChangePasswordWindow(Usuario usuario)
        {
            InitializeComponent();
            _viewModel = new ChangePasswordViewModel(usuario);
            DataContext = _viewModel;

            _viewModel.PasswordChangeCompleted += OnPasswordChangeCompleted;
        }

        private void OnPasswordChangeCompleted(object? sender, bool success)
        {
            if (success)
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.NewPassword = NewPasswordBox.Password;
            }
        }

        private void ConfirmNewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ConfirmNewPassword = ConfirmNewPasswordBox.Password;
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PasswordChangeCompleted -= OnPasswordChangeCompleted;
                _viewModel.Dispose();
            }
            base.OnClosed(e);
        }
    }
}
