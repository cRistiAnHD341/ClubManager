using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class EditarTipoAbonoViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ClubDbContext _dbContext;
        private readonly TipoAbono? _originalTipoAbono;
        private readonly bool _isEditing;

        // Propiedades del tipo de abono
        private string _nombre = "";
        private string _descripcion = "";
        private decimal _precio = 0;

        // UI Properties
        private string _windowTitle = "";
        private string _subTitle = "";

        // Validation
        private string _errorMessage = "";

        // Evento para notificar cuando se completa el guardado
        public event EventHandler<bool>? SaveCompleted;

        public EditarTipoAbonoViewModel(TipoAbono? tipoAbono = null)
        {
            _dbContext = new ClubDbContext();
            _originalTipoAbono = tipoAbono;
            _isEditing = tipoAbono != null;

            InitializeCommands();
            InitializeUI();

            if (_isEditing && tipoAbono != null)
            {
                LoadTipoAbonoData(tipoAbono);
            }
        }

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public string SubTitle
        {
            get => _subTitle;
            set => SetProperty(ref _subTitle, value);
        }

        public string Nombre
        {
            get => _nombre;
            set
            {
                if (SetProperty(ref _nombre, value))
                {
                    ValidateNombre();
                }
            }
        }

        public string Descripcion
        {
            get => _descripcion;
            set => SetProperty(ref _descripcion, value);
        }

        public decimal Precio
        {
            get => _precio;
            set
            {
                if (SetProperty(ref _precio, value))
                {
                    ValidatePrecio();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        // Propiedades para la UI sin converters
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        #endregion

        #region Commands

        public ICommand GuardarCommand { get; private set; }
        public ICommand CancelarCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            GuardarCommand = new RelayCommand(SaveTipoAbono, CanSave);
            CancelarCommand = new RelayCommand(Cancel);
        }

        private void InitializeUI()
        {
            if (_isEditing)
            {
                WindowTitle = "✏️ Editar Tipo de Abono";
                SubTitle = "Modifica los datos del tipo de abono seleccionado";
            }
            else
            {
                WindowTitle = "➕ Nuevo Tipo de Abono";
                SubTitle = "Introduce los datos del nuevo tipo de abono";
            }
        }

        private void LoadTipoAbonoData(TipoAbono tipoAbono)
        {
            Nombre = tipoAbono.Nombre;
            Descripcion = tipoAbono.Descripcion ?? "";
            Precio = tipoAbono.Precio;

            // Debug para verificar carga
            System.Diagnostics.Debug.WriteLine($"Cargando TipoAbono - Nombre: '{Nombre}', Precio: {Precio}");
        }

        #endregion

        #region Validation

        private void ValidateNombre()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                ErrorMessage = "El nombre es obligatorio";
                return;
            }

            if (Nombre.Trim().Length < 3)
            {
                ErrorMessage = "El nombre debe tener al menos 3 caracteres";
                return;
            }

            // Validación asíncrona de nombre único
            ValidateNombreUnicoAsync();
        }

        private async void ValidateNombreUnicoAsync()
        {
            try
            {
                var nombreTrimmed = Nombre.Trim();
                var exists = await _dbContext.TiposAbono
                    .AnyAsync(t => t.Nombre.ToLower() == nombreTrimmed.ToLower() &&
                                  (_originalTipoAbono == null || t.Id != _originalTipoAbono.Id));

                if (exists)
                {
                    ErrorMessage = "Ya existe un tipo de abono con este nombre";
                }
                else if (ErrorMessage == "Ya existe un tipo de abono con este nombre" ||
                         ErrorMessage == "El nombre es obligatorio" ||
                         ErrorMessage == "El nombre debe tener al menos 3 caracteres")
                {
                    ErrorMessage = "";
                }
            }
            catch
            {
                // Si falla la validación, no es crítico
            }
        }

        private void ValidatePrecio()
        {
            if (Precio < 0)
            {
                ErrorMessage = "El precio no puede ser negativo";
            }
            else if (Precio > 9999.99m)
            {
                ErrorMessage = "El precio no puede superar los 9999.99€";
            }
            else if (ErrorMessage == "El precio no puede ser negativo" ||
                     ErrorMessage == "El precio no puede superar los 9999.99€")
            {
                ErrorMessage = "";
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Nombre) &&
                   Precio >= 0 &&
                   string.IsNullOrEmpty(ErrorMessage);
        }

        #endregion

        #region Commands Implementation

        private async void SaveTipoAbono()
        {
            if (!CanSave())
            {
                MessageBox.Show("Por favor, corrige los errores antes de guardar.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                TipoAbono tipoAbono;

                if (_isEditing && _originalTipoAbono != null)
                {
                    // ✅ CORRECCIÓN: Obtener la entidad desde la base de datos para asegurar tracking
                    tipoAbono = await _dbContext.TiposAbono.FindAsync(_originalTipoAbono.Id);
                    if (tipoAbono == null)
                    {
                        MessageBox.Show("El tipo de abono ya no existe en la base de datos.", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        SaveCompleted?.Invoke(this, false);
                        return;
                    }
                }
                else
                {
                    // Crear nuevo tipo de abono
                    tipoAbono = new TipoAbono();
                    _dbContext.TiposAbono.Add(tipoAbono);
                }

                // Mapear datos del formulario al modelo
                tipoAbono.Nombre = Nombre.Trim();
                tipoAbono.Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim();
                tipoAbono.Precio = Precio;
                tipoAbono.Activo = true; // Por defecto activo

                // Si es nuevo, establecer fecha de creación
                if (!_isEditing)
                {
                    tipoAbono.FechaCreacion = DateTime.Now;
                }

                // Debug para verificar guardado
                System.Diagnostics.Debug.WriteLine($"Guardando TipoAbono - ID: {tipoAbono.Id}, Nombre: '{tipoAbono.Nombre}', Precio: {tipoAbono.Precio}");

                var changes = await _dbContext.SaveChangesAsync();

                if (changes > 0)
                {
                    // Registrar en historial
                    string accion = _isEditing
                        ? $"Editó tipo de abono: {tipoAbono.Nombre} (Precio: {tipoAbono.Precio:C})"
                        : $"Creó tipo de abono: {tipoAbono.Nombre} (Precio: {tipoAbono.Precio:C})";

                    await LogAction(accion);

                    string mensaje = _isEditing ? "Tipo de abono actualizado correctamente." : "Tipo de abono creado correctamente.";
                    MessageBox.Show(mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    SaveCompleted?.Invoke(this, true);
                }
                else
                {
                    MessageBox.Show("No se realizaron cambios.", "Información",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                string accion = _isEditing ? "actualizar" : "crear";
                MessageBox.Show($"Error al {accion} el tipo de abono: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                SaveCompleted?.Invoke(this, false);

                // Para debug
                System.Diagnostics.Debug.WriteLine($"Error completo: {ex}");
            }
        }

        private void Cancel()
        {
            SaveCompleted?.Invoke(this, false);
        }

        #endregion

        #region Helper Methods

        private async Task LogAction(string accion)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = accion,
                    TipoAccion = _isEditing ? "Edición" : "Creación",
                    FechaHora = DateTime.Now,
                    Detalles = $"Tipo de abono: {Nombre} - Precio: {Precio:C}"
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Si falla el log, no es crítico pero log para debug
                System.Diagnostics.Debug.WriteLine($"Error logging action: {ex.Message}");
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            CommandManager.InvalidateRequerySuggested();
            return true;
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
}