// ChangePasswordViewModel.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;

namespace ClubManager.ViewModels
{
    public class ChangePasswordViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private readonly Usuario _user;

        private string _newPassword = "";
        private string _confirmNewPassword = "";
        private string _newPasswordError = "";
        private string _confirmNewPasswordError = "";

        public event EventHandler<bool>? PasswordChangeCompleted;

        public ChangePasswordViewModel(Usuario user)
        {
            _dbContext = new ClubDbContext();
            _user = user;

            InitializeCommands();
        }

        #region Properties

        public string Username => _user.NombreUsuario;
        public string Role => _user.Rol;

        public string RoleColor => Role switch
        {
            "Administrador" => "#FFDC3545",
            "Gestor" => "#FF007ACC",
            "Usuario" => "#FF28A745",
            "Solo Lectura" => "#FF6C757D",
            _ => "#FF6C757D"
        };

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged();
                ValidateNewPassword();
                ValidateConfirmNewPassword();
            }
        }

        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set
            {
                _confirmNewPassword = value;
                OnPropertyChanged();
                ValidateConfirmNewPassword();
            }
        }

        public string NewPasswordError
        {
            get => _newPasswordError;
            set { _newPasswordError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNewPasswordError)); }
        }

        public string ConfirmNewPasswordError
        {
            get => _confirmNewPasswordError;
            set { _confirmNewPasswordError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasConfirmNewPasswordError)); }
        }

        public Visibility HasNewPasswordError => string.IsNullOrEmpty(NewPasswordError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasConfirmNewPasswordError => string.IsNullOrEmpty(ConfirmNewPasswordError) ? Visibility.Collapsed : Visibility.Visible;

        #endregion

        #region Commands

        public ICommand ChangePasswordCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            ChangePasswordCommand = new RelayCommand(ChangePassword, CanChangePassword);
        }

        #endregion

        #region Validation

        private void ValidateNewPassword()
        {
            NewPasswordError = "";

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                NewPasswordError = "La nueva contraseña es obligatoria";
                return;
            }

            if (NewPassword.Length < 6)
            {
                NewPasswordError = "La contraseña debe tener al menos 6 caracteres";
                return;
            }
        }

        private void ValidateConfirmNewPassword()
        {
            ConfirmNewPasswordError = "";

            if (string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                ConfirmNewPasswordError = "Debes confirmar la nueva contraseña";
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ConfirmNewPasswordError = "Las contraseñas no coinciden";
                return;
            }
        }

        private bool CanChangePassword()
        {
            return string.IsNullOrEmpty(NewPasswordError) &&
                   string.IsNullOrEmpty(ConfirmNewPasswordError) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmNewPassword);
        }

        #endregion

        #region Command Methods

        private async void ChangePassword()
        {
            if (!CanChangePassword())
            {
                MessageBox.Show("Por favor, corrige los errores antes de continuar.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Actualizar la contraseña del usuario
                _user.PasswordHash = HashPassword(NewPassword);

                await _dbContext.SaveChangesAsync();

                // Registrar en historial
                await LogAction($"Cambió contraseña del usuario: {_user.NombreUsuario}");

                MessageBox.Show("Contraseña cambiada correctamente.", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                PasswordChangeCompleted?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar la contraseña: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                PasswordChangeCompleted?.Invoke(this, false);
            }
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
                    TipoAccion = "Configuracion",
                    FechaHora = DateTime.Now
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch
            {
                // Si falla el log, no es crítico
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Revalidar comando cuando cambian propiedades relevantes
            if (propertyName == nameof(NewPassword) || propertyName == nameof(ConfirmNewPassword))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #endregion
    }
}