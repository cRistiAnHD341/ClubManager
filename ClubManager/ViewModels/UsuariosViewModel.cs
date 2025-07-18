using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager.ViewModels
{
    public class UsuariosViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<UsuarioExtendido> _usuarios;
        private ObservableCollection<UsuarioExtendido> _usuariosFiltered;
        private UsuarioExtendido? _selectedUser;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;
        private bool _hasSelectedUser = false;

        // Filtros
        private ObservableCollection<UserFiltroItem> _rolesFilter;
        private ObservableCollection<UserFiltroItem> _estadoFilter;
        private string? _selectedRolFilter;
        private bool? _selectedEstadoFilter;

        public UsuariosViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            _usuarios = new ObservableCollection<UsuarioExtendido>();
            _usuariosFiltered = new ObservableCollection<UsuarioExtendido>();
            _rolesFilter = new ObservableCollection<UserFiltroItem>();
            _estadoFilter = new ObservableCollection<UserFiltroItem>();

            InitializeCommands();
            InitializeFilters();
            LoadData();
            UpdateCanEdit();
        }

        #region Properties

        public ObservableCollection<UsuarioExtendido> UsuariosFiltered
        {
            get => _usuariosFiltered;
            set { _usuariosFiltered = value; OnPropertyChanged(); }
        }

        public UsuarioExtendido? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                HasSelectedUser = value != null;
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string SubTitle
        {
            get => _subTitle;
            set { _subTitle = value; OnPropertyChanged(); }
        }

        public bool CanEdit
        {
            get => _canEdit;
            set { _canEdit = value; OnPropertyChanged(); }
        }

        public bool HasSelectedUser
        {
            get => _hasSelectedUser;
            set { _hasSelectedUser = value; OnPropertyChanged(); }
        }

        // Filtros
        public ObservableCollection<UserFiltroItem> RolesFilter
        {
            get => _rolesFilter;
            set { _rolesFilter = value; OnPropertyChanged(); }
        }

        public ObservableCollection<UserFiltroItem> EstadoFilter
        {
            get => _estadoFilter;
            set { _estadoFilter = value; OnPropertyChanged(); }
        }

        public string? SelectedRolFilter
        {
            get => _selectedRolFilter;
            set
            {
                _selectedRolFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public bool? SelectedEstadoFilter
        {
            get => _selectedEstadoFilter;
            set
            {
                _selectedEstadoFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        #endregion

        #region Commands

        public ICommand NewUserCommand { get; private set; }
        public ICommand EditUserCommand { get; private set; }
        public ICommand DeleteUserCommand { get; private set; }
        public ICommand ToggleUserCommand { get; private set; }
        public ICommand ChangePasswordCommand { get; private set; }
        public ICommand ManagePermissionsCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ShowAuditCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            NewUserCommand = new RelayCommand(NewUser);
            EditUserCommand = new RelayCommand<UsuarioExtendido>(EditUser);
            DeleteUserCommand = new RelayCommand<UsuarioExtendido>(DeleteUser);
            ToggleUserCommand = new RelayCommand<UsuarioExtendido>(ToggleUser);
            ChangePasswordCommand = new RelayCommand(ChangePassword);
            ManagePermissionsCommand = new RelayCommand(ManagePermissions);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ShowAuditCommand = new RelayCommand(ShowAudit);
        }

        private void InitializeFilters()
        {
            // Filtros de rol
            RolesFilter.Add(new UserFiltroItem { Display = "Todos los roles", Value = null });
            RolesFilter.Add(new UserFiltroItem { Display = "Administrador", Value = "Administrador" });
            RolesFilter.Add(new UserFiltroItem { Display = "Gestor", Value = "Gestor" });
            RolesFilter.Add(new UserFiltroItem { Display = "Usuario", Value = "Usuario" });
            RolesFilter.Add(new UserFiltroItem { Display = "Solo Lectura", Value = "Solo Lectura" });

            // Filtros de estado
            EstadoFilter.Add(new UserFiltroItem { Display = "Todos", Value = null });
            EstadoFilter.Add(new UserFiltroItem { Display = "Activos", Value = true });
            EstadoFilter.Add(new UserFiltroItem { Display = "Inactivos", Value = false });
        }

        private async void LoadData()
        {
            try
            {
                var usuarios = await _dbContext.Usuarios
                    .Include(u => u.HistorialAcciones)
                    .OrderBy(u => u.NombreUsuario)
                    .ToListAsync();

                _usuarios.Clear();
                foreach (var usuario in usuarios)
                {
                    var usuarioExtendido = new UsuarioExtendido(usuario);
                    _usuarios.Add(usuarioExtendido);
                }

                ApplyFilters();
                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar usuarios: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCanEdit()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            CanEdit = licenseInfo.IsValid && !licenseInfo.IsExpired;
        }

        #endregion

        #region Filtering

        private void ApplyFilters()
        {
            try
            {
                var filtered = _usuarios.AsEnumerable();

                // Filtro por texto de búsqueda
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(u =>
                        u.NombreUsuario.ToLower().Contains(searchLower));
                }

                // Filtro por rol
                if (!string.IsNullOrEmpty(SelectedRolFilter))
                {
                    filtered = filtered.Where(u => u.Rol == SelectedRolFilter);
                }

                // Filtro por estado
                if (SelectedEstadoFilter.HasValue)
                {
                    filtered = filtered.Where(u => u.Activo == SelectedEstadoFilter.Value);
                }

                UsuariosFiltered.Clear();
                foreach (var usuario in filtered)
                {
                    UsuariosFiltered.Add(usuario);
                }

                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSubTitle()
        {
            var total = _usuarios.Count;
            var filtered = UsuariosFiltered.Count;
            var activos = UsuariosFiltered.Count(u => u.Activo);

            if (total == filtered)
            {
                SubTitle = $"Total: {total} usuarios • Activos: {activos}";
            }
            else
            {
                SubTitle = $"Mostrando: {filtered} de {total} usuarios • Activos: {activos}";
            }
        }

        #endregion

        #region Command Methods

        private void NewUser()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para crear usuarios. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new UserEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditUser(UsuarioExtendido? usuario)
        {
            if (usuario == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para editar usuarios. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var editWindow = new UserEditWindow(usuario.Usuario);
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private async void DeleteUser(UsuarioExtendido? usuario)
        {
            if (usuario == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para eliminar usuarios. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            if (usuario.NombreUsuario == "admin")
            {
                MessageBox.Show("No se puede eliminar el usuario administrador principal.", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Estás seguro de que quieres eliminar al usuario '{usuario.NombreUsuario}'?\n\n" +
                "Esta acción no se puede deshacer y se eliminarán todos sus registros de historial.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Usuarios.Remove(usuario.Usuario);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminó usuario: {usuario.NombreUsuario}");

                    LoadData();

                    MessageBox.Show("Usuario eliminado correctamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar el usuario: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ToggleUser(UsuarioExtendido? usuario)
        {
            if (usuario == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para modificar usuarios. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            if (usuario.NombreUsuario == "admin")
            {
                MessageBox.Show("No se puede desactivar el usuario administrador principal.", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                usuario.Usuario.Activo = !usuario.Usuario.Activo;
                await _dbContext.SaveChangesAsync();

                var accion = usuario.Usuario.Activo ? "activó" : "desactivó";
                await LogAction($"{accion} usuario: {usuario.NombreUsuario}");

                // Actualizar propiedades del usuario extendido
                usuario.Activo = usuario.Usuario.Activo;

                ApplyFilters();

                var mensaje = usuario.Usuario.Activo ? "activado" : "desactivado";
                MessageBox.Show($"Usuario {mensaje} correctamente.", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar el estado del usuario: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePassword()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Selecciona un usuario para cambiar la contraseña.", "Información",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var passwordWindow = new ChangePasswordWindow(SelectedUser.Usuario);
            if (passwordWindow.ShowDialog() == true)
            {
                MessageBox.Show("Contraseña cambiada exitosamente.", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ManagePermissions()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Selecciona un usuario para gestionar permisos.", "Información",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var permissionsWindow = new PermissionsWindow(SelectedUser.Usuario);
            if (permissionsWindow.ShowDialog() == true)
            {
                LoadData(); // Recargar para actualizar permisos mostrados
            }
        }

        private void ClearFilters()
        {
            SearchText = "";
            SelectedRolFilter = null;
            SelectedEstadoFilter = null;
        }

        private void ShowAudit()
        {
            var auditWindow = new AuditWindow();
            auditWindow.ShowDialog();
        }

        #endregion

        #region Helper Methods

        private async Task LogAction(string accion)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = 1, // Por ahora usuario por defecto
                    Accion = accion,
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
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #endregion
    }

    #region Helper Classes

    public class UserFiltroItem
    {
        public string Display { get; set; } = "";
        public object? Value { get; set; }
    }

    public class UsuarioExtendido : INotifyPropertyChanged
    {
        public Usuario Usuario { get; }

        public UsuarioExtendido(Usuario usuario)
        {
            Usuario = usuario;
            CalculateProperties();
        }

        private void CalculateProperties()
        {
            TotalAcciones = Usuario.HistorialAcciones?.Count ?? 0;
            UltimoAcceso = Usuario.HistorialAcciones?.OrderByDescending(h => h.FechaHora).FirstOrDefault()?.FechaHora;

            // Simular permisos basados en rol
            switch (Rol)
            {
                case "Administrador":
                    TienePermisoGestion = Visibility.Visible;
                    TienePermisoAbonados = Visibility.Visible;
                    TienePermisoReportes = Visibility.Visible;
                    TienePermisoConfiguracion = Visibility.Visible;
                    TienePermisoUsuarios = Visibility.Visible;
                    break;
                case "Gestor":
                    TienePermisoGestion = Visibility.Visible;
                    TienePermisoAbonados = Visibility.Visible;
                    TienePermisoReportes = Visibility.Visible;
                    TienePermisoConfiguracion = Visibility.Collapsed;
                    TienePermisoUsuarios = Visibility.Collapsed;
                    break;
                case "Usuario":
                    TienePermisoGestion = Visibility.Collapsed;
                    TienePermisoAbonados = Visibility.Visible;
                    TienePermisoReportes = Visibility.Visible;
                    TienePermisoConfiguracion = Visibility.Collapsed;
                    TienePermisoUsuarios = Visibility.Collapsed;
                    break;
                default:
                    TienePermisoGestion = Visibility.Collapsed;
                    TienePermisoAbonados = Visibility.Collapsed;
                    TienePermisoReportes = Visibility.Visible;
                    TienePermisoConfiguracion = Visibility.Collapsed;
                    TienePermisoUsuarios = Visibility.Collapsed;
                    break;
            }
        }

        // Propiedades delegadas
        public string NombreUsuario => Usuario.NombreUsuario;
        public string Rol => Usuario.Rol;
        public bool Activo
        {
            get => Usuario.Activo;
            set
            {
                if (Usuario.Activo != value)
                {
                    Usuario.Activo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EstadoTexto));
                    OnPropertyChanged(nameof(ToggleButtonText));
                    OnPropertyChanged(nameof(ToggleButtonTooltip));
                }
            }
        }
        public DateTime FechaCreacion => Usuario.FechaCreacion;

        // Propiedades calculadas
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
        public int TotalAcciones { get; private set; }
        public DateTime? UltimoAcceso { get; private set; }

        // Propiedades de permisos
        public Visibility TienePermisoGestion { get; private set; }
        public Visibility TienePermisoAbonados { get; private set; }
        public Visibility TienePermisoReportes { get; private set; }
        public Visibility TienePermisoConfiguracion { get; private set; }
        public Visibility TienePermisoUsuarios { get; private set; }

        // Propiedades para botones
        public string ToggleButtonText => Activo ? "⏸️" : "▶️";
        public string ToggleButtonTooltip => Activo ? "Desactivar usuario" : "Activar usuario";
        public object ToggleButtonStyle => Activo ? "WarningButton" : "SuccessButton";
        public Visibility CanDelete => NombreUsuario != "admin" ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Placeholder windows
    public class UserEditWindow : Window
    {
        public UserEditWindow(Usuario? usuario = null)
        {
            Title = usuario == null ? "Nuevo Usuario" : "Editar Usuario";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "Ventana de Edición de Usuario - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    public class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow(Usuario usuario)
        {
            Title = "Cambiar Contraseña";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = new System.Windows.Controls.TextBlock
            {
                Text = $"Cambiar contraseña de {usuario.NombreUsuario} - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    public class PermissionsWindow : Window
    {
        public PermissionsWindow(Usuario usuario)
        {
            Title = "Gestionar Permisos";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = new System.Windows.Controls.TextBlock
            {
                Text = $"Permisos de {usuario.NombreUsuario} - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    public class AuditWindow : Window
    {
        public AuditWindow()
        {
            Title = "Auditoría de Usuarios";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "Auditoría de Usuarios - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    #endregion
}