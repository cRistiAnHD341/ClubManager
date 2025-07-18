using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClubManager.ViewModels
{
    public class AbonadosViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<Abonado> _abonados;
        private ObservableCollection<Abonado> _abonadosFiltered;
        private Abonado? _selectedAbonado;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;
        private bool _hasSelectedAbonado = false;

        // Filtros
        private List<FilterItem> _estadoFilter;
        private int? _selectedEstadoFilter;
        private ObservableCollection<Peña> _peñasFilter;
        private int? _selectedPeñaFilter;
        private ObservableCollection<TipoAbono> _tiposAbonoFilter;
        private int? _selectedTipoAbonoFilter;

        public AbonadosViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            _abonados = new ObservableCollection<Abonado>();
            _abonadosFiltered = new ObservableCollection<Abonado>();
            _estadoFilter = new List<FilterItem>();
            _peñasFilter = new ObservableCollection<Peña>();
            _tiposAbonoFilter = new ObservableCollection<TipoAbono>();

            InitializeCommands();
            InitializeFilters();
            LoadData();
            UpdateCanEdit();
        }

        #region Properties

        public ObservableCollection<Abonado> AbonadosFiltered
        {
            get => _abonadosFiltered;
            set { _abonadosFiltered = value; OnPropertyChanged(); }
        }

        public Abonado? SelectedAbonado
        {
            get => _selectedAbonado;
            set
            {
                _selectedAbonado = value;
                OnPropertyChanged();
                HasSelectedAbonado = value != null;
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string SubTitle
        {
            get => _subTitle;
            set { _subTitle = value; OnPropertyChanged(); }
        }

        public bool CanEdit
        {
            get => _canEdit;
            set { _canEdit = value; OnPropertyChanged(); }
        }

        public bool HasSelectedAbonado
        {
            get => _hasSelectedAbonado;
            set { _hasSelectedAbonado = value; OnPropertyChanged(); }
        }

        // Filtros
        public List<FilterItem> EstadoFilter
        {
            get => _estadoFilter;
            set { _estadoFilter = value; OnPropertyChanged(); }
        }

        public int? SelectedEstadoFilter
        {
            get => _selectedEstadoFilter;
            set
            {
                _selectedEstadoFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public ObservableCollection<Peña> PeñasFilter
        {
            get => _peñasFilter;
            set { _peñasFilter = value; OnPropertyChanged(); }
        }

        public int? SelectedPeñaFilter
        {
            get => _selectedPeñaFilter;
            set
            {
                _selectedPeñaFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public ObservableCollection<TipoAbono> TiposAbonoFilter
        {
            get => _tiposAbonoFilter;
            set { _tiposAbonoFilter = value; OnPropertyChanged(); }
        }

        public int? SelectedTipoAbonoFilter
        {
            get => _selectedTipoAbonoFilter;
            set
            {
                _selectedTipoAbonoFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        #endregion

        #region Commands

        public ICommand NewAbonadoCommand { get; private set; }
        public ICommand EditAbonadoCommand { get; private set; }
        public ICommand DeleteAbonadoCommand { get; private set; }
        public ICommand MarkAsPrintedCommand { get; private set; }
        public ICommand ShowStatsCommand { get; private set; }
        public ICommand PrintCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            NewAbonadoCommand = new RelayCommand(NewAbonado);
            EditAbonadoCommand = new RelayCommand<Abonado>(EditAbonado);
            DeleteAbonadoCommand = new RelayCommand<Abonado>(DeleteAbonado);
            MarkAsPrintedCommand = new RelayCommand<Abonado>(MarkAsPrinted);
            ShowStatsCommand = new RelayCommand(ShowStats);
            PrintCommand = new RelayCommand(PrintSelected);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
        }

        private void InitializeFilters()
        {
            EstadoFilter = new List<FilterItem>
            {
                new FilterItem { Display = "Todos", Value = null },
                new FilterItem { Display = "Activos", Value = (int)EstadoAbonado.Activo },
                new FilterItem { Display = "Inactivos", Value = (int)EstadoAbonado.Inactivo }
            };
        }

        private async void LoadData()
        {
            try
            {
                // Cargar abonados con sus relaciones
                var abonados = await _dbContext.Abonados
                    .Include(a => a.Gestor)
                    .Include(a => a.Peña)
                    .Include(a => a.TipoAbono)
                    .OrderBy(a => a.NumeroSocio)
                    .ToListAsync();

                _abonados.Clear();
                foreach (var abonado in abonados)
                {
                    _abonados.Add(abonado);
                }

                // Cargar filtros
                await LoadFilters();

                ApplyFilters();
                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadFilters()
        {
            try
            {
                // Cargar peñas para filtro
                var peñas = await _dbContext.Peñas.OrderBy(p => p.Nombre).ToListAsync();
                PeñasFilter.Clear();
                PeñasFilter.Add(new Peña { Id = 0, Nombre = "Todas las peñas" });
                foreach (var peña in peñas)
                {
                    PeñasFilter.Add(peña);
                }

                // Cargar tipos de abono para filtro
                var tiposAbono = await _dbContext.TiposAbono.OrderBy(t => t.Nombre).ToListAsync();
                TiposAbonoFilter.Clear();
                TiposAbonoFilter.Add(new TipoAbono { Id = 0, Nombre = "Todos los tipos" });
                foreach (var tipo in tiposAbono)
                {
                    TiposAbonoFilter.Add(tipo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar filtros: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCanEdit()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            CanEdit = licenseInfo.IsValid && !licenseInfo.IsExpired;
        }

        #endregion

        #region Filtering

        private void ApplyFilters()
        {
            try
            {
                var filtered = _abonados.AsEnumerable();

                // Filtro por texto de búsqueda
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(a =>
                        a.Nombre.ToLower().Contains(searchLower) ||
                        a.Apellidos.ToLower().Contains(searchLower) ||
                        a.DNI.ToLower().Contains(searchLower) ||
                        a.NumeroSocio.ToString().Contains(searchLower));
                }

                // Filtro por estado
                if (SelectedEstadoFilter.HasValue)
                {
                    filtered = filtered.Where(a => (int)a.Estado == SelectedEstadoFilter.Value);
                }

                // Filtro por peña
                if (SelectedPeñaFilter.HasValue && SelectedPeñaFilter.Value > 0)
                {
                    filtered = filtered.Where(a => a.PeñaId == SelectedPeñaFilter.Value);
                }

                // Filtro por tipo de abono
                if (SelectedTipoAbonoFilter.HasValue && SelectedTipoAbonoFilter.Value > 0)
                {
                    filtered = filtered.Where(a => a.TipoAbonoId == SelectedTipoAbonoFilter.Value);
                }

                AbonadosFiltered.Clear();
                foreach (var abonado in filtered)
                {
                    AbonadosFiltered.Add(abonado);
                }

                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSubTitle()
        {
            var total = _abonados.Count;
            var filtered = AbonadosFiltered.Count;
            var activos = AbonadosFiltered.Count(a => a.Estado == EstadoAbonado.Activo);

            if (total == filtered)
            {
                SubTitle = $"Total: {total} abonados • Activos: {activos}";
            }
            else
            {
                SubTitle = $"Mostrando: {filtered} de {total} abonados • Activos: {activos}";
            }
        }

        #endregion

        #region Command Methods

        private void NewAbonado()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para crear abonados. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new Views.AbonadoEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditAbonado(Abonado? abonado)
        {
            if (abonado == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para editar abonados. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            if (abonado.Impreso)
            {
                var result = MessageBox.Show(
                    $"El abonado '{abonado.NombreCompleto}' ya está marcado como impreso.\n\n" +
                    "¿Estás seguro de que quieres modificar sus datos?",
                    "Abonado Impreso",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            var editWindow = new Views.AbonadoEditWindow(abonado);
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private async void DeleteAbonado(Abonado? abonado)
        {
            if (abonado == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para eliminar abonados. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var result = MessageBox.Show(
                $"¿Estás seguro de que quieres eliminar al abonado '{abonado.NombreCompleto}'?\n\n" +
                "Esta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Abonados.Remove(abonado);
                    await _dbContext.SaveChangesAsync();

                    // Registrar en historial
                    await LogAction($"Eliminó abonado: {abonado.NombreCompleto} (DNI: {abonado.DNI})");

                    LoadData();

                    MessageBox.Show("Abonado eliminado correctamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar el abonado: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void MarkAsPrinted(Abonado? abonado)
        {
            if (abonado == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para marcar como impreso. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            try
            {
                abonado.Impreso = !abonado.Impreso;
                await _dbContext.SaveChangesAsync();

                var accion = abonado.Impreso ? "marcó como impreso" : "desmarcó como impreso";
                await LogAction($"{accion} abonado: {abonado.NombreCompleto}");

                ApplyFilters(); // Actualizar vista sin recargar todo

                var mensaje = abonado.Impreso ? "marcado como impreso" : "desmarcado como impreso";
                MessageBox.Show($"Abonado {mensaje} correctamente.", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar el estado: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowStats()
        {
            var statsWindow = new StatsWindow(_abonados.ToList());
            statsWindow.ShowDialog();
        }

        private void PrintSelected()
        {
            if (SelectedAbonado == null)
            {
                MessageBox.Show("Selecciona un abonado para imprimir.", "Información",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Por ahora solo marcar como impreso
            MarkAsPrinted(SelectedAbonado);
        }

        private void ClearFilters()
        {
            SearchText = "";
            SelectedEstadoFilter = null;
            SelectedPeñaFilter = null;
            SelectedTipoAbonoFilter = null;
        }

        private void ExportData()
        {
            try
            {
                var exportWindow = new Views.ExportWindow(AbonadosFiltered.ToList());
                exportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

    #region Helper Classes

    public class FilterItem
    {
        public string Display { get; set; } = "";
        public int? Value { get; set; }
    }

    // Placeholder windows - se implementarán después
    public class StatsWindow : Window
    {
        public StatsWindow(List<Abonado> abonados)
        {
            Title = "Estadísticas";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = new TextBlock
            {
                Text = $"Estadísticas de {abonados.Count} abonados - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    public class ExportWindow : Window
    {
        public ExportWindow(List<Abonado> abonados)
        {
            Title = "Exportar Datos";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = new System.Windows.Controls.TextBlock
            {
                Text = $"Exportar {abonados.Count} abonados - Cargando...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    #endregion
}