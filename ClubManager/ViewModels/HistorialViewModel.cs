using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class HistorialViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<HistorialAccionExtendido> _historialAcciones;
        private ObservableCollection<HistorialAccionExtendido> _historialFiltered;
        private HistorialAccionExtendido? _selectedAccion;
        private string _searchText = "";
        private string _subTitle = "";
        private DateTime _fechaDesde = DateTime.Today.AddDays(-30);
        private DateTime _fechaHasta = DateTime.Today.AddDays(1);
        private string _tipoAccionFilter = "Todos";

        // Filtros
        private ObservableCollection<string> _tiposAccion;
        private ObservableCollection<Usuario> _usuariosFilter;
        private int? _selectedUsuarioFilter;

        public HistorialViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            _historialAcciones = new ObservableCollection<HistorialAccionExtendido>();
            _historialFiltered = new ObservableCollection<HistorialAccionExtendido>();
            _tiposAccion = new ObservableCollection<string>();
            _usuariosFilter = new ObservableCollection<Usuario>();

            InitializeCommands();
            LoadFilters();
            LoadData();
        }

        #region Properties

        public ObservableCollection<HistorialAccionExtendido> HistorialFiltered
        {
            get => _historialFiltered;
            set => SetProperty(ref _historialFiltered, value);
        }

        public HistorialAccionExtendido? SelectedAccion
        {
            get => _selectedAccion;
            set => SetProperty(ref _selectedAccion, value);
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

        public DateTime FechaDesde
        {
            get => _fechaDesde;
            set
            {
                SetProperty(ref _fechaDesde, value);
                ApplyFilters();
            }
        }

        public DateTime FechaHasta
        {
            get => _fechaHasta;
            set
            {
                SetProperty(ref _fechaHasta, value);
                ApplyFilters();
            }
        }

        public string TipoAccionFilter
        {
            get => _tipoAccionFilter;
            set
            {
                SetProperty(ref _tipoAccionFilter, value);
                ApplyFilters();
            }
        }

        public int? SelectedUsuarioFilter
        {
            get => _selectedUsuarioFilter;
            set
            {
                SetProperty(ref _selectedUsuarioFilter, value);
                ApplyFilters();
            }
        }

        public ObservableCollection<string> TiposAccion => _tiposAccion;
        public ObservableCollection<Usuario> UsuariosFilter => _usuariosFilter;

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;
        public ICommand ExportCommand { get; private set; } = null!;
        public ICommand DeleteOldCommand { get; private set; } = null!;
        public ICommand ViewDetailsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand(async () => await RefreshData());
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
            DeleteOldCommand = new RelayCommand(DeleteOldRecords);
            ViewDetailsCommand = new RelayCommand<HistorialAccionExtendido>(ViewDetails, a => a != null);
        }

        #endregion

        #region Command Methods

        private async Task RefreshData()
        {
            LoadData();
            await Task.Delay(100); // Simular carga
        }

        private void ClearFilters()
        {
            SearchText = "";
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today.AddDays(1);
            TipoAccionFilter = "Todos";
            SelectedUsuarioFilter = null;
        }

        private void ExportData()
        {
            MessageBox.Show("Funcionalidad de exportación - Próximamente", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteOldRecords()
        {
            var result = MessageBox.Show(
                "¿Está seguro de eliminar registros antiguos (más de 6 meses)?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var fechaLimite = DateTime.Now.AddMonths(-6);
                    var registrosAntiguos = await _dbContext.HistorialAcciones
                        .Where(h => h.FechaHora < fechaLimite)
                        .ToListAsync();

                    if (registrosAntiguos.Any())
                    {
                        _dbContext.HistorialAcciones.RemoveRange(registrosAntiguos);
                        await _dbContext.SaveChangesAsync();

                        MessageBox.Show($"Se eliminaron {registrosAntiguos.Count} registros antiguos.", "Éxito",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("No hay registros antiguos para eliminar.", "Información",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar registros: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewDetails(HistorialAccionExtendido? accion)
        {
            if (accion == null) return;

            var detalles = $"Detalles de la Acción:\n\n" +
                          $"Usuario: {accion.NombreUsuario}\n" +
                          $"Acción: {accion.Accion}\n" +
                          $"Tipo: {accion.TipoAccion}\n" +
                          $"Fecha: {accion.FechaCompleta}\n" +
                          $"Detalles: {accion.Detalles ?? "Sin detalles adicionales"}";

            MessageBox.Show(detalles, "Detalles de la Acción", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Methods

        private async void LoadData()
        {
            try
            {
                var acciones = await _dbContext.HistorialAcciones
                    .Include(h => h.Usuario)
                    .OrderByDescending(h => h.FechaHora)
                    .Take(1000) // Limitar a las últimas 1000 acciones
                    .ToListAsync();

                _historialAcciones.Clear();
                foreach (var accion in acciones)
                {
                    var accionExtendida = new HistorialAccionExtendido(accion);
                    _historialAcciones.Add(accionExtendida);
                }

                ApplyFilters();
                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadFilters()
        {
            try
            {
                // Cargar tipos de acción únicos
                var tipos = await _dbContext.HistorialAcciones
                    .Select(h => h.TipoAccion)
                    .Distinct()
                    .Where(t => !string.IsNullOrEmpty(t))
                    .OrderBy(t => t)
                    .ToListAsync();

                _tiposAccion.Clear();
                _tiposAccion.Add("Todos");
                foreach (var tipo in tipos)
                {
                    _tiposAccion.Add(tipo!);
                }

                // Cargar usuarios
                var usuarios = await _dbContext.Usuarios
                    .OrderBy(u => u.NombreUsuario)
                    .ToListAsync();

                _usuariosFilter.Clear();
                _usuariosFilter.Add(new Usuario { Id = 0, NombreUsuario = "Todos los usuarios" });
                foreach (var usuario in usuarios)
                {
                    _usuariosFilter.Add(usuario);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando filtros: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _historialAcciones.AsEnumerable();

                // Filtro por texto
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(h =>
                        h.Accion.ToLower().Contains(searchLower) ||
                        h.NombreUsuario.ToLower().Contains(searchLower) ||
                        (h.Detalles?.ToLower().Contains(searchLower) ?? false));
                }

                // Filtro por fechas
                filtered = filtered.Where(h => h.FechaHora >= FechaDesde && h.FechaHora <= FechaHasta);

                // Filtro por tipo de acción
                if (TipoAccionFilter != "Todos")
                {
                    filtered = filtered.Where(h => h.TipoAccion == TipoAccionFilter);
                }

                // Filtro por usuario
                if (SelectedUsuarioFilter.HasValue && SelectedUsuarioFilter.Value > 0)
                {
                    filtered = filtered.Where(h => h.UsuarioId == SelectedUsuarioFilter.Value);
                }

                HistorialFiltered.Clear();
                foreach (var accion in filtered)
                {
                    HistorialFiltered.Add(accion);
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
            var total = HistorialFiltered.Count;
            var fechaInicio = HistorialFiltered.Any() ? HistorialFiltered.Min(h => h.FechaHora) : DateTime.Now;
            var fechaFin = HistorialFiltered.Any() ? HistorialFiltered.Max(h => h.FechaHora) : DateTime.Now;

            SubTitle = $"{total} acciones registradas (desde {fechaInicio:dd/MM/yyyy} hasta {fechaFin:dd/MM/yyyy})";
        }

        #endregion
    }

    // Clase extendida para mostrar información adicional
    public class HistorialAccionExtendido : BaseViewModel
    {
        public HistorialAccion HistorialAccion { get; }

        public HistorialAccionExtendido(HistorialAccion historialAccion)
        {
            HistorialAccion = historialAccion;
        }

        // Propiedades del historial
        public int Id => HistorialAccion.Id;
        public int UsuarioId => HistorialAccion.UsuarioId;
        public string NombreUsuario => HistorialAccion.Usuario?.NombreUsuario ?? "Usuario desconocido";
        public string Accion => HistorialAccion.Accion;
        public string TipoAccion => HistorialAccion.TipoAccion ?? "General";
        public DateTime FechaHora => HistorialAccion.FechaHora;
        public string? Detalles => HistorialAccion.Detalles;

        // Propiedades calculadas
        public string FechaTexto => FechaHora.ToString("dd/MM HH:mm");
        public string FechaCompleta => FechaHora.ToString("dd/MM/yyyy HH:mm:ss");
        public string IconoTipo => GetIconoTipo(TipoAccion);
        public string ColorTipo => GetColorTipo(TipoAccion);

        private string GetIconoTipo(string tipo)
        {
            return tipo switch
            {
                "Login" => "🔑",
                "Abonados" => "👤",
                "Usuarios" => "👥",
                "TiposAbono" => "🎫",
                "Gestores" => "🏢",
                "Peñas" => "🚩",
                "Configuracion" => "⚙️",
                "Sistema" => "🔧",
                _ => "📝"
            };
        }

        private string GetColorTipo(string tipo)
        {
            return tipo switch
            {
                "Login" => "#4CAF50",
                "Abonados" => "#2196F3",
                "Usuarios" => "#FF9800",
                "TiposAbono" => "#9C27B0",
                "Gestores" => "#607D8B",
                "Peñas" => "#E91E63",
                "Configuracion" => "#795548",
                "Sistema" => "#F44336",
                _ => "#6C757D"
            };
        }
    }
}