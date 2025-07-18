using System;
using System.Windows;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Verificar licencia al inicio
                var licenseService = new LicenseService();
                licenseService.LoadSavedLicense();
                var licenseInfo = licenseService.GetCurrentLicenseInfo();

                // Si no hay licencia válida, mostrar ventana de activación
                if (!licenseInfo.IsValid)
                {
                    var licenseWindow = new LicenseWindow();
                    var result = licenseWindow.ShowDialog();

                    // Si el usuario cancela o no activa una licencia válida
                    if (result != true)
                    {
                        // Verificar si ahora tiene una licencia válida (aunque sea expirada)
                        licenseInfo = licenseService.GetCurrentLicenseInfo();
                        if (!licenseInfo.IsValid)
                        {
                            MessageBox.Show("No se puede continuar sin una licencia válida.\n\n" +
                                          "Contacta con el administrador para obtener una clave de licencia.",
                                          "Licencia Requerida",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Warning);
                            Shutdown();
                            return;
                        }
                    }
                }

                // Mostrar mensaje de bienvenida si la licencia está expirada
                licenseInfo = licenseService.GetCurrentLicenseInfo();
                if (licenseInfo.IsValid && licenseInfo.IsExpired)
                {
                    MessageBox.Show("Tu licencia ha expirado.\n\n" +
                                  "El programa funcionará en modo solo lectura.\n" +
                                  "Puedes consultar los datos pero no realizar modificaciones.",
                                  "Licencia Expirada",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }

                // Crear y mostrar la ventana principal
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar la aplicación:\n\n{ex.Message}",
                              "Error de Inicio",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Ha ocurrido un error inesperado:\n\n{e.Exception.Message}",
                          "Error",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}