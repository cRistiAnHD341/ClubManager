using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;

namespace ClubManager.ViewModels
{
    public class AbonadoEditViewModel : INotifyPropertyChanged, IDataErrorInfo, IDisposable
    {
        private readonly ClubDbContext _dbContext;
        private readonly Abonado? _originalAbonado;
        private readonly bool _isEditing;

        // Campos del formulario
        private string _numeroSocio = "";
        private string _nombre = "";
        private string _apellidos = "";
        private string _dni = "";
        private string _email = "";
        private string _telefono = "";
        private string _direccion = "";
        private string _tallaCamiseta = "Sin especificar";
        private string _observaciones = "";
        private DateTime _fechaNacimiento = DateTime.Today.AddYears(-30);
        private string _codigoBarras = "";
        private bool _esActivo = true;
        private bool _impreso = false;

        // Cambio: usar objetos completos en lugar de solo IDs
        private Gestor? _selectedGestor = null;
        private Peña? _selectedPeña = null;
        private TipoAbono? _selectedTipoAbono = null;

        // UI Properties
        private string _windowTitle = "";
        private string _subTitle = "";

        // Collections para ComboBoxes
        private ObservableCollection<Gestor> _gestores;
        private ObservableCollection<Peña> _peñas;
        private ObservableCollection<TipoAbono> _tiposAbono;
        private ObservableCollection<string> _tallasCamiseta;

        // Validation errors
        private string _numeroSocioError = "";
        private string _nombreError = "";
        private string _apellidosError = "";
        private string _dniError = "";
        private string _emailError = "";
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
            _tallasCamiseta = new ObservableCollection<string>
            {
                "Sin especificar", "XS", "S", "M", "L", "XL", "XXL", "3XL", "4XL", "5XL", "6XL"
            };

            InitializeCommands();
            InitializeUI();
            LoadData();

            if (_isEditing && abonado != null)
            {
                LoadAbonadoData(abonado);
            }
            else
            {
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
                if (SetProperty(ref _numeroSocio, value))
                {
                    ValidateNumeroSocio();
                }
            }
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

        public string Apellidos
        {
            get => _apellidos;
            set
            {
                if (SetProperty(ref _apellidos, value))
                {
                    ValidateApellidos();
                }
            }
        }

        public string DNI
        {
            get => _dni;
            set
            {
                if (SetProperty(ref _dni, value))
                {
                    ValidateDNI();
                    // Regenerar código de barras cuando cambie el DNI
                    if (!string.IsNullOrEmpty(value))
                    {
                        GenerateCodigoBarras();
                    }
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ValidateEmail();
                }
            }
        }

        public string Telefono
        {
            get => _telefono;
            set => SetProperty(ref _telefono, value);
        }

        public string Direccion
        {
            get => _direccion;
            set => SetProperty(ref _direccion, value);
        }

        public string TallaCamiseta
        {
            get => _tallaCamiseta;
            set => SetProperty(ref _tallaCamiseta, value);
        }

        public string Observaciones
        {
            get => _observaciones;
            set => SetProperty(ref _observaciones, value);
        }

        public DateTime FechaNacimiento
        {
            get => _fechaNacimiento;
            set => SetProperty(ref _fechaNacimiento, value);
        }

        public string CodigoBarras
        {
            get => _codigoBarras;
            set => SetProperty(ref _codigoBarras, value);
        }

        public bool EsActivo
        {
            get => _esActivo;
            set => SetProperty(ref _esActivo, value);
        }

        public bool Impreso
        {
            get => _impreso;
            set => SetProperty(ref _impreso, value);
        }

        // Cambio: propiedades para objetos completos
        public Gestor? SelectedGestor
        {
            get => _selectedGestor;
            set => SetProperty(ref _selectedGestor, value);
        }

        public Peña? SelectedPeña
        {
            get => _selectedPeña;
            set => SetProperty(ref _selectedPeña, value);
        }

        public TipoAbono? SelectedTipoAbono
        {
            get => _selectedTipoAbono;
            set
            {
                if (SetProperty(ref _selectedTipoAbono, value))
                {
                    ValidateTipoAbono();
                }
            }
        }

        // Collections
        public ObservableCollection<Gestor> Gestores
        {
            get => _gestores;
            set => SetProperty(ref _gestores, value);
        }

        public ObservableCollection<Peña> Peñas
        {
            get => _peñas;
            set => SetProperty(ref _peñas, value);
        }

        public ObservableCollection<TipoAbono> TiposAbono
        {
            get => _tiposAbono;
            set => SetProperty(ref _tiposAbono, value);
        }

        public ObservableCollection<string> TallasCamiseta
        {
            get => _tallasCamiseta;
            set => SetProperty(ref _tallasCamiseta, value);
        }

