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
using ClubManager.Views;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class TiposAbonoViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<TipoAbonoExtendido> _tiposAbono;
        private ObservableCollection<TipoAbonoExtendido> _tiposAbonoFiltered;
        private TipoAbonoExtendido? _selectedTipoAbono;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;

        public TiposAbonoViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            _tiposAbono = new ObservableCollection<TipoAbonoExtendido>();
            _tiposAbonoFiltered = new ObservableCollection<TipoAbonoExtendido>();

            InitializeCommands();
            LoadData();
            UpdateCanEdit();
        }

        #region Properties

        public ObservableCollection<TipoAbonoExtendido> TiposAbonoFiltered
        {
            get => _tiposAbonoFiltered;
            set => SetProperty(ref _tiposAbonoFiltered, value);
        }

        public TipoAbonoExtendido? SelectedTipoAbono
        {
            get => _selectedTipoAbono;
            set => SetProperty(ref _selectedTipoAbono, value);
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

        public ICommand NewTipoAbonoCommand { get; private set; } = null!;
        public ICommand EditTipoAbonoCommand { get; private set; } = null!;
        public ICommand DeleteTipoAbonoCommand { get; private set; } = null!;
        public ICommand ViewAbonadosCommand { get; private set; } = null!;
        public ICommand ShowStatsCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;
        public ICommand ExportCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NewTipoAbonoCommand = new RelayCommand(NewTipoAbono, () => CanEdit);
            EditTipoAbonoCommand = new RelayCommand<TipoAbonoExtendido>(EditTipoAbono, t => CanEdit && t != null);
            DeleteTipoAbonoCommand = new RelayCommand<TipoAbonoExtendido>(DeleteTipoAbono, t => CanEdit && t != null);
            ViewAbonadosCommand = new RelayCommand<TipoAbonoExtendido>(ViewAbonados, t => t != null);
            ShowStatsCommand = new RelayCommand(ShowStats);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
        }

        #endregion

        #region Command Methods

        // ✅ CORREGIDO: Usar RefreshData() en lugar de LoadData()
        private void NewTipoAbono()
        {
            try
            {
                var editWindow = new EditarTipoAbonoWindow();
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData(); // ✅ Cambio aquí
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear tipo de abono: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ CORREGIDO: Usar RefreshData() en lugar de LoadData()
        private void EditTipoAbono(TipoAbonoExtendido? tipoAbono)
        {
            if (tipoAbono?.TipoAbono == null) return;

            try
            {
                var editWindow = new EditarTipoAbonoWindow(tipoAbono.TipoAbono);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData(); // ✅ Cambio aquí
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar tipo de abono: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteTipoAbono(TipoAbonoExtendido? tipoAbono)
        {
            if (tipoAbono?.TipoAbono == null) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar el tipo de abono '{tipoAbono.Nombre}'?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.TiposAbono.Remove(tipoAbono.TipoAbono);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminado tipo de abono: {tipoAbono.Nombre}");
                    RefreshData(); // ✅ Cambio aquí también

                    MessageBox.Show("Tipo de abono eliminado correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar tipo de abono: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewAbonados(TipoAbonoExtendido? tipoAbono)
        {
            if (tipoAbono == null) return;

            MessageBox.Show($"Ver abonados del tipo '{tipoAbono.Nombre}' - Funcionalidad próximamente",
                          "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowStats()
        {
            var totalTipos = TiposAbonoFiltered.Count;
            var tiposActivos = TiposAbonoFiltered.Count(t => t.TotalAbonados > 0);
            var ingresoTotal = TiposAbonoFiltered.Sum(t => t.IngresosGenerados);

            MessageBox.Show($"Estadísticas de Tipos de Abono:\n\n" +
                          $"Total de tipos: {totalTipos}\n" +
                          $"Tipos con abonados: {tiposActivos}\n" +
                          $"Ingresos totales: €{ingresoTotal:N2}",
                          "Estadísticas", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearFilters()
        {
            SearchText = "";
        }

        private void ExportData()
        {
            MessageBox.Show("Funcionalidad de exportación - Próximamente", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Methods

        // ✅ NUEVO MÉTODO: Para refrescar datos desde la base de datos
        private async void RefreshData()
        {
            try
            {
                // Limpiar el tracking context para asegurar datos frescos
                _dbContext.ChangeTracker.Clear();

                var tipos = await _dbContext.TiposAbono
                    .Include(t => t.Abonados)
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();

                _tiposAbono.Clear();
                foreach (var tipo in tipos)
                {
                    var tipoExtendido = new TipoAbonoExtendido(tipo);
                    _tiposAbono.Add(tipoExtendido);
                }

                ApplyFilters();
                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al refrescar tipos de abono: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ ACTUALIZADO: LoadData ahora usa RefreshData
        private async void LoadData()
        {
            RefreshData();
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _tiposAbono.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(t =>
                        t.Nombre.ToLower().Contains(searchLower) ||
                        (t.Descripcion?.ToLower().Contains(searchLower) ?? false));
                }

                TiposAbonoFiltered.Clear();
                foreach (var tipo in filtered)
                {
                    TiposAbonoFiltered.Add(tipo);
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
            var total = TiposAbonoFiltered.Count;
            var conAbonados = TiposAbonoFiltered.Count(t => t.TotalAbonados > 0);
            SubTitle = $"{total} tipos de abono ({conAbonados} con abonados activos)";
        }

        private void UpdateCanEdit()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            var permissions = UserSession.Instance.CurrentPermissions;
            CanEdit = licenseInfo.IsValid && !licenseInfo.IsExpired &&
                     permissions?.CanAccessTiposAbono == true;
        }

        private async Task LogAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "TiposAbono",
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
    public class TipoAbonoExtendido : BaseViewModel
    {
        public TipoAbono TipoAbono { get; }

        public TipoAbonoExtendido(TipoAbono tipoAbono)
        {
            TipoAbono = tipoAbono;
            CalculateExtendedProperties();
        }

        private void CalculateExtendedProperties()
        {
            TotalAbonados = TipoAbono.Abonados?.Count ?? 0;
            IngresosGenerados = TotalAbonados * TipoAbono.Precio;
            EstadoTexto = TotalAbonados > 0 ? "Activo" : "Sin abonados";
            ColorEstado = TotalAbonados > 0 ? "#4CAF50" : "#F44336";
        }

        // Propiedades del tipo de abono
        public int Id => TipoAbono.Id;
        public string Nombre => TipoAbono.Nombre;
        public string? Descripcion => TipoAbono.Descripcion;
        public decimal Precio => TipoAbono.Precio;
        public DateTime FechaCreacion => TipoAbono.FechaCreacion;

        // Propiedades calculadas
        public int TotalAbonados { get; private set; }
        public decimal IngresosGenerados { get; private set; }
        public string EstadoTexto { get; private set; } = "";
        public string ColorEstado { get; private set; } = "";
        public string PrecioFormateado => $"€{Precio:N2}";
        public string IngresosFormateados => $"€{IngresosGenerados:N2}";
    }
}