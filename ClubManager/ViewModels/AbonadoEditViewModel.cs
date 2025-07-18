using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;

namespace ClubManager.ViewModels
{
    public class AbonadoEditViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly ClubDbContext _dbContext;
        private readonly Abonado? _originalAbonado;
        private readonly bool _isEditing;

        // Campos del formulario
        private string _numeroSocio = "";
        private string _nombre = "";
        private string _apellidos = "";
        private string _dni = "";
        private string _codigoBarras = "";
        private bool _esActivo = false;
        private bool _impreso = false;
        private int? _gestorId;
        private int? _peñaId;
        private int? _tipoAbonoId;

        // UI Properties
        private string _windowTitle = "";
        private string _subTitle = "";

        // Collections para ComboBoxes
        private ObservableCollection<Gestor> _gestores;
        private ObservableCollection<Peña> _peñas;
        private ObservableCollection<TipoAbono> _tiposAbono;

        // Validation errors
        private string _numeroSocioError = "";
        private string _nombreError = "";
        private string _apellidosError = "";
        private string _dniError = "";
        private string _tipoAbonoError = "";

        public event EventHandler<bool>? SaveCompleted;

        public AbonadoEditViewModel(Abonado? abonado = null)
        {
            _dbContext = new ClubDbContext();
            _originalAbonado = abonado;
            _isEditing = abonado != null;

            _gestores = new ObservableCollection<Gestor>();
            _peñas = new ObservableCollection<Peña>();
            _tiposAbono = new ObservableCollection<TipoAbono>();

            InitializeCommands();
            InitializeUI();
            LoadData();

            if (_isEditing && abonado != null)
            {
                LoadAbonadoData(abonado);
            }
            else
            {
                GenerateCodigoBarras();
                GenerateNextNumeroSocio();
            }
        }

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        public string SubTitle
        {
            get => _subTitle;
            set { _subTitle = value; OnPropertyChanged(); }
        }

