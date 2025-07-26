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
    public class AbonadosViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;
        private readonly IConfiguracionService _configuracionService;

        private ObservableCollection<AbonadoSelectableViewModel> _abonados;
        private ObservableCollection<AbonadoSelectableViewModel> _abonadosFiltered;
        private AbonadoSelectableViewModel? _selectedAbonado;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;
        private bool _selectAll = false;
        private bool _updatingSelection = false;

        // Filtros - cambio a usar objetos completos en lugar de IDs
        private ObservableCollection<Gestor> _gestoresFilter;
        private ObservableCollection<Peña> _peñasFilter;
        private ObservableCollection<TipoAbono> _tiposAbonoFilter;
        private ObservableCollection<EstadoFilterItem> _estadosFilter;
        private ObservableCollection<ImpresoFilterItem> _impresosFilter;

        private Gestor? _selectedGestorFilter;
        private Peña? _selectedPeñaFilter;
        private TipoAbono? _selectedTipoAbonoFilter;
        private EstadoFilterItem? _selectedEstadoFilter;
        private ImpresoFilterItem? _selectedImpresoFilter;

        public AbonadosViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();
            _configuracionService = new ConfiguracionService();

            _abonados = new ObservableCollection<AbonadoSelectableViewModel>();
            _abonadosFiltered = new ObservableCollection<AbonadoSelectableViewModel>();
            _gestoresFilter = new ObservableCollection<Gestor>();
            _peñasFilter = new ObservableCollection<Peña>();
            _tiposAbonoFilter = new ObservableCollection<TipoAbono>();
            _estadosFilter = new ObservableCollection<EstadoFilterItem>();
            _impresosFilter = new ObservableCollection<ImpresoFilterItem>();

            InitializeCommands();
            InitializeFilters();
            LoadFilters();
            LoadData();
            UpdateCanEdit();
        }

        #region Properties

        public ObservableCollection<AbonadoSelectableViewModel> AbonadosFiltered
        {
            get => _abonadosFiltered;
            set => SetProperty(ref _abonadosFiltered, value);
        }

        public AbonadoSelectableViewModel? SelectedAbonado
        {
            get => _selectedAbonado;
            set => SetProperty(ref _selectedAbonado, value);
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

        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                if (_selectAll != value)
                {
                    _selectAll = value;
                    OnPropertyChanged();

                    // Solo llamar UpdateAllSelection si el cambio viene del usuario, no de la lógica interna
                    if (!_updatingSelection)
                    {
                        UpdateAllSelection();
                    }
                }
            }
        }
        public int SelectedCount => AbonadosFiltered.Count(a => a.IsSelected);

        public bool HasSelectedItems => SelectedCount > 0;

        // Propiedades de filtros
        public ObservableCollection<Gestor> GestoresFilter => _gestoresFilter;
        public ObservableCollection<Peña> PeñasFilter => _peñasFilter;
        public ObservableCollection<TipoAbono> TiposAbonoFilter => _tiposAbonoFilter;
        public ObservableCollection<EstadoFilterItem> EstadosFilter => _estadosFilter;
        public ObservableCollection<ImpresoFilterItem> ImpresosFilter => _impresosFilter;

        public Gestor? SelectedGestorFilter
        {
            get => _selectedGestorFilter;
            set { SetProperty(ref _selectedGestorFilter, value); ApplyFilters(); }
        }

        public Peña? SelectedPeñaFilter
        {
            get => _selectedPeñaFilter;
            set { SetProperty(ref _selectedPeñaFilter, value); ApplyFilters(); }
        }

        public TipoAbono? SelectedTipoAbonoFilter
        {
            get => _selectedTipoAbonoFilter;
            set { SetProperty(ref _selectedTipoAbonoFilter, value); ApplyFilters(); }
        }

        public EstadoFilterItem? SelectedEstadoFilter
        {
            get => _selectedEstadoFilter;
            set { SetProperty(ref _selectedEstadoFilter, value); ApplyFilters(); }
        }

        public ImpresoFilterItem? SelectedImpresoFilter
        {
            get => _selectedImpresoFilter;
            set { SetProperty(ref _selectedImpresoFilter, value); ApplyFilters(); }
        }

        #endregion

        #region Commands

        public ICommand NewAbonadoCommand { get; private set; } = null!;
        public ICommand EditAbonadoCommand { get; private set; } = null!;
        public ICommand DeleteAbonadoCommand { get; private set; } = null!;
        public ICommand ToggleEstadoCommand { get; private set; } = null!;
        public ICommand MarkAsPrintedCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;
        public ICommand ExportCommand { get; private set; } = null!;
        public ICommand PrintSelectedCommand { get; private set; } = null!;

        // Comandos para acciones múltiples
        public ICommand DeleteSelectedCommand { get; private set; } = null!;
        public ICommand ActivateSelectedCommand { get; private set; } = null!;
        public ICommand DeactivateSelectedCommand { get; private set; } = null!;
        public ICommand MarkSelectedAsPrintedCommand { get; private set; } = null!;
        public ICommand MarkSelectedAsNotPrintedCommand { get; private set; } = null!;
        public ICommand ClearSelectionCommand { get; private set; } = null!;

        // Comandos Impresion de Tarjetas
        public ICommand PrintCardCommand { get; private set; } = null!;
        public ICommand PrintSelectedCardsCommand { get; private set; } = null!;

        // Comando NFC
        public ICommand WriteNfcCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NewAbonadoCommand = new RelayCommand(NewAbonado, () => CanEdit);
            EditAbonadoCommand = new RelayCommand<AbonadoSelectableViewModel>(EditAbonado, a => CanEdit && a != null);
            DeleteAbonadoCommand = new RelayCommand<AbonadoSelectableViewModel>(DeleteAbonado, a => CanEdit && a != null);
            ToggleEstadoCommand = new RelayCommand<AbonadoSelectableViewModel>(ToggleEstado, a => CanEdit && a != null);
            MarkAsPrintedCommand = new RelayCommand<AbonadoSelectableViewModel>(MarkAsPrinted, a => CanEdit && a != null);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
            PrintSelectedCommand = new RelayCommand(PrintSelected, () => SelectedAbonado != null);
            PrintCardCommand = new RelayCommand<AbonadoSelectableViewModel>(PrintCard, a => a != null);
            PrintSelectedCardsCommand = new RelayCommand(PrintSelectedCards, () => HasSelectedItems);
            WriteNfcCommand = new RelayCommand<AbonadoSelectableViewModel>(WriteNfc, a => a != null);

            // Comandos múltiples
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => CanEdit && HasSelectedItems);
            ActivateSelectedCommand = new RelayCommand(ActivateSelected, () => CanEdit && HasSelectedItems);
            DeactivateSelectedCommand = new RelayCommand(DeactivateSelected, () => CanEdit && HasSelectedItems);
            MarkSelectedAsPrintedCommand = new RelayCommand(MarkSelectedAsPrinted, () => CanEdit && HasSelectedItems);
            MarkSelectedAsNotPrintedCommand = new RelayCommand(MarkSelectedAsNotPrinted, () => CanEdit && HasSelectedItems);
            ClearSelectionCommand = new RelayCommand(ClearSelection, () => HasSelectedItems);
        }

        #endregion

        #region Command Methods

        private void NewAbonado()
        {
            try
            {
                var editWindow = new AbonadoEditWindow();
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear abonado: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAbonado(AbonadoSelectableViewModel? abonadoVm)
        {
            if (abonadoVm?.Abonado == null) return;

            try
            {
                // Verificar si está impreso y mostrar advertencia
                if (abonadoVm.Abonado.Impreso)
                {
                    var result = MessageBox.Show(
                        $"El abonado {abonadoVm.Abonado.NombreCompleto} ya tiene su tarjeta impresa.\n\n" +
                        "Si modifica los datos, es recomendable reimprimir la tarjeta.\n\n" +
                        "¿Desea continuar con la edición?",
                        "Tarjeta Ya Impresa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                var editWindow = new AbonadoEditWindow(abonadoVm.Abonado);
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar abonado: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteAbonado(AbonadoSelectableViewModel? abonadoVm)
        {
            if (abonadoVm?.Abonado == null) return;

            var abonado = abonadoVm.Abonado;
            var result = MessageBox.Show(
                $"¿Está seguro de eliminar al abonado {abonado.NombreCompleto}?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Abonados.Remove(abonado);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminado abonado: {abonado.NombreCompleto}");
                    LoadData();

                    MessageBox.Show("Abonado eliminado correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar abonado: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ToggleEstado(AbonadoSelectableViewModel? abonadoVm)
        {
            if (abonadoVm?.Abonado == null) return;

            var abonado = abonadoVm.Abonado;
            try
            {
                var nuevoEstado = abonado.Estado == EstadoAbonado.Activo ? EstadoAbonado.Inactivo : EstadoAbonado.Activo;
                abonado.Estado = nuevoEstado;

                await _dbContext.SaveChangesAsync();
                await LogAction($"Cambio estado abonado {abonado.NombreCompleto}: {nuevoEstado}");

                // Refrescar la vista
                abonadoVm.NotifyPropertyChanged(nameof(abonadoVm.Abonado));
                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar estado: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MarkAsPrinted(AbonadoSelectableViewModel? abonadoVm)
        {
            if (abonadoVm?.Abonado == null) return;

            var abonado = abonadoVm.Abonado;
            try
            {
                abonado.Impreso = !abonado.Impreso;
                await _dbContext.SaveChangesAsync();
                await LogAction($"Marcado como {(abonado.Impreso ? "impreso" : "no impreso")}: {abonado.NombreCompleto}");

                // Refrescar la vista
                abonadoVm.NotifyPropertyChanged(nameof(abonadoVm.Abonado));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al marcar impresión: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFilters()
        {
            SearchText = "";
            // Seleccionar los primeros elementos (que son "Todos...")
            SelectedGestorFilter = _gestoresFilter.FirstOrDefault();
            SelectedPeñaFilter = _peñasFilter.FirstOrDefault();
            SelectedTipoAbonoFilter = _tiposAbonoFilter.FirstOrDefault();
            SelectedEstadoFilter = _estadosFilter.FirstOrDefault();
            SelectedImpresoFilter = _impresosFilter.FirstOrDefault();
        }

        private void ExportData()
        {
            MessageBox.Show("Funcionalidad de exportación - Próximamente", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintSelected()
        {
            if (SelectedAbonado?.Abonado == null) return;

            PrintCard(SelectedAbonado);
        }

        private void WriteNfc(AbonadoSelectableViewModel? abonadoVm)
        {
            if (abonadoVm?.Abonado == null) return;

            MessageBox.Show($"Funcionalidad NFC para {abonadoVm.Abonado.NombreCompleto} - Próximamente",
                          "NFC", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Multiple Selection Commands

        private async void DeleteSelected()
        {
            var selectedItems = AbonadosFiltered.Where(a => a.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar {selectedItems.Count} abonado(s) seleccionado(s)?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación Múltiple",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var abonados = selectedItems.Select(vm => vm.Abonado).ToList();
                    _dbContext.Abonados.RemoveRange(abonados);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminados {selectedItems.Count} abonados en lote");
                    LoadData();

                    MessageBox.Show($"{selectedItems.Count} abonado(s) eliminado(s) correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar abonados: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ActivateSelected()
        {
            var selectedItems = AbonadosFiltered.Where(a => a.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            try
            {
                foreach (var item in selectedItems)
                {
                    item.Abonado.Estado = EstadoAbonado.Activo;
                }

                await _dbContext.SaveChangesAsync();
                await LogAction($"Activados {selectedItems.Count} abonados en lote");

                // Refrescar vista
                foreach (var item in selectedItems)
                {
                    item.NotifyPropertyChanged(nameof(item.Abonado));
                }
                UpdateSubTitle();

                MessageBox.Show($"{selectedItems.Count} abonado(s) activado(s) correctamente.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al activar abonados: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeactivateSelected()
        {
            var selectedItems = AbonadosFiltered.Where(a => a.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            try
            {
                foreach (var item in selectedItems)
                {
                    item.Abonado.Estado = EstadoAbonado.Inactivo;
                }

                await _dbContext.SaveChangesAsync();
                await LogAction($"Desactivados {selectedItems.Count} abonados en lote");

                // Refrescar vista
                foreach (var item in selectedItems)
                {
                    item.NotifyPropertyChanged(nameof(item.Abonado));
                }
                UpdateSubTitle();

                MessageBox.Show($"{selectedItems.Count} abonado(s) desactivado(s) correctamente.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al desactivar abonados: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MarkSelectedAsPrinted()
        {
            var selectedItems = AbonadosFiltered.Where(a => a.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            try
            {
                foreach (var item in selectedItems)
                {
                    item.Abonado.Impreso = true;
                }

                await _dbContext.SaveChangesAsync();
                await LogAction($"Marcados como impresos {selectedItems.Count} abonados en lote");

                // Refrescar vista
                foreach (var item in selectedItems)
                {
                    item.NotifyPropertyChanged(nameof(item.Abonado));
                }

                MessageBox.Show($"{selectedItems.Count} abonado(s) marcado(s) como impresos.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al marcar como impresos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MarkSelectedAsNotPrinted()
        {
            var selectedItems = AbonadosFiltered.Where(a => a.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            try
            {
                foreach (var item in selectedItems)
                {
                    item.Abonado.Impreso = false;
                }

                await _dbContext.SaveChangesAsync();
                await LogAction($"Marcados como no impresos {selectedItems.Count} abonados en lote");

                // Refrescar vista
                foreach (var item in selectedItems)
                {
                    item.NotifyPropertyChanged(nameof(item.Abonado));
                }

                MessageBox.Show($"{selectedItems.Count} abonado(s) marcado(s) como no impresos.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al marcar como no impresos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearSelection()
        {
            foreach (var item in AbonadosFiltered)
            {
                item.IsSelected = false;
            }
            UpdateSelectionProperties();
        }

        #endregion

        #region Methods

        private void InitializeFilters()
        {
            // Inicializar filtros de estado
            _estadosFilter.Add(new EstadoFilterItem { Value = null, Texto = "Todos los estados" });
            _estadosFilter.Add(new EstadoFilterItem { Value = EstadoAbonado.Activo, Texto = "Activos" });
            _estadosFilter.Add(new EstadoFilterItem { Value = EstadoAbonado.Inactivo, Texto = "Inactivos" });

            // Inicializar filtros de impreso
            _impresosFilter.Add(new ImpresoFilterItem { Value = null, Texto = "Todos" });
            _impresosFilter.Add(new ImpresoFilterItem { Value = true, Texto = "Impresos" });
            _impresosFilter.Add(new ImpresoFilterItem { Value = false, Texto = "No impresos" });
        }

        private async void LoadData()
        {
            try
            {
                var abonados = await _dbContext.Abonados
                    .Include(a => a.Gestor)
                    .Include(a => a.Peña)
                    .Include(a => a.TipoAbono)
                    .OrderBy(a => a.NumeroSocio)
                    .ToListAsync();

                _abonados.Clear();
                foreach (var abonado in abonados)
                {
                    var vm = new AbonadoSelectableViewModel(abonado);
                    vm.SelectionChanged += OnItemSelectionChanged;
                    _abonados.Add(vm);
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar abonados: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadFilters()
        {
            try
            {
                // Cargar gestores
                var gestores = await _dbContext.Gestores.OrderBy(g => g.Nombre).ToListAsync();
                _gestoresFilter.Clear();
                _gestoresFilter.Add(new Gestor { Id = 0, Nombre = "Todos los gestores" });
                foreach (var gestor in gestores)
                {
                    _gestoresFilter.Add(gestor);
                }

                // Cargar peñas
                var peñas = await _dbContext.Peñas.OrderBy(p => p.Nombre).ToListAsync();
                _peñasFilter.Clear();
                _peñasFilter.Add(new Peña { Id = 0, Nombre = "Todas las peñas" });
                foreach (var peña in peñas)
                {
                    _peñasFilter.Add(peña);
                }

                // Cargar tipos de abono
                var tipos = await _dbContext.TiposAbono.OrderBy(t => t.Nombre).ToListAsync();
                _tiposAbonoFilter.Clear();
                _tiposAbonoFilter.Add(new TipoAbono { Id = 0, Nombre = "Todos los tipos" });
                foreach (var tipo in tipos)
                {
                    _tiposAbonoFilter.Add(tipo);
                }

                // Establecer selecciones por defecto después de cargar
                SelectedGestorFilter = _gestoresFilter.FirstOrDefault();
                SelectedPeñaFilter = _peñasFilter.FirstOrDefault();
                SelectedTipoAbonoFilter = _tiposAbonoFilter.FirstOrDefault();
                SelectedEstadoFilter = _estadosFilter.FirstOrDefault();
                SelectedImpresoFilter = _impresosFilter.FirstOrDefault();
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
                var filtered = _abonados.AsEnumerable();

                // Filtro por texto
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(a =>
                        a.Abonado.Nombre.ToLower().Contains(searchLower) ||
                        a.Abonado.Apellidos.ToLower().Contains(searchLower) ||
                        (a.Abonado.DNI != null && a.Abonado.DNI.ToLower().Contains(searchLower)) ||
                        a.Abonado.NumeroSocio.ToString().Contains(searchLower));
                }

                // Filtros por selección
                if (SelectedGestorFilter != null && SelectedGestorFilter.Id > 0)
                    filtered = filtered.Where(a => a.Abonado.GestorId == SelectedGestorFilter.Id);

                if (SelectedPeñaFilter != null && SelectedPeñaFilter.Id > 0)
                    filtered = filtered.Where(a => a.Abonado.PeñaId == SelectedPeñaFilter.Id);

                if (SelectedTipoAbonoFilter != null && SelectedTipoAbonoFilter.Id > 0)
                    filtered = filtered.Where(a => a.Abonado.TipoAbonoId == SelectedTipoAbonoFilter.Id);

                if (SelectedEstadoFilter != null && SelectedEstadoFilter.Value.HasValue)
                    filtered = filtered.Where(a => a.Abonado.Estado == SelectedEstadoFilter.Value.Value);

                if (SelectedImpresoFilter != null && SelectedImpresoFilter.Value.HasValue)
                    filtered = filtered.Where(a => a.Abonado.Impreso == SelectedImpresoFilter.Value.Value);

                AbonadosFiltered.Clear();
                foreach (var abonado in filtered)
                {
                    AbonadosFiltered.Add(abonado);
                }

                UpdateSubTitle();
                UpdateSelectionProperties();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando filtros: {ex.Message}");
            }
        }

        private void UpdateSubTitle()
        {
            var total = AbonadosFiltered.Count;
            var activos = AbonadosFiltered.Count(a => a.Abonado.Estado == EstadoAbonado.Activo);
            var impresos = AbonadosFiltered.Count(a => a.Abonado.Impreso);
            var seleccionados = SelectedCount;

            if (seleccionados > 0)
            {
                SubTitle = $"{total} abonados encontrados ({activos} activos, {impresos} impresos) - {seleccionados} seleccionados";
            }
            else
            {
                SubTitle = $"{total} abonados encontrados ({activos} activos, {impresos} impresos)";
            }
        }

        private void UpdateCanEdit()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            var permissions = UserSession.Instance.CurrentPermissions;
            CanEdit = licenseInfo.IsValid && !licenseInfo.IsExpired &&
                     permissions?.CanEditAbonados == true;
        }

        private async Task LogAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "Abonados",
                    FechaHora = DateTime.Now
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch { }
        }

        private void UpdateAllSelection()
        {
            // Temporalmente desconectar los eventos para evitar bucles
            foreach (var item in AbonadosFiltered)
            {
                item.SelectionChanged -= OnItemSelectionChanged;
                item.IsSelected = SelectAll;
                item.SelectionChanged += OnItemSelectionChanged;
            }
            UpdateSelectionProperties();
        }

        private void OnItemSelectionChanged()
        {
            UpdateSelectionProperties();
        }

        private void UpdateSelectionProperties()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedItems));

            // Actualizar SelectAll - se marca si TODOS los elementos están seleccionados
            var allSelected = AbonadosFiltered.Any() && AbonadosFiltered.All(a => a.IsSelected);

            // Solo actualizar si el valor realmente cambió para evitar bucles infinitos
            if (_selectAll != allSelected)
            {
                _selectAll = allSelected;
                OnPropertyChanged(nameof(SelectAll));
            }

            UpdateSubTitle();
        }

        #endregion

        #region Métodos de Impresión

        private async void PrintCard(AbonadoSelectableViewModel? abonadoVm)
        {
            if (abonadoVm?.Abonado == null) return;

            try
            {
                // Cargar plantilla predeterminada
                var templateService = new TemplateService();
                var plantilla = await templateService.GetPlantillaPredeterminadaAsync();

                if (plantilla == null)
                {
                    MessageBox.Show("No hay plantilla predeterminada configurada. Abra el diseñador para crear una.",
                                  "Sin plantilla", MessageBoxButton.OK, MessageBoxImage.Information);
                    OpenCardDesigner();
                    return;
                }

                // Crear servicio de impresión
                var printService = new CardPrintService(templateService, _configuracionService);

                // Obtener configuración actual
                var configuracion = _configuracionService.GetConfiguracion();

                // Crear configuración específica de impresión
                var configuracionImpresion = new ConfiguracionImpresionEspecifica
                {
                    NombreImpresora = configuracion.ConfiguracionImpresion.ImpresoraPredeterminada,
                    TamañoPapel = configuracion.ConfiguracionImpresion.TamañoPapel,
                    ImpresionColor = configuracion.ConfiguracionImpresion.ImpresionColor,
                    Calidad = configuracion.ConfiguracionImpresion.Calidad,
                    Copias = 1,
                    MostrarDialogoImpresion = configuracion.ConfiguracionImpresion.MostrarVistaPrevia,
                    GuardarCopia = configuracion.ConfiguracionImpresion.GuardarCopiaDigital,
                    RutaCopia = configuracion.ConfiguracionImpresion.RutaCopiasDigitales
                };

                // Imprimir
                var resultado = await printService.ImprimirTarjetaAsync(abonadoVm.Abonado, plantilla, configuracionImpresion);

                if (resultado.Exitoso)
                {
                    // Marcar como impreso solo si está configurado
                    if (configuracion.ConfiguracionImpresion.MarcarComoImpresoAutomaticamente)
                    {
                        abonadoVm.Abonado.Impreso = true;
                        await _dbContext.SaveChangesAsync();
                        abonadoVm.NotifyPropertyChanged(nameof(abonadoVm.Abonado));
                    }

                    await LogAction($"Impresa tarjeta de {abonadoVm.Abonado.NombreCompleto}");

                    MessageBox.Show($"✅ Tarjeta impresa correctamente!\n\n" +
                                   $"• Abonado: {abonadoVm.Abonado.NombreCompleto}\n" +
                                   $"• Tiempo: {resultado.TiempoTranscurrido.TotalSeconds:F1} segundos",
                                   "Impresión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var errores = string.Join("\n• ", resultado.Errores);
                    MessageBox.Show($"❌ Error al imprimir tarjeta:\n\n• {errores}",
                                   "Error en Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir tarjeta: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PrintSelectedCards()
        {
            var selectedItems = AbonadosFiltered.Where(a => a.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            try
            {
                var templateService = new TemplateService();
                var plantilla = await templateService.GetPlantillaPredeterminadaAsync();

                if (plantilla == null)
                {
                    MessageBox.Show("No hay plantilla predeterminada configurada. Abra el diseñador para crear una.",
                                  "Sin plantilla", MessageBoxButton.OK, MessageBoxImage.Information);
                    OpenCardDesigner();
                    return;
                }

                var printService = new CardPrintService(templateService, _configuracionService);
                var abonados = selectedItems.Select(vm => vm.Abonado).ToList();

                var result = MessageBox.Show(
                    $"¿Desea imprimir {selectedItems.Count} tarjetas?",
                    "Confirmar Impresión",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var exitoso = await printService.ImprimirTarjetasAsync(abonados, plantilla);

                    if (exitoso)
                    {
                        await LogAction($"Impresas {selectedItems.Count} tarjetas en lote");

                        // Actualizar estado de impreso si está configurado
                        var configuracion = _configuracionService.GetConfiguracion();
                        if (configuracion.ConfiguracionImpresion.MarcarComoImpresoAutomaticamente)
                        {
                            foreach (var item in selectedItems)
                            {
                                item.Abonado.Impreso = true;
                                item.NotifyPropertyChanged(nameof(item.Abonado));
                            }
                            await _dbContext.SaveChangesAsync();
                        }

                        MessageBox.Show($"{selectedItems.Count} tarjetas enviadas a impresora correctamente.",
                                      "Impresión completada", MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadData(); // Refrescar vista
                    }
                    else
                    {
                        MessageBox.Show("Error durante la impresión de las tarjetas.", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir tarjetas: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCardDesigner()
        {
            try
            {
                var designerWindow = new CardDesignerWindow();
                designerWindow.Owner = Application.Current.MainWindow;
                designerWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir diseñador de tarjetas: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    // ViewModel para elementos seleccionables
    public class AbonadoSelectableViewModel : BaseViewModel
    {
        private bool _isSelected;
        public Abonado Abonado { get; }

        public AbonadoSelectableViewModel(Abonado abonado)
        {
            Abonado = abonado;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    SelectionChanged?.Invoke();
                }
            }
        }

        public event Action? SelectionChanged;

        // Método público para notificar cambios en propiedades
        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }

    // Clases auxiliares para los filtros
    public class EstadoFilterItem
    {
        public EstadoAbonado? Value { get; set; }
        public string Texto { get; set; } = "";

        public override string ToString()
        {
            return Texto;
        }
    }

    public class ImpresoFilterItem
    {
        public bool? Value { get; set; }
        public string Texto { get; set; } = "";

        public override string ToString()
        {
            return Texto;
        }
    }
}