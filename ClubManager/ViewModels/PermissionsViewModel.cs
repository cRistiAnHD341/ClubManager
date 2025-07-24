using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class PermissionsViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly Usuario _usuario;
        private UserPermissions _permissions;

        public PermissionsViewModel(Usuario usuario)
        {
            _dbContext = new ClubDbContext();
            _usuario = usuario;
            _permissions = usuario.Permissions ?? new UserPermissions { UsuarioId = usuario.Id };

            InitializeCommands();
            LoadPermissions();
        }

        #region Properties

        public string UsuarioNombre => _usuario.NombreUsuario;
        public string UsuarioRol => _usuario.Rol;

        // Permisos de acceso
        public bool CanAccessDashboard
        {
            get => _permissions.CanAccessDashboard;
            set
            {
                _permissions.CanAccessDashboard = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessAbonados
        {
            get => _permissions.CanAccessAbonados;
            set
            {
                _permissions.CanAccessAbonados = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessTiposAbono
        {
            get => _permissions.CanAccessTiposAbono;
            set
            {
                _permissions.CanAccessTiposAbono = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessGestores
        {
            get => _permissions.CanAccessGestores;
            set
            {
                _permissions.CanAccessGestores = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessPeñas
        {
            get => _permissions.CanAccessPeñas;
            set
            {
                _permissions.CanAccessPeñas = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessUsuarios
        {
            get => _permissions.CanAccessUsuarios;
            set
            {
                _permissions.CanAccessUsuarios = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessHistorial
        {
            get => _permissions.CanAccessHistorial;
            set
            {
                _permissions.CanAccessHistorial = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessConfiguracion
        {
            get => _permissions.CanAccessConfiguracion;
            set
            {
                _permissions.CanAccessConfiguracion = value;
                OnPropertyChanged();
            }
        }

        // Permisos de acción
        public bool CanCreateAbonados
        {
            get => _permissions.CanCreateAbonados;
            set
            {
                _permissions.CanCreateAbonados = value;
                OnPropertyChanged();
            }
        }

        public bool CanEditAbonados
        {
            get => _permissions.CanEditAbonados;
            set
            {
                _permissions.CanEditAbonados = value;
                OnPropertyChanged();
            }
        }

        public bool CanDeleteAbonados
        {
            get => _permissions.CanDeleteAbonados;
            set
            {
                _permissions.CanDeleteAbonados = value;
                OnPropertyChanged();
            }
        }

        public bool CanExportData
        {
            get => _permissions.CanExportData;
            set
            {
                _permissions.CanExportData = value;
                OnPropertyChanged();
            }
        }

        public bool CanImportData
        {
            get => _permissions.CanImportData;
            set
            {
                _permissions.CanImportData = value;
                OnPropertyChanged();
            }
        }

        public bool CanManageSeasons
        {
            get => _permissions.CanManageSeasons;
            set
            {
                _permissions.CanManageSeasons = value;
                OnPropertyChanged();
            }
        }

        public bool CanChangeLicense
        {
            get => _permissions.CanChangeLicense;
            set
            {
                _permissions.CanChangeLicense = value;
                OnPropertyChanged();
            }
        }

        public bool CanCreateBackups
        {
            get => _permissions.CanCreateBackups;
            set
            {
                _permissions.CanCreateBackups = value;
                OnPropertyChanged();
            }
        }

        public bool CanAccessTemplates
        {
            get => _permissions.CanAccessTemplates;
            set
            {
                _permissions.CanAccessTemplates = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand GuardarCommand { get; private set; } = null!;
        public ICommand CancelarCommand { get; private set; } = null!;
        public ICommand PerfilAdministradorCommand { get; private set; } = null!;
        public ICommand PerfilGestorCommand { get; private set; } = null!;
        public ICommand PerfilUsuarioCommand { get; private set; } = null!;
        public ICommand PerfilLecturaCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            GuardarCommand = new RelayCommand(async () => await Guardar());
            CancelarCommand = new RelayCommand(Cancelar);
            PerfilAdministradorCommand = new RelayCommand(AplicarPerfilAdministrador);
            PerfilGestorCommand = new RelayCommand(AplicarPerfilGestor);
            PerfilUsuarioCommand = new RelayCommand(AplicarPerfilUsuario);
            PerfilLecturaCommand = new RelayCommand(AplicarPerfilLectura);
        }

        #endregion

        #region Command Methods

        private async Task Guardar()
        {
            try
            {
                // Verificar si los permisos ya existen
                var existingPermissions = await _dbContext.UserPermissions
                    .FirstOrDefaultAsync(p => p.UsuarioId == _usuario.Id);

                if (existingPermissions == null)
                {
                    _dbContext.UserPermissions.Add(_permissions);
                }
                else
                {
                    // Actualizar permisos existentes
                    existingPermissions.CanAccessDashboard = _permissions.CanAccessDashboard;
                    existingPermissions.CanAccessAbonados = _permissions.CanAccessAbonados;
                    existingPermissions.CanAccessTiposAbono = _permissions.CanAccessTiposAbono;
                    existingPermissions.CanAccessGestores = _permissions.CanAccessGestores;
                    existingPermissions.CanAccessPeñas = _permissions.CanAccessPeñas;
                    existingPermissions.CanAccessUsuarios = _permissions.CanAccessUsuarios;
                    existingPermissions.CanAccessHistorial = _permissions.CanAccessHistorial;
                    existingPermissions.CanAccessConfiguracion = _permissions.CanAccessConfiguracion;
                    existingPermissions.CanCreateAbonados = _permissions.CanCreateAbonados;
                    existingPermissions.CanEditAbonados = _permissions.CanEditAbonados;
                    existingPermissions.CanDeleteAbonados = _permissions.CanDeleteAbonados;
                    existingPermissions.CanExportData = _permissions.CanExportData;
                    existingPermissions.CanImportData = _permissions.CanImportData;
                    existingPermissions.CanManageSeasons = _permissions.CanManageSeasons;
                    existingPermissions.CanChangeLicense = _permissions.CanChangeLicense;
                    existingPermissions.CanCreateBackups = _permissions.CanCreateBackups;
                    existingPermissions.CanAccessTemplates = _permissions.CanAccessTemplates;
                }

                await _dbContext.SaveChangesAsync();

                // Log de la acción
                await LogAction($"Actualizados permisos para usuario: {_usuario.NombreUsuario}");

                MessageBox.Show("Permisos actualizados correctamente.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                CloseWindow(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar permisos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar()
        {
            CloseWindow(false);
        }

        private void AplicarPerfilAdministrador()
        {
            SetAllPermissions(true);
            MessageBox.Show("Perfil de Administrador aplicado.", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AplicarPerfilGestor()
        {
            SetAllPermissions(false);
            CanAccessDashboard = true;
            CanAccessAbonados = true;
            CanAccessGestores = true;
            CanAccessPeñas = true;
            CanAccessHistorial = true;
            CanCreateAbonados = true;
            CanEditAbonados = true;
            CanExportData = true;
            CanAccessTemplates = true;

            MessageBox.Show("Perfil de Gestor aplicado.", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AplicarPerfilUsuario()
        {
            SetAllPermissions(false);
            CanAccessDashboard = true;
            CanAccessAbonados = true;
            CanCreateAbonados = true;
            CanEditAbonados = true;

            MessageBox.Show("Perfil de Usuario aplicado.", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AplicarPerfilLectura()
        {
            SetAllPermissions(false);
            CanAccessDashboard = true;

            MessageBox.Show("Perfil de Solo Lectura aplicado.", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Methods

        private void LoadPermissions()
        {
            // Los permisos ya están cargados en el constructor
            // Notificar todas las propiedades para actualizar la UI
            OnPropertyChanged(nameof(CanAccessDashboard));
            OnPropertyChanged(nameof(CanAccessAbonados));
            OnPropertyChanged(nameof(CanAccessTiposAbono));
            OnPropertyChanged(nameof(CanAccessGestores));
            OnPropertyChanged(nameof(CanAccessPeñas));
            OnPropertyChanged(nameof(CanAccessUsuarios));
            OnPropertyChanged(nameof(CanAccessHistorial));
            OnPropertyChanged(nameof(CanAccessConfiguracion));
            OnPropertyChanged(nameof(CanCreateAbonados));
            OnPropertyChanged(nameof(CanEditAbonados));
            OnPropertyChanged(nameof(CanDeleteAbonados));
            OnPropertyChanged(nameof(CanExportData));
            OnPropertyChanged(nameof(CanImportData));
            OnPropertyChanged(nameof(CanManageSeasons));
            OnPropertyChanged(nameof(CanChangeLicense));
            OnPropertyChanged(nameof(CanCreateBackups));
            OnPropertyChanged(nameof(CanAccessTemplates));
        }

        private void SetAllPermissions(bool value)
        {
            CanAccessDashboard = value;
            CanAccessAbonados = value;
            CanAccessTiposAbono = value;
            CanAccessGestores = value;
            CanAccessPeñas = value;
            CanAccessUsuarios = value;
            CanAccessHistorial = value;
            CanAccessConfiguracion = value;
            CanCreateAbonados = value;
            CanEditAbonados = value;
            CanDeleteAbonados = value;
            CanExportData = value;
            CanImportData = value;
            CanManageSeasons = value;
            CanChangeLicense = value;
            CanCreateBackups = value;
            CanAccessTemplates = value;
        }

        private async Task LogAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "Permisos",
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
                if (window is PermissionsWindow permWindow &&
                    ReferenceEquals(permWindow.DataContext, this))
                {
                    permWindow.DialogResult = dialogResult;
                    permWindow.Close();
                    break;
                }
            }
        }

        #endregion
    }
}