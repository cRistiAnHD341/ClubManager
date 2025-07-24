using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class EditarGestorViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly Gestor? _originalGestor;
        private bool _isNewGestor;

        // Propiedades del gestor
        private string _nombre = "";
        private string _descripcion = "";
        private string _telefono = "";
        private string _email = "";
        private string _nombrePreview = "Nombre del gestor";

        // Validación
        private string _errorMessage = "";

        public EditarGestorViewModel(Gestor? gestor = null)
        {
            _dbContext = new ClubDbContext();
            _originalGestor = gestor;
            _isNewGestor = gestor == null;

            InitializeCommands();

            if (_originalGestor != null)
            {
                LoadGestorData();
            }
            else
            {
                // Para nuevo gestor, inicializar vista previa
                UpdateNombrePreview();
            }
        }

        #region Properties

        public string Nombre
        {
            get => _nombre;
            set
            {
                if (SetProperty(ref _nombre, value))
                {
                    UpdateNombrePreview();
                    ValidateNombre();
                    OnPropertyChanged(nameof(CanGuardarStatus));
                    OnPropertyChanged(nameof(DebugInfo));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Descripcion
        {
            get => _descripcion;
            set => SetProperty(ref _descripcion, value);
        }

        public string Telefono
        {
            get => _telefono;
            set
            {
                SetProperty(ref _telefono, value);
                ValidateTelefono();
            }
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

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(CanGuardarStatus));
                    OnPropertyChanged(nameof(DebugInfo));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string WindowTitle => _isNewGestor ? "Nuevo Gestor" : "Editar Gestor";

        // Propiedad para mostrar el nombre en la vista previa
        public string NombrePreview
        {
            get => _nombrePreview;
            set => SetProperty(ref _nombrePreview, value);
        }

        // Propiedad para verificar si se puede guardar
        public bool CanGuardarStatus => CanGuardar();

        // Propiedades de debugging
        public string DebugInfo => $"Nombre: '{Nombre}', CanGuardar: {CanGuardar()}, Error: '{ErrorMessage}'";

        #endregion

        #region Commands

        public ICommand GuardarCommand { get; private set; } = null!;
        public ICommand CancelarCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            GuardarCommand = new RelayCommand(async () => await Guardar(), CanGuardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        #endregion

        #region Command Methods

        public bool CanGuardar()
        {
            return !string.IsNullOrWhiteSpace(Nombre) &&
                   string.IsNullOrEmpty(ErrorMessage);
        }

        private async Task Guardar()
        {
            try
            {
                // Validar que no exista otro gestor con el mismo nombre
                if (!await ValidateNombreUnico())
                {
                    return;
                }

                Gestor gestor;

                if (_isNewGestor)
                {
                    gestor = new Gestor();
                    _dbContext.Gestores.Add(gestor);
                }
                else
                {
                    gestor = _originalGestor!;
                    // Para edición, necesitamos asegurar que Entity Framework rastree el objeto
                    _dbContext.Entry(gestor).State = EntityState.Modified;
                }

                // Actualizar propiedades
                gestor.Nombre = Nombre.Trim();

                // Si el modelo Gestor tiene estas propiedades, descomenta estas líneas:
                // gestor.Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim();
                // gestor.Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim();
                // gestor.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();

                if (_isNewGestor)
                {
                    gestor.FechaCreacion = DateTime.Now;
                }

                // Guardar cambios
                var changes = await _dbContext.SaveChangesAsync();

                if (changes > 0)
                {
                    // Log de la acción
                    await LogAction(_isNewGestor ?
                        $"Creado nuevo gestor: {gestor.Nombre}" :
                        $"Editado gestor: {gestor.Nombre}");

                    MessageBox.Show($"Gestor {(_isNewGestor ? "creado" : "actualizado")} correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    CloseWindow(true);
                }
                else
                {
                    MessageBox.Show("No se realizaron cambios.", "Información",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar gestor: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);

                // Para debug
                System.Diagnostics.Debug.WriteLine($"Error completo: {ex}");
            }
        }

        private void Cancelar()
        {
            CloseWindow(false);
        }

        #endregion

        #region Methods

        private void UpdateNombrePreview()
        {
            NombrePreview = string.IsNullOrWhiteSpace(Nombre) ? "Nombre del gestor" : Nombre.Trim();
        }

        private void LoadGestorData()
        {
            if (_originalGestor == null) return;

            Nombre = _originalGestor.Nombre;
            UpdateNombrePreview(); // Asegurar que la vista previa se actualice

            // Cargar otras propiedades cuando se agreguen al modelo
            // Descripcion = _originalGestor.Descripcion ?? "";
            // Telefono = _originalGestor.Telefono ?? "";
            // Email = _originalGestor.Email ?? "";
        }

        private async Task<bool> ValidateNombreUnico()
        {
            try
            {
                var existeNombre = await _dbContext.Gestores
                    .AnyAsync(g => g.Nombre.ToLower() == Nombre.Trim().ToLower() &&
                                  (!_isNewGestor ? g.Id != _originalGestor!.Id : true));

                if (existeNombre)
                {
                    ErrorMessage = "Ya existe un gestor con este nombre.";
                    return false;
                }

                ErrorMessage = "";
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error validando nombre: {ex.Message}";
                return false;
            }
        }

        private void ValidateNombre()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
            }
            else if (Nombre.Trim().Length < 2)
            {
                ErrorMessage = "El nombre debe tener al menos 2 caracteres.";
            }
            else if (Nombre.Trim().Length > 100)
            {
                ErrorMessage = "El nombre no puede superar los 100 caracteres.";
            }
            else
            {
                ErrorMessage = "";
            }

            // Las notificaciones se manejan en las propiedades SetProperty
        }

        private void ValidateTelefono()
        {
            if (!string.IsNullOrWhiteSpace(Telefono))
            {
                // Validación básica de teléfono español
                var telefonoLimpio = Telefono.Replace(" ", "").Replace("-", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(telefonoLimpio, @"^[6-9]\d{8}$"))
                {
                    ErrorMessage = "El formato del teléfono no es válido (ej: 600123456).";
                    return;
                }
            }

            // Solo limpiar el error si no hay otros errores
            if (ErrorMessage.Contains("teléfono"))
            {
                ErrorMessage = "";
            }
        }

        private void ValidateEmail()
        {
            if (!string.IsNullOrWhiteSpace(Email) && !IsValidEmail(Email))
            {
                ErrorMessage = "El formato del email no es válido.";
                return;
            }

            // Solo limpiar el error si no hay otros errores
            if (ErrorMessage.Contains("email"))
            {
                ErrorMessage = "";
            }
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

        private async Task LogAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "Gestores",
                    FechaHora = DateTime.Now
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log del error pero no interrumpir el flujo
                System.Diagnostics.Debug.WriteLine($"Error logging action: {ex.Message}");
            }
        }

        private void CloseWindow(bool dialogResult)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    if (window is Window w)
                    {
                        w.DialogResult = dialogResult;
                        w.Close();
                    }
                    break;
                }
            }
        }

        // Limpiar recursos
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}