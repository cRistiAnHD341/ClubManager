using ClubManager.ViewModels;

namespace ClubManager.Views
{
    // EditarUsuarioWindow.xaml.cs
    public partial class EditarUsuarioWindow : Window
    {
        public EditarUsuarioWindow(Usuario? usuario = null)
        {
            InitializeComponent();
            DataContext = new EditarUsuarioViewModel(usuario);
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditarUsuarioViewModel vm && sender is PasswordBox pb)
            {
                vm.Password = pb.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditarUsuarioViewModel vm && sender is PasswordBox pb)
            {
                vm.ConfirmPassword = pb.Password;
            }
        }
    }

    // PermissionsWindow.xaml.cs
    public partial class PermissionsWindow : Window
    {
        public PermissionsWindow(Usuario usuario)
        {
            InitializeComponent();
            DataContext = new PermissionsViewModel(usuario);
        }
    }

    // CambiarPasswordWindow.xaml.cs
    public partial class CambiarPasswordWindow : Window
    {
        public CambiarPasswordWindow(Usuario usuario)
        {
            InitializeComponent();
            DataContext = new CambiarPasswordViewModel(usuario);
        }

        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is CambiarPasswordViewModel vm && sender is PasswordBox pb)
            {
                vm.CurrentPassword = pb.Password;
            }
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is CambiarPasswordViewModel vm && sender is PasswordBox pb)
            {
                vm.NewPassword = pb.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is CambiarPasswordViewModel vm && sender is PasswordBox pb)
            {
                vm.ConfirmPassword = pb.Password;
            }
        }
    }

    // LicenseWindow.xaml.cs
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
            DataContext = new LicenseViewModel();
        }

        private void AceptarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public partial class ReportWindow : Window
    {
        public ReportWindow()
        {
            InitializeComponent();
            DataContext = new ReportWindowViewModel();

            Title = "📊 Generador de Reportes - ClubManager";

            // Configurar la ventana
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Manejar el evento de cierre
            Closing += ReportWindow_Closing;
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReportWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Limpiar recursos del ViewModel
            if (DataContext is ReportWindowViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}
