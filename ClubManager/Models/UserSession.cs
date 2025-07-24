// UserSession.cs - Gestión de sesión sin persistencia
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClubManager.Models;

namespace ClubManager.Services
{
    public class UserSession : INotifyPropertyChanged
    {
        private static UserSession? _instance;
        private static readonly object _lock = new object();

        private Usuario? _currentUser;
        private bool _isLoggedIn = false;
        private DateTime? _loginTime;
        private TimeSpan _sessionDuration;

        public event EventHandler<Usuario>? UserLoggedIn;
        public event EventHandler? UserLoggedOut;
        public event PropertyChangedEventHandler? PropertyChanged;

        private UserSession()
        {
            // Constructor privado para patrón Singleton
            StartSessionTimer();
        }

        public static UserSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new UserSession();
                    }
                }
                return _instance;
            }
        }

        #region Properties

        public Usuario? CurrentUser
        {
            get => _currentUser;
            private set
            {
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(UserRole));
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanAdmin));
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(SessionInfo));
            }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn && _currentUser != null;
            private set
            {
                _isLoggedIn = value;
                OnPropertyChanged();
            }
        }

        public string Username => CurrentUser?.NombreUsuario ?? "No autenticado";
        public string UserRole => CurrentUser?.Rol ?? "N/A";

        public DateTime? LoginTime
        {
            get => _loginTime;
            private set
            {
                _loginTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SessionInfo));
            }
        }

        public TimeSpan SessionDuration
        {
            get => _sessionDuration;
            private set
            {
                _sessionDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SessionInfo));
            }
        }

        public string SessionInfo
        {
            get
            {
                if (!IsLoggedIn || LoginTime == null)
                    return "No hay sesión activa";

                var duration = DateTime.Now - LoginTime.Value;
                return $"🕐 Sesión activa: {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }
        }

        // Permisos basados en roles
        public bool CanEdit => IsLoggedIn && CurrentUser?.Rol != "Solo Lectura";
        public bool CanAdmin => IsLoggedIn && CurrentUser?.Rol == "Administrador";
        public bool IsReadOnly => !CanEdit;

        #endregion

        #region Methods

        public void Login(Usuario user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Limpiar sesión anterior si existe
            if (IsLoggedIn)
            {
                Logout();
            }

            CurrentUser = user;
            IsLoggedIn = true;
            LoginTime = DateTime.Now;

            // Notificar login
            UserLoggedIn?.Invoke(this, user);

            System.Diagnostics.Debug.WriteLine($"✅ Usuario logueado: {user.NombreUsuario} ({user.Rol})");
        }

        public void Logout()
        {
            if (IsLoggedIn)
            {
                var loggedOutUser = CurrentUser;
                System.Diagnostics.Debug.WriteLine($"🚪 Cerrando sesión: {loggedOutUser?.NombreUsuario}");

                // Limpiar completamente la sesión
                CurrentUser = null;
                IsLoggedIn = false;
                LoginTime = null;
                SessionDuration = TimeSpan.Zero;

                // Notificar logout
                UserLoggedOut?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool HasPermission(string requiredRole)
        {
            if (!IsLoggedIn || CurrentUser == null)
                return false;

            return CurrentUser.Rol switch
            {
                "Administrador" => true, // Administrador tiene todos los permisos
                "Gestor" => requiredRole != "Administrador",
                "Usuario" => requiredRole == "Usuario" || requiredRole == "Solo Lectura",
                "Solo Lectura" => requiredRole == "Solo Lectura",
                _ => false
            };
        }

        public bool CanAccessFeature(string feature)
        {
            if (!IsLoggedIn || CurrentUser == null)
                return false;

            return feature.ToLower() switch
            {
                "usuarios" => CurrentUser.Rol == "Administrador",
                "configuracion" => CurrentUser.Rol is "Administrador" or "Gestor",
                "exportar" => CurrentUser.Rol != "Solo Lectura",
                "editar" => CurrentUser.Rol != "Solo Lectura",
                "eliminar" => CurrentUser.Rol is "Administrador" or "Gestor",
                "estadisticas" => true, // Todos pueden ver estadísticas
                "dashboard" => true, // Todos pueden ver el dashboard
                _ => false
            };
        }

        public void RefreshSessionTimer()
        {
            if (IsLoggedIn && LoginTime.HasValue)
            {
                SessionDuration = DateTime.Now - LoginTime.Value;
                OnPropertyChanged(nameof(SessionInfo));
            }
        }

        private void StartSessionTimer()
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += (s, e) => RefreshSessionTimer();
            timer.Start();
        }

        public void ValidateSession()
        {
            // Verificar que la sesión siga siendo válida
            if (IsLoggedIn && CurrentUser != null)
            {
                // Aquí podrías agregar validaciones adicionales como:
                // - Tiempo máximo de sesión
                // - Verificar si el usuario sigue activo en BD
                // - Etc.

                if (!CurrentUser.Activo)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Usuario desactivado durante la sesión");
                    Logout();
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}