        // Validation Properties
        public string NumeroSocioError
        {
            get => _numeroSocioError;
            set
            {
                if (SetProperty(ref _numeroSocioError, value))
                {
                    OnPropertyChanged(nameof(HasNumeroSocioError));
                }
            }
        }

        public string NombreError
        {
            get => _nombreError;
            set
            {
                if (SetProperty(ref _nombreError, value))
                {
                    OnPropertyChanged(nameof(HasNombreError));
                }
            }
        }

        public string ApellidosError
        {
            get => _apellidosError;
            set
            {
                if (SetProperty(ref _apellidosError, value))
                {
                    OnPropertyChanged(nameof(HasApellidosError));
                }
            }
        }

        public string DNIError
        {
            get => _dniError;
            set
            {
                if (SetProperty(ref _dniError, value))
                {
                    OnPropertyChanged(nameof(HasDNIError));
                }
            }
        }

        public string EmailError
        {
            get => _emailError;
            set
            {
                if (SetProperty(ref _emailError, value))
                {
                    OnPropertyChanged(nameof(HasEmailError));
                }
            }
        }

        public string TipoAbonoError
        {
            get => _tipoAbonoError;
            set
            {
                if (SetProperty(ref _tipoAbonoError, value))
                {
                    OnPropertyChanged(nameof(HasTipoAbonoError));
                }
            }
        }

