using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;

namespace ClubManager.ViewModels
{
    public class CambiarPasswordViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly Usuario _usuario;

        private string _currentPassword = "";
        private string _newPassword = "";
        private string _confirmPassword = "";
        private string _errorMessage = "";

        public CambiarPasswordViewModel(Usuario usuario)
        {
            _dbContext = new ClubDbContext();
            _usuario = usuario;

            InitializeCommands();
        }

        #region Properties

        public string UsuarioNombre => _usuario.NombreUsuario;

        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                SetProperty(ref _currentPassword, value);
                ValidateCurrentPassword();
            }
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                SetProperty(ref _newPassword, value);
                ValidateNewPassword();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                SetProperty(ref _confirmPassword, value);
                ValidateConfirmPassword();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsCurrentUser => UserSession.Instance.CurrentUser?.Id == _usuario.Id;

        #endregion

        #region Commands

        public ICommand CambiarPasswordCommand { get; private set; } = null!;
        public ICommand CancelarCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            CambiarPasswordCommand = new RelayCommand(async () => await CambiarPassword(), CanCambiarPassword);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        #endregion

        #region Command Methods

        private bool CanCambiarPassword()
        {
            if (IsCurrentUser)
            {
                return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                       !string.IsNullOrWhiteSpace(NewPassword) &&
                       !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                       NewPassword == ConfirmPassword &&
                       string.IsNullOrEmpty(ErrorMessage);
            }
            else
            {
                // Para otros usuarios, no necesita contraseña actual
                return !string.IsNullOrWhiteSpace(NewPassword) &&
                       !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                       NewPassword == ConfirmPassword &&
                       string.IsNullOrEmpty(ErrorMessage);
            }
        }

        private async Task CambiarPassword()
        {
            try
            {
                // Verificar contraseña actual si es el mismo usuario
                if (IsCurrentUser)
                {
                    var currentPasswordHash = HashPassword(CurrentPassword);
                    if (currentPasswordHash != _usuario.PasswordHash)
                    {
                        ErrorMessage = "La contraseña actual es incorrecta.";
                        return;
                    }
                }

                // Cambiar contraseña
                _usuario.PasswordHash = HashPassword(NewPassword);
                await _dbContext.SaveChangesAsync();

                // Log de la acción
                await LogAction($"Contraseña cambiada para usuario: {_usuario.NombreUsuario}");

                MessageBox.Show("Contraseña cambiada correctamente.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                CloseWindow(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar contraseña: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar()
        {
            CloseWindow(false);
        }

        #endregion

        #region Methods

        private void ValidateCurrentPassword()
        {
            if (IsCurrentUser && string.IsNullOrWhiteSpace(CurrentPassword))
            {
                ErrorMessage = "Debe introducir la contraseña actual.";
            }
            else
            {
                ErrorMessage = "";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void ValidateNewPassword()
        {
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "La nueva contraseña es obligatoria.";
            }
            else if (NewPassword.Length < 6)
            {
                ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.";
            }
            else if (IsCurrentUser && NewPassword == CurrentPassword)
            {
                ErrorMessage = "La nueva contraseña debe ser diferente a la actual.";
            }
            else
            {
                ErrorMessage = "";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void ValidateConfirmPassword()
        {
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Debe confirmar la nueva contraseña.";
            }
            else if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Las contraseñas no coinciden.";
            }
            else
            {
                ErrorMessage = "";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
            return Convert.ToBase64String(hashedBytes);
        }

        private async Task LogAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "Seguridad",
                    FechaHora = DateTime.Now
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch { }
        }

        private void CloseWindow(bool dialogResult)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is CambiarPasswordWindow passWindow &&
                    ReferenceEquals(passWindow.DataContext, this))
                {
                    passWindow.DialogResult = dialogResult;
                    passWindow.Close();
                    break;
                }
            }
        }

        #endregion
    }
}