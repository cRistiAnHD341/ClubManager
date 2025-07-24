using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Views;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class EditarUsuarioViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly Usuario? _originalUsuario;
        private bool _isNewUsuario;

        // Propiedades del usuario
        private string _nombreUsuario = "";
        private string _nombreCompleto = "";
        private string _email = "";
        private string _password = "";
        private string _confirmPassword = "";
        private string _rol = "Usuario";
        private bool _activo = true;

        // Colecciones
        private ObservableCollection<string> _roles;

        // Validación
        private string _errorMessage = "";

        public EditarUsuarioViewModel(Usuario? usuario = null)
        {
            _dbContext = new ClubDbContext();
            _originalUsuario = usuario;
            _isNewUsuario = usuario == null;

            _roles = new ObservableCollection<string>
            {
                "Administrador",
                "Gestor",
                "Usuario",
                "Solo Lectura"
            };

            InitializeCommands();

            if (_originalUsuario != null)
            {
                LoadUsuarioData();
            }
        }

        #region Properties

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set
            {
                SetProperty(ref _nombreUsuario, value);
                ValidateNombreUsuario();
            }
        }

        public string NombreCompleto
        {
            get => _nombreCompleto;
            set => SetProperty(ref _nombreCompleto, value);
        }

        public string Email
        {
            get => _email;
            set
            {
                SetProperty(ref _email, value);
                ValidateEmail();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ValidatePassword();
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

        public string Rol
        {
            get => _rol;
            set => SetProperty(ref _rol, value);
        }

        public bool Activo
        {
            get => _activo;
            set => SetProperty(ref _activo, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<string> Roles => _roles;

        public string WindowTitle => _isNewUsuario ? "Nuevo Usuario" : "Editar Usuario";
        public bool RequierePassword => _isNewUsuario;
        public string PasswordLabel => _isNewUsuario ? "Contraseña:*" : "Nueva Contraseña: (dejar vacío para no cambiar)";

        #endregion

        #region Commands

        public ICommand GuardarCommand { get; private set; } = null!;
        public ICommand CancelarCommand { get; private set; } = null!;
        public ICommand EditPermissionsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            GuardarCommand = new RelayCommand(async () => await Guardar(), CanGuardar);
            CancelarCommand = new RelayCommand(Cancelar);
            EditPermissionsCommand = new RelayCommand(EditPermissions, () => !_isNewUsuario);
        }

        #endregion

        #region Command Methods

        private bool CanGuardar()
        {
            return !string.IsNullOrWhiteSpace(NombreUsuario) &&
                   (!RequierePassword || !string.IsNullOrWhiteSpace(Password)) &&
                   (string.IsNullOrWhiteSpace(Password) || Password == ConfirmPassword) &&
                   string.IsNullOrEmpty(ErrorMessage);
        }

        private async Task Guardar()
        {
            try
            {
                // Validar que no exista otro usuario con el mismo nombre
                if (!await ValidateNombreUsuarioUnico())
                {
                    return;
                }

                Usuario usuario;
                bool needsPermissions = false;

                if (_isNewUsuario)
                {
                    usuario = new Usuario();
                    _dbContext.Usuarios.Add(usuario);
                    needsPermissions = true;
                }
                else
                {
                    usuario = _originalUsuario!;
                }

                // Actualizar propiedades
                usuario.NombreUsuario = NombreUsuario.Trim();
                usuario.NombreCompleto = string.IsNullOrWhiteSpace(NombreCompleto) ? null : NombreCompleto.Trim();
                usuario.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                usuario.Rol = Rol;
                usuario.Activo = Activo;

                // Actualizar contraseña si se proporcionó
                if (!string.IsNullOrWhiteSpace(Password))
                {
                    usuario.PasswordHash = HashPassword(Password);
                }

                if (_isNewUsuario)
                {
                    usuario.FechaCreacion = DateTime.Now;
                }

                await _dbContext.SaveChangesAsync();

                // Crear permisos por defecto para usuarios nuevos
                if (needsPermissions)
                {
                    await CreateDefaultPermissions(usuario);
                }

                // Log de la acción
                await LogAction(_isNewUsuario ?
                    $"Creado nuevo usuario: {usuario.NombreUsuario}" :
                    $"Editado usuario: {usuario.NombreUsuario}");

                MessageBox.Show($"Usuario {(_isNewUsuario ? "creado" : "actualizado")} correctamente.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                CloseWindow(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar usuario: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar()
        {
            CloseWindow(false);
        }

        private void EditPermissions()
        {
            if (_originalUsuario == null) return;

            try
            {
                var permissionsWindow = new PermissionsWindow(_originalUsuario)
                {
                    Owner = Application.Current.MainWindow
                };

                permissionsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir ventana de permisos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Methods

        private void LoadUsuarioData()
        {
            if (_originalUsuario == null) return;

            NombreUsuario = _originalUsuario.NombreUsuario;
            NombreCompleto = _originalUsuario.NombreCompleto ?? "";
            Email = _originalUsuario.Email ?? "";
            Rol = _originalUsuario.Rol;
            Activo = _originalUsuario.Activo;
            // No cargar contraseña por seguridad
        }

        private async Task<bool> ValidateNombreUsuarioUnico()
        {
            try
            {
                var existeNombre = await _dbContext.Usuarios
                    .AnyAsync(u => u.NombreUsuario.ToLower() == NombreUsuario.Trim().ToLower() &&
                                  (!_isNewUsuario ? u.Id != _originalUsuario!.Id : true));

                if (existeNombre)
                {
                    ErrorMessage = "Ya existe un usuario con este nombre.";
                    return false;
                }

                ErrorMessage = "";
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error validando nombre de usuario: {ex.Message}";
                return false;
            }
        }

        private async Task CreateDefaultPermissions(Usuario usuario)
        {
            try
            {
                var permissions = new UserPermissions
                {
                    UsuarioId = usuario.Id,
                    CanAccessDashboard = true,
                    CanAccessAbonados = Rol == "Administrador" || Rol == "Gestor",
                    CanAccessTiposAbono = Rol == "Administrador",
                    CanAccessGestores = Rol == "Administrador",
                    CanAccessPeñas = Rol == "Administrador",
                    CanAccessUsuarios = Rol == "Administrador",
                    CanAccessHistorial = Rol == "Administrador" || Rol == "Gestor",
                    CanAccessConfiguracion = Rol == "Administrador",
                    CanCreateAbonados = Rol == "Administrador" || Rol == "Gestor",
                    CanEditAbonados = Rol == "Administrador" || Rol == "Gestor",
                    CanDeleteAbonados = Rol == "Administrador",
                    CanExportData = Rol == "Administrador" || Rol == "Gestor",
                    CanImportData = Rol == "Administrador",
                    CanManageSeasons = Rol == "Administrador",
                    CanChangeLicense = Rol == "Administrador",
                    CanCreateBackups = Rol == "Administrador",
                    CanAccessTemplates = Rol == "Administrador" || Rol == "Gestor"
                };

                _dbContext.UserPermissions.Add(permissions);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando permisos: {ex.Message}");
            }
        }

        private void ValidateNombreUsuario()
        {
            if (string.IsNullOrWhiteSpace(NombreUsuario))
            {
                ErrorMessage = "El nombre de usuario es obligatorio.";
            }
            else if (NombreUsuario.Trim().Length < 3)
            {
                ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres.";
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(NombreUsuario, @"^[a-zA-Z0-9_]+$"))
            {
                ErrorMessage = "El nombre de usuario solo puede contener letras, números y guiones bajos.";
            }
            else
            {
                ErrorMessage = "";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void ValidateEmail()
        {
            if (!string.IsNullOrWhiteSpace(Email) && !IsValidEmail(Email))
            {
                ErrorMessage = "El formato del email no es válido.";
            }
            else
            {
                ErrorMessage = "";
            }
        }

        private void ValidatePassword()
        {
            if (RequierePassword && string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "La contraseña es obligatoria.";
            }
            else if (!string.IsNullOrWhiteSpace(Password) && Password.Length < 6)
            {
                ErrorMessage = "La contraseña debe tener al menos 6 caracteres.";
            }
            else
            {
                ErrorMessage = "";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void ValidateConfirmPassword()
        {
            if (!string.IsNullOrWhiteSpace(Password) && Password != ConfirmPassword)
            {
                ErrorMessage = "Las contraseñas no coinciden.";
            }
            else
            {
                ErrorMessage = "";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
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
                    TipoAccion = "Usuarios",
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
                if (window is EditarUsuarioWindow editWindow &&
                    ReferenceEquals(editWindow.DataContext, this))
                {
                    editWindow.DialogResult = dialogResult;
                    editWindow.Close();
                    break;
                }
            }
        }

        #endregion
    }
}