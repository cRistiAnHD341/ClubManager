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
using ClubManager.Services;
using ClubManager.Views;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class UsuariosViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<UsuarioExtendido> _usuarios;
        private ObservableCollection<UsuarioExtendido> _usuariosFiltered;
        private UsuarioExtendido? _selectedUsuario;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;

        public UsuariosViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            _usuarios = new ObservableCollection<UsuarioExtendido>();
            _usuariosFiltered = new ObservableCollection<UsuarioExtendido>();

            InitializeCommands();
            LoadData();
            UpdateCanEdit();
        }

        #region Properties

        public ObservableCollection<UsuarioExtendido> UsuariosFiltered
        {
            get => _usuariosFiltered;
            set => SetProperty(ref _usuariosFiltered, value);
        }

        public UsuarioExtendido? SelectedUsuario
        {
            get => _selectedUsuario;
            set => SetProperty(ref _selectedUsuario, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }

        public string SubTitle
        {
            get => _subTitle;
            set => SetProperty(ref _subTitle, value);
        }

        public bool CanEdit
        {
            get => _canEdit;
            set => SetProperty(ref _canEdit, value);
        }

        #endregion

        #region Commands

        public ICommand NewUserCommand { get; private set; } = null!;
        public ICommand EditUserCommand { get; private set; } = null!;
        public ICommand DeleteUserCommand { get; private set; } = null!;
        public ICommand ToggleActiveCommand { get; private set; } = null!;
        public ICommand ChangePasswordCommand { get; private set; } = null!;
        public ICommand ManagePermissionsCommand { get; private set; } = null!;
        public ICommand ViewHistoryCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NewUserCommand = new RelayCommand(NewUser, () => CanEdit);
            EditUserCommand = new RelayCommand<UsuarioExtendido>(EditUser, u => CanEdit && u != null);
            DeleteUserCommand = new RelayCommand<UsuarioExtendido>(DeleteUser, u => CanEdit && u != null && u.NombreUsuario != "admin");
            ToggleActiveCommand = new RelayCommand<UsuarioExtendido>(ToggleActive, u => CanEdit && u != null && u.NombreUsuario != "admin");
            ChangePasswordCommand = new RelayCommand<UsuarioExtendido>(ChangePassword, u => CanEdit && u != null);
            ManagePermissionsCommand = new RelayCommand<UsuarioExtendido>(ManagePermissions, u => CanEdit && u != null);
            ViewHistoryCommand = new RelayCommand<UsuarioExtendido>(ViewHistory, u => u != null);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
        }

        #endregion

        #region Command Methods

        private void NewUser()
        {
            try
            {
                var editWindow = new EditarUsuarioWindow();
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear usuario: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditUser(UsuarioExtendido? usuario)
        {
            if (usuario?.Usuario == null) return;

            try
            {
                var editWindow = new EditarUsuarioWindow(usuario.Usuario);
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar usuario: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteUser(UsuarioExtendido? usuario)
        {
            if (usuario?.Usuario == null || usuario.NombreUsuario == "admin") return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar al usuario '{usuario.NombreUsuario}'?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Usuarios.Remove(usuario.Usuario);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminado usuario: {usuario.NombreUsuario}");
                    LoadData();

                    MessageBox.Show("Usuario eliminado correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar usuario: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ToggleActive(UsuarioExtendido? usuario)
        {
            if (usuario?.Usuario == null || usuario.NombreUsuario == "admin") return;

            try
            {
                usuario.Usuario.Activo = !usuario.Usuario.Activo;
                await _dbContext.SaveChangesAsync();

                await LogAction($"Usuario {usuario.NombreUsuario} {(usuario.Usuario.Activo ? "activado" : "desactivado")}");

                // Refrescar vista
                var index = UsuariosFiltered.IndexOf(usuario);
                if (index >= 0)
                {
                    var updatedUser = new UsuarioExtendido(usuario.Usuario);
                    UsuariosFiltered[index] = updatedUser;
                }

                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar estado del usuario: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePassword(UsuarioExtendido? usuario)
        {
            if (usuario?.Usuario == null) return;

            try
            {
                var passwordWindow = new CambiarPasswordWindow(usuario.Usuario);
                if (passwordWindow.ShowDialog() == true)
                {
                    MessageBox.Show($"Contraseña cambiada correctamente para {usuario.NombreUsuario}.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar contraseña: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManagePermissions(UsuarioExtendido? usuario)
        {
            if (usuario?.Usuario == null) return;

            try
            {
                var permissionsWindow = new PermissionsWindow(usuario.Usuario);
                if (permissionsWindow.ShowDialog() == true)
                {
                    LoadData(); // Recargar para mostrar cambios
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al gestionar permisos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewHistory(UsuarioExtendido? usuario)
        {
            if (usuario == null) return;

            MessageBox.Show($"Ver historial de {usuario.NombreUsuario} - Funcionalidad próximamente",
                          "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearFilters()
        {
            SearchText = "";
        }

        #endregion

        #region Methods

        private async void LoadData()
        {
            try
            {
                var usuarios = await _dbContext.Usuarios
                    .Include(u => u.Permissions)
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

        private void ApplyFilters()
        {
            try
            {
                var filtered = _usuarios.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(u =>
                        u.NombreUsuario.ToLower().Contains(searchLower) ||
                        (u.NombreCompleto?.ToLower().Contains(searchLower) ?? false) ||
                        (u.Email?.ToLower().Contains(searchLower) ?? false) ||
                        u.Rol.ToLower().Contains(searchLower));
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
                System.Diagnostics.Debug.WriteLine($"Error aplicando filtros: {ex.Message}");
            }
        }

        private void UpdateSubTitle()
        {
            var total = UsuariosFiltered.Count;
            var activos = UsuariosFiltered.Count(u => u.Activo);
            SubTitle = $"{total} usuarios ({activos} activos)";
        }

        private void UpdateCanEdit()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            var permissions = UserSession.Instance.CurrentPermissions;
            CanEdit = licenseInfo.IsValid && !licenseInfo.IsExpired &&
                     permissions?.CanAccessUsuarios == true;
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

        #endregion
    }

    // Clase extendida para mostrar información adicional
    public class UsuarioExtendido : BaseViewModel
    {
        public Usuario Usuario { get; }

        public UsuarioExtendido(Usuario usuario)
        {
            Usuario = usuario;
            CalculateExtendedProperties();
        }

        private void CalculateExtendedProperties()
        {
            TotalAcciones = Usuario.HistorialAcciones?.Count ?? 0;
            UltimoAcceso = Usuario.UltimoAcceso;
            EstadoTexto = Usuario.Activo ? "Activo" : "Inactivo";
            ColorEstado = Usuario.Activo ? "#4CAF50" : "#F44336";

            // Calcular permisos resumidos
            var permisos = Usuario.Permissions;
            PermisosSummary = permisos != null ? $"Permisos: {GetPermissionSummary(permisos)}" : "Sin permisos";
        }

        private string GetPermissionSummary(UserPermissions permissions)
        {
            var permisos = new List<string>();

            if (permissions.CanAccessAbonados) permisos.Add("Abonados");
            if (permissions.CanAccessUsuarios) permisos.Add("Usuarios");
            if (permissions.CanAccessConfiguracion) permisos.Add("Config");
            if (permissions.CanChangeLicense) permisos.Add("Licencia");

            return permisos.Any() ? string.Join(", ", permisos) : "Básicos";
        }

        // Propiedades del usuario
        public int Id => Usuario.Id;
        public string NombreUsuario => Usuario.NombreUsuario;
        public string? NombreCompleto => Usuario.NombreCompleto;
        public string? Email => Usuario.Email;
        public string Rol => Usuario.Rol;
        public bool Activo => Usuario.Activo;
        public DateTime FechaCreacion => Usuario.FechaCreacion;

        // Propiedades calculadas
        public int TotalAcciones { get; private set; }
        public DateTime? UltimoAcceso { get; private set; }
        public string EstadoTexto { get; private set; } = "";
        public string ColorEstado { get; private set; } = "";
        public string PermisosSummary { get; private set; } = "";
        public string UltimoAccesoTexto => UltimoAcceso?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";
        public string ToggleButtonText => Activo ? "⏸️" : "▶️";
        public string ToggleButtonTooltip => Activo ? "Desactivar usuario" : "Activar usuario";
        public bool CanDelete => NombreUsuario != "admin";
    }
}