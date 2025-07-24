using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private string _username = "";
        private string _password = "";
        private string _errorMessage = "";
        private bool _isLoggingIn = false;
        private bool _rememberMe = false;

        public LoginViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            LoginCommand = new RelayCommand(async () => await Login(), CanLogin);
            CancelCommand = new RelayCommand(Cancel);

            // Verificar licencia al iniciar
            CheckLicense();
        }

        #region Properties

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        private bool CanLogin() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoggingIn;

        private async Task Login()
        {
            try
            {
                IsLoggingIn = true;
                ErrorMessage = "";

                // Verificar licencia antes del login
                var licenseInfo = _licenseService.GetCurrentLicenseInfo();
                if (!licenseInfo.IsValid || licenseInfo.IsExpired)
                {
                    var licenseWindow = new LicenseWindow();
                    var result = licenseWindow.ShowDialog();

                    if (result != true)
                    {
                        ErrorMessage = "Se requiere una licencia válida para usar la aplicación";
                        return;
                    }

                    // Recargar información de licencia
                    _licenseService.LoadSavedLicense();
                    licenseInfo = _licenseService.GetCurrentLicenseInfo();

                    if (!licenseInfo.IsValid || licenseInfo.IsExpired)
                    {
                        ErrorMessage = "La licencia sigue siendo inválida";
                        return;
                    }
                }

                // Autenticar usuario
                var passwordHash = HashPassword(Password);
                var user = await _dbContext.Usuarios
                    .Include(u => u.Permissions)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == Username && u.PasswordHash == passwordHash && u.Activo);

                if (user?.Permissions == null)
                {
                    ErrorMessage = "Usuario o contraseña incorrectos";
                    return;
                }

                // Actualizar último acceso
                user.UltimoAcceso = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                // Registrar acción
                await LogAction(user.Id, $"Inicio de sesión: {Username}");

                // Establecer sesión
                UserSession.Instance.Login(user, user.Permissions);

                // Cerrar ventana de login
                CloseLoginWindow();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al iniciar sesión: {ex.Message}";
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private void Cancel()
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Methods

        private void CheckLicense()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();

            if (!licenseInfo.IsValid || licenseInfo.IsExpired)
            {
                ErrorMessage = "⚠️ Se requiere licencia válida";
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
            return Convert.ToBase64String(hashedBytes);
        }

        private async Task LogAction(int userId, string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = userId,
                    Accion = action,
                    TipoAccion = "Login",
                    FechaHora = DateTime.Now
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch { }
        }

        private void CloseLoginWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is LoginWindow)
                {
                    window.DialogResult = true;
                    window.Close();
                    break;
                }
            }
        }

        #endregion
    }
}