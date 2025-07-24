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
    public class EditarPeñaViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly Peña? _originalPeña;
        private bool _isNewPeña;

        // Propiedades de la peña
        private string _nombre = "";
        private string _nombrePreview = "Nombre de la peña";

        // Validación
        private string _errorMessage = "";

        public EditarPeñaViewModel(Peña? peña = null)
        {
            _dbContext = new ClubDbContext();
            _originalPeña = peña;
            _isNewPeña = peña == null;

            InitializeCommands();

            if (_originalPeña != null)
            {
                LoadPeñaData();
            }
            else
            {
                // Para nueva peña, inicializar vista previa
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
                    CommandManager.InvalidateRequerySuggested();
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
                    OnPropertyChanged(nameof(CanGuardarStatus));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string WindowTitle => _isNewPeña ? "Nueva Peña" : "Editar Peña";

        // Propiedad para mostrar el nombre en la vista previa
        public string NombrePreview
        {
            get => _nombrePreview;
            set => SetProperty(ref _nombrePreview, value);
        }

        // Propiedad para verificar si se puede guardar
        public bool CanGuardarStatus => CanGuardar();

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

        private bool CanGuardar()
        {
            return !string.IsNullOrWhiteSpace(Nombre) &&
                   string.IsNullOrEmpty(ErrorMessage);
        }

        private async Task Guardar()
        {
            try
            {
                // Validar que no exista otra peña con el mismo nombre
                if (!await ValidateNombreUnico())
                {
                    return;
                }

                Peña peña;

                if (_isNewPeña)
                {
                    peña = new Peña();
                    _dbContext.Peñas.Add(peña);
                }
                else
                {
                    peña = _originalPeña!;
                    // Para edición, asegurar que Entity Framework rastree el objeto
                    _dbContext.Entry(peña).State = EntityState.Modified;
                }

                // Actualizar propiedades
                peña.Nombre = Nombre.Trim();

                // Guardar cambios
                var changes = await _dbContext.SaveChangesAsync();

                if (changes > 0)
                {
                    // Log de la acción
                    await LogAction(_isNewPeña ?
                        $"Creada nueva peña: {peña.Nombre}" :
                        $"Editada peña: {peña.Nombre}");

                    MessageBox.Show($"Peña {(_isNewPeña ? "creada" : "actualizada")} correctamente.", "Éxito",
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
                MessageBox.Show($"Error al guardar peña: {ex.Message}", "Error",
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
            NombrePreview = string.IsNullOrWhiteSpace(Nombre) ? "Nombre de la peña" : Nombre.Trim();
        }

        private void LoadPeñaData()
        {
            if (_originalPeña == null) return;

            Nombre = _originalPeña.Nombre;
            UpdateNombrePreview(); // Asegurar que la vista previa se actualice
        }

        private async Task<bool> ValidateNombreUnico()
        {
            try
            {
                var existeNombre = await _dbContext.Peñas
                    .AnyAsync(p => p.Nombre.ToLower() == Nombre.Trim().ToLower() &&
                                  (!_isNewPeña ? p.Id != _originalPeña!.Id : true));

                if (existeNombre)
                {
                    ErrorMessage = "Ya existe una peña con este nombre.";
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

        private async Task LogAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "Peñas",
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