        public string NumeroSocio
        {
            get => _numeroSocio;
            set
            {
                _numeroSocio = value;
                OnPropertyChanged();
                ValidateNumeroSocio();
            }
        }

        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged();
                ValidateNombre();
            }
        }

        public string Apellidos
        {
            get => _apellidos;
            set
            {
                _apellidos = value;
                OnPropertyChanged();
                ValidateApellidos();
            }
        }

        public string DNI
        {
            get => _dni;
            set
            {
                _dni = value;
                OnPropertyChanged();
                ValidateDNI();
            }
        }

        public string CodigoBarras
        {
            get => _codigoBarras;
            set { _codigoBarras = value; OnPropertyChanged(); }
        }

        public bool EsActivo
        {
            get => _esActivo;
            set { _esActivo = value; OnPropertyChanged(); }
        }

        public bool Impreso
        {
            get => _impreso;
            set { _impreso = value; OnPropertyChanged(); }
        }

        public int? GestorId
        {
            get => _gestorId;
            set { _gestorId = value; OnPropertyChanged(); }
        }

        public int? PeñaId
        {
            get => _peñaId;
            set { _peñaId = value; OnPropertyChanged(); }
        }

        public int? TipoAbonoId
        {
            get => _tipoAbonoId;
            set
            {
                _tipoAbonoId = value;
                OnPropertyChanged();
                ValidateTipoAbono();
            }
        }

        // Collections
        public ObservableCollection<Gestor> Gestores
        {
            get => _gestores;
            set { _gestores = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Peña> Peñas
        {
            get => _peñas;
            set { _peñas = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TipoAbono> TiposAbono
        {
            get => _tiposAbono;
            set { _tiposAbono = value; OnPropertyChanged(); }
        }

        // Validation Properties
        public string NumeroSocioError
        {
            get => _numeroSocioError;
            set { _numeroSocioError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNumeroSocioError)); }
        }

        public string NombreError
        {
            get => _nombreError;
            set { _nombreError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNombreError)); }
        }

        public string ApellidosError
        {
            get => _apellidosError;
            set { _apellidosError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasApellidosError)); }
        }

        public string DNIError
        {
            get => _dniError;
            set { _dniError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDNIError)); }
        }

        public string TipoAbonoError
        {
            get => _tipoAbonoError;
            set { _tipoAbonoError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasTipoAbonoError)); }
        }

        public Visibility HasNumeroSocioError => string.IsNullOrEmpty(NumeroSocioError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasNombreError => string.IsNullOrEmpty(NombreError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasApellidosError => string.IsNullOrEmpty(ApellidosError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasDNIError => string.IsNullOrEmpty(DNIError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasTipoAbonoError => string.IsNullOrEmpty(TipoAbonoError) ? Visibility.Collapsed : Visibility.Visible;

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand GenerateCodigoBarrasCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(SaveAbonado, CanSave);
            GenerateCodigoBarrasCommand = new RelayCommand(GenerateCodigoBarras);
        }

        private void InitializeUI()
        {
            if (_isEditing)
            {
                WindowTitle = "✏️ Editar Abonado";
                SubTitle = "Modifica los datos del abonado seleccionado";
            }
            else
            {
                WindowTitle = "➕ Nuevo Abonado";
                SubTitle = "Introduce los datos del nuevo abonado";
            }
        }

        private async void LoadData()
        {
            try
            {
                // Cargar gestores
                var gestores = await _dbContext.Gestores.OrderBy(g => g.Nombre).ToListAsync();
                Gestores.Clear();
                Gestores.Add(new Gestor { Id = 0, Nombre = "Sin asignar" });
                foreach (var gestor in gestores)
                {
                    Gestores.Add(gestor);
                }

                // Cargar peñas
                var peñas = await _dbContext.Peñas.OrderBy(p => p.Nombre).ToListAsync();
                Peñas.Clear();
                Peñas.Add(new Peña { Id = 0, Nombre = "Sin asignar" });
                foreach (var peña in peñas)
                {
                    Peñas.Add(peña);
                }

                // Cargar tipos de abono
                var tiposAbono = await _dbContext.TiposAbono.OrderBy(t => t.Nombre).ToListAsync();
                TiposAbono.Clear();
                foreach (var tipo in tiposAbono)
                {
                    TiposAbono.Add(tipo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAbonadoData(Abonado abonado)
        {
            NumeroSocio = abonado.NumeroSocio.ToString();
            Nombre = abonado.Nombre;
            Apellidos = abonado.Apellidos;
            DNI = abonado.DNI;
            CodigoBarras = abonado.CodigoBarras;
            EsActivo = abonado.Estado == EstadoAbonado.Activo;
            Impreso = abonado.Impreso;
            GestorId = abonado.GestorId;
            PeñaId = abonado.PeñaId;
            TipoAbonoId = abonado.TipoAbonoId;
        }

        private async void GenerateNextNumeroSocio()
        {
            try
            {
                var maxNumero = await _dbContext.Abonados.MaxAsync(a => (int?)a.NumeroSocio) ?? 0;
                NumeroSocio = (maxNumero + 1).ToString();
            }
            catch
            {
                NumeroSocio = "1";
            }
        }

        #endregion

        #region Validation

        private void ValidateNumeroSocio()
        {
            NumeroSocioError = "";

            if (string.IsNullOrWhiteSpace(NumeroSocio))
            {
                NumeroSocioError = "El número de socio es obligatorio";
                return;
            }

            if (!int.TryParse(NumeroSocio, out int numero) || numero <= 0)
            {
                NumeroSocioError = "El número de socio debe ser un número positivo";
                return;
            }

            // Verificar que no exista (solo si es nuevo o cambió el número)
            Task.Run(async () =>
            {
                try
                {
                    bool exists = await _dbContext.Abonados
                        .AnyAsync(a => a.NumeroSocio == numero &&
                                      (_originalAbonado == null || a.Id != _originalAbonado.Id));

                    if (exists)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            NumeroSocioError = "Ya existe un abonado con este número de socio";
                        });
                    }
                }
                catch { }
            });
        }

        private void ValidateNombre()
        {
            NombreError = string.IsNullOrWhiteSpace(Nombre) ? "El nombre es obligatorio" : "";
        }

        private void ValidateApellidos()
        {
            ApellidosError = string.IsNullOrWhiteSpace(Apellidos) ? "Los apellidos son obligatorios" : "";
        }

        private void ValidateDNI()
        {
            DNIError = "";

            if (string.IsNullOrWhiteSpace(DNI))
            {
                DNIError = "El DNI es obligatorio";
                return;
            }

            if (DNI.Length < 8)
            {
                DNIError = "El DNI debe tener al menos 8 caracteres";
                return;
            }

            // Verificar que no exista (solo si es nuevo o cambió el DNI)
            Task.Run(async () =>
            {
                try
                {
                    bool exists = await _dbContext.Abonados
                        .AnyAsync(a => a.DNI == DNI &&
                                      (_originalAbonado == null || a.Id != _originalAbonado.Id));

                    if (exists)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DNIError = "Ya existe un abonado con este DNI";
                        });
                    }
                }
                catch { }
            });
        }

        private void ValidateTipoAbono()
        {
            TipoAbonoError = !TipoAbonoId.HasValue || TipoAbonoId.Value == 0 ? "Debe seleccionar un tipo de abono" : "";
        }

        private bool CanSave()
        {
            return string.IsNullOrEmpty(NumeroSocioError) &&
                   string.IsNullOrEmpty(NombreError) &&
                   string.IsNullOrEmpty(ApellidosError) &&
                   string.IsNullOrEmpty(DNIError) &&
                   string.IsNullOrEmpty(TipoAbonoError) &&
                   !string.IsNullOrWhiteSpace(NumeroSocio) &&
                   !string.IsNullOrWhiteSpace(Nombre) &&
                   !string.IsNullOrWhiteSpace(Apellidos) &&
                   !string.IsNullOrWhiteSpace(DNI) &&
                   TipoAbonoId.HasValue && TipoAbonoId.Value > 0;
        }

        #endregion

        #region Commands Implementation

        private async void SaveAbonado()
        {
            if (!CanSave())
            {
                MessageBox.Show("Por favor, corrige los errores antes de guardar.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Abonado abonado;

                if (_isEditing && _originalAbonado != null)
                {
                    // Actualizar abonado existente
                    abonado = _originalAbonado;
                }
                else
                {
                    // Crear nuevo abonado
                    abonado = new Abonado();
                    _dbContext.Abonados.Add(abonado);
                }

                // Mapear datos del formulario al modelo
                abonado.NumeroSocio = int.Parse(NumeroSocio);
                abonado.Nombre = Nombre.Trim();
                abonado.Apellidos = Apellidos.Trim();
                abonado.DNI = DNI.Trim().ToUpper();
                abonado.CodigoBarras = CodigoBarras;
                abonado.Estado = EsActivo ? EstadoAbonado.Activo : EstadoAbonado.Inactivo;
                abonado.Impreso = Impreso;
                abonado.GestorId = GestorId > 0 ? GestorId : null;
                abonado.PeñaId = PeñaId > 0 ? PeñaId : null;
                abonado.TipoAbonoId = TipoAbonoId;

                // Si es nuevo, establecer fecha de creación
                if (!_isEditing)
                {
                    abonado.FechaCreacion = DateTime.Now;
                }

                await _dbContext.SaveChangesAsync();

                // Registrar en historial
                string accion = _isEditing
                    ? $"Editó abonado: {abonado.NombreCompleto} (DNI: {abonado.DNI})"
                    : $"Creó abonado: {abonado.NombreCompleto} (DNI: {abonado.DNI})";

                await LogAction(accion);

                string mensaje = _isEditing ? "Abonado actualizado correctamente." : "Abonado creado correctamente.";
                MessageBox.Show(mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                SaveCompleted?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                string accion = _isEditing ? "actualizar" : "crear";
                MessageBox.Show($"Error al {accion} el abonado: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                SaveCompleted?.Invoke(this, false);
            }
        }

        private void GenerateCodigoBarras()
        {
            // Generar código de barras único basado en timestamp y random
            var timestamp = DateTime.Now.Ticks.ToString().Substring(8);
            var random = new Random().Next(1000, 9999);
            CodigoBarras = $"CLB{timestamp}{random}";
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

        #region IDataErrorInfo

        public string Error => "";

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(NumeroSocio):
                        return NumeroSocioError;
                    case nameof(Nombre):
                        return NombreError;
                    case nameof(Apellidos):
                        return ApellidosError;
                    case nameof(DNI):
                        return DNIError;
                    case nameof(TipoAbonoId):
                        return TipoAbonoError;
                    default:
                        return "";
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Revalidar comando Save cuando cambian las propiedades
            if (propertyName != nameof(WindowTitle) && propertyName != nameof(SubTitle))
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