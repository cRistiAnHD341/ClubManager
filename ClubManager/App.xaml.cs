using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Inicializar servicios
                InitializeServices();

                // Verificar y crear base de datos
                await InitializeDatabase();

                // Verificar licencia
                await CheckLicense();

                var templateService = new TemplateService();

                // Crear y mostrar ventana principal
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

        private void InitializeServices()
        {
            // Inicializar servicios singleton
            var themeService = ThemeService.Instance;

            // Configurar directorio de datos
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ClubManager");

            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            // Configurar directorio de plantillas
            var templateDirectory = Path.Combine(dataDirectory, "Plantillas");
            if (!Directory.Exists(templateDirectory))
            {
                Directory.CreateDirectory(templateDirectory);
            }

            // Configurar directorio de backups
            var backupDirectory = Path.Combine(dataDirectory, "Backups");
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }
        }

        private async Task InitializeDatabase()
        {
            try
            {
                using var context = new ClubDbContext();
                await context.Database.EnsureCreatedAsync();

                // Verificar que hay datos iniciales
                await EnsureSeedData(context);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar la base de datos: {ex.Message}");
            }
        }

        private async Task EnsureSeedData(ClubDbContext context)
        {
            // Verificar si existe el usuario admin
            var adminExists = await context.Usuarios.AnyAsync(u => u.NombreUsuario == "admin");

            if (!adminExists)
            {
                // Crear usuario admin con permisos completos
                var adminUser = new Usuario
                {
                    NombreUsuario = "admin",
                    PasswordHash = HashPassword("admin123"),
                    NombreCompleto = "Administrador del Sistema",
                    Email = "admin@clubmanager.com",
                    Rol = "Administrador",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                context.Usuarios.Add(adminUser);
                await context.SaveChangesAsync();

                // Crear permisos para admin
                var permissions = new UserPermissions
                {
                    UsuarioId = adminUser.Id,
                    CanAccessDashboard = true,
                    CanAccessAbonados = true,
                    CanAccessTiposAbono = true,
                    CanAccessGestores = true,
                    CanAccessPeñas = true,
                    CanAccessUsuarios = true,
                    CanAccessHistorial = true,
                    CanAccessConfiguracion = true,
                    CanCreateAbonados = true,
                    CanEditAbonados = true,
                    CanDeleteAbonados = true,
                    CanExportData = true,
                    CanImportData = true,
                    CanManageSeasons = true,
                    CanChangeLicense = true,
                    CanCreateBackups = true,
                    CanAccessTemplates = true
                };

                context.UserPermissions.Add(permissions);
                await context.SaveChangesAsync();
            }
        }

        private async Task CheckLicense()
        {
            try
            {
                var licenseService = new LicenseService();
                licenseService.LoadSavedLicense();

                var licenseInfo = licenseService.GetCurrentLicenseInfo();

                if (licenseInfo.IsExpired)
                {
                    MessageBox.Show("La licencia ha expirado. Algunas funciones pueden estar limitadas.",
                                  "Licencia Expirada",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
                else if (!licenseInfo.IsValid)
                {
                    var result = MessageBox.Show("No se encontró una licencia válida. ¿Desea configurar la licencia ahora?",
                                                "Licencia Requerida",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var licenseWindow = new LicenseWindow();
                        licenseWindow.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando licencia: {ex.Message}");
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
            return Convert.ToBase64String(hashedBytes);
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

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Cerrar sesión al salir de la aplicación
                UserSession.Instance.Logout();

                // Limpiar recursos
                CleanupResources();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar aplicación: {ex.Message}");
            }

            base.OnExit(e);
        }

        private void CleanupResources()
        {
            // Limpiar archivos temporales
            var tempPath = Path.GetTempPath();
            var clubManagerTempFiles = Directory.GetFiles(tempPath, "ClubManager_*");

            foreach (var file in clubManagerTempFiles)
            {
                try
                {
                    if (File.GetCreationTime(file) < DateTime.Now.AddDays(-1))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignorar errores de limpieza
                }
            }
        }
    }
}