        public Visibility HasNumeroSocioError => string.IsNullOrEmpty(NumeroSocioError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasNombreError => string.IsNullOrEmpty(NombreError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasApellidosError => string.IsNullOrEmpty(ApellidosError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasDNIError => string.IsNullOrEmpty(DNIError) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasEmailError => string.IsNullOrEmpty(EmailError) ? Visibility.Collapsed : Visibility.Visible;
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
                foreach (var gestor in gestores)
                {
                    Gestores.Add(gestor);
                }

                // Cargar peñas
                var peñas = await _dbContext.Peñas.OrderBy(p => p.Nombre).ToListAsync();
                Peñas.Clear();
                foreach (var peña in peñas)
                {
                    Peñas.Add(peña);
                }

                // Cargar tipos de abono activos
                var tiposAbono = await _dbContext.TiposAbono
                    .Where(t => t.Activo)
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();
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
            DNI = abonado.DNI ?? "";
            Email = abonado.Email ?? "";
            Telefono = abonado.Telefono ?? "";
            Direccion = abonado.Direccion ?? "";
            TallaCamiseta = string.IsNullOrWhiteSpace(abonado.TallaCamiseta) ? "Sin especificar" : abonado.TallaCamiseta;
            Observaciones = abonado.Observaciones ?? "";
            FechaNacimiento = abonado.FechaNacimiento;
            CodigoBarras = abonado.CodigoBarras ?? "";
            EsActivo = abonado.Estado == EstadoAbonado.Activo;
            Impreso = abonado.Impreso;

            // Buscar y asignar objetos completos
            SelectedGestor = _gestores.FirstOrDefault(g => g.Id == abonado.GestorId);
            SelectedPeña = _peñas.FirstOrDefault(p => p.Id == abonado.PeñaId);
            SelectedTipoAbono = _tiposAbono.FirstOrDefault(t => t.Id == abonado.TipoAbonoId);

            System.Diagnostics.Debug.WriteLine($"Cargando - TallaCamiseta BD: '{abonado.TallaCamiseta}' -> UI: '{TallaCamiseta}'");
            System.Diagnostics.Debug.WriteLine($"Cargando - SelectedGestor: {SelectedGestor?.Nombre}, SelectedPeña: {SelectedPeña?.Nombre}, SelectedTipoAbono: {SelectedTipoAbono?.Nombre}");
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

        private void ValidateEmail()
        {
            EmailError = "";

            if (string.IsNullOrWhiteSpace(Email))
                return; // Email es opcional

            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(Email, emailPattern))
            {
                EmailError = "El formato del email no es válido";
            }
        }

        private void ValidateTipoAbono()
        {
            TipoAbonoError = SelectedTipoAbono == null ? "Debe seleccionar un tipo de abono" : "";
        }

        private bool CanSave()
        {
            bool isValid = string.IsNullOrEmpty(NumeroSocioError) &&
                          string.IsNullOrEmpty(NombreError) &&
                          string.IsNullOrEmpty(ApellidosError) &&
                          string.IsNullOrEmpty(DNIError) &&
                          string.IsNullOrEmpty(EmailError) &&
                          string.IsNullOrEmpty(TipoAbonoError) &&
                          !string.IsNullOrWhiteSpace(NumeroSocio) &&
                          !string.IsNullOrWhiteSpace(Nombre) &&
                          !string.IsNullOrWhiteSpace(Apellidos) &&
                          !string.IsNullOrWhiteSpace(DNI) &&
                          SelectedTipoAbono != null;

            System.Diagnostics.Debug.WriteLine($"CanSave: {isValid}");
            System.Diagnostics.Debug.WriteLine($"- NumeroSocio: '{NumeroSocio}' (Error: '{NumeroSocioError}')");
            System.Diagnostics.Debug.WriteLine($"- Nombre: '{Nombre}' (Error: '{NombreError}')");
            System.Diagnostics.Debug.WriteLine($"- Apellidos: '{Apellidos}' (Error: '{ApellidosError}')");
            System.Diagnostics.Debug.WriteLine($"- DNI: '{DNI}' (Error: '{DNIError}')");
            System.Diagnostics.Debug.WriteLine($"- TipoAbono: {SelectedTipoAbono?.Nombre} (Error: '{TipoAbonoError}')");

            return isValid;
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
                    _dbContext.Entry(abonado).State = EntityState.Modified;
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
                abonado.DNI = string.IsNullOrWhiteSpace(DNI) ? "00000000A" : DNI.Trim().ToUpper();
                abonado.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim().ToLower();
                abonado.Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim();
                abonado.Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim();
                abonado.TallaCamiseta = (TallaCamiseta == "Sin especificar" || string.IsNullOrWhiteSpace(TallaCamiseta)) ? null : TallaCamiseta;
                abonado.Observaciones = string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones.Trim();
                abonado.FechaNacimiento = FechaNacimiento;
                abonado.CodigoBarras = string.IsNullOrWhiteSpace(CodigoBarras) ? null : CodigoBarras.Trim();
                abonado.Estado = EsActivo ? EstadoAbonado.Activo : EstadoAbonado.Inactivo;
                abonado.Impreso = Impreso;

                // Asignar IDs de objetos relacionados
                abonado.GestorId = SelectedGestor?.Id;
                abonado.PeñaId = SelectedPeña?.Id;
                abonado.TipoAbonoId = SelectedTipoAbono?.Id;

                // Debug
                System.Diagnostics.Debug.WriteLine($"Guardando - TallaCamiseta: '{TallaCamiseta}' -> BD: '{abonado.TallaCamiseta}'");
                System.Diagnostics.Debug.WriteLine($"Guardando - GestorId: {abonado.GestorId}, PeñaId: {abonado.PeñaId}, TipoAbonoId: {abonado.TipoAbonoId}");

                // Si es nuevo, establecer fecha de creación
                if (!_isEditing)
                {
                    abonado.FechaCreacion = DateTime.Now;
                }

                var changes = await _dbContext.SaveChangesAsync();

                if (changes > 0)
                {
                    // Registrar en historial
                    string accion = _isEditing
                        ? $"Editó abonado: {abonado.NombreCompleto} (DNI: {abonado.DNI})"
                        : $"Creó abonado: {abonado.NombreCompleto} (DNI: {abonado.DNI})";

                    await LogAction(accion);

                    string mensaje = _isEditing ? "Abonado actualizado correctamente." : "Abonado creado correctamente.";
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
                MessageBox.Show($"Error al {accion} el abonado: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                SaveCompleted?.Invoke(this, false);

                System.Diagnostics.Debug.WriteLine($"Error completo: {ex}");
            }
        }

        private void GenerateCodigoBarras()
        {
            if (string.IsNullOrWhiteSpace(DNI))
            {
                var timestamp = DateTime.Now.Ticks.ToString().Substring(8);
                var random = new Random().Next(1000, 9999);
                CodigoBarras = $"CLB{timestamp}{random}";
                return;
            }

            try
            {
                CodigoBarras = GenerateSecureBarcode(DNI);
            }
            catch
            {
                var timestamp = DateTime.Now.Ticks.ToString().Substring(8);
                CodigoBarras = $"CLB{DNI.Replace("-", "").Replace(" ", "").Substring(0, Math.Min(8, DNI.Length))}{timestamp.Substring(0, 4)}";
            }
        }

        private string GenerateSecureBarcode(string dni)
        {
            string cleanDni = dni.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            const string secretKey = "ClubManager2024SecretKey_CambiarPorClavePropia";
            string additionalData = $"{NumeroSocio}|{DateTime.Now:yyyyMMdd}";
            string dataToHash = $"{cleanDni}|{secretKey}|{additionalData}";

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                string hash = Convert.ToBase64String(hashBytes)
                    .Replace("+", "A")
                    .Replace("/", "B")
                    .Replace("=", "C");

                string dniPart = cleanDni.Length >= 8 ? cleanDni.Substring(0, 8) : cleanDni.PadRight(8, '0');
                string hashPart = hash.Substring(0, 6);
                string barcode = $"CM{dniPart}{hashPart}";

                return barcode.ToUpperInvariant();
            }
        }

        #endregion

        #region Helper Methods

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

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
                    Detalles = $"Abonado #{NumeroSocio} - {Nombre} {Apellidos}"
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
                    case nameof(Email):
                        return EmailError;
                    case nameof(SelectedTipoAbono):
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