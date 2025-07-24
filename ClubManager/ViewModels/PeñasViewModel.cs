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
    public class PeñasViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<PeñaExtendida> _peñas;
        private ObservableCollection<PeñaExtendida> _peñasFiltered;
        private PeñaExtendida? _selectedPeña;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;

        public PeñasViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            _peñas = new ObservableCollection<PeñaExtendida>();
            _peñasFiltered = new ObservableCollection<PeñaExtendida>();

            InitializeCommands();
            LoadData();
            UpdateCanEdit();
        }

        #region Properties

        public ObservableCollection<PeñaExtendida> PeñasFiltered
        {
            get => _peñasFiltered;
            set => SetProperty(ref _peñasFiltered, value);
        }

        public PeñaExtendida? SelectedPeña
        {
            get => _selectedPeña;
            set => SetProperty(ref _selectedPeña, value);
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

        public ICommand NewPeñaCommand { get; private set; } = null!;
        public ICommand EditPeñaCommand { get; private set; } = null!;
        public ICommand DeletePeñaCommand { get; private set; } = null!;
        public ICommand ViewAbonadosCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;
        public ICommand ExportCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NewPeñaCommand = new RelayCommand(NewPeña, () => CanEdit);
            EditPeñaCommand = new RelayCommand<PeñaExtendida>(EditPeña, p => CanEdit && p != null);
            DeletePeñaCommand = new RelayCommand<PeñaExtendida>(DeletePeña, p => CanEdit && p != null);
            ViewAbonadosCommand = new RelayCommand<PeñaExtendida>(ViewAbonados, p => p != null);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
        }

        #endregion

        #region Command Methods

        private void NewPeña()
        {
            try
            {
                var editWindow = new EditarPeñaWindow();
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear peña: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditPeña(PeñaExtendida? peña)
        {
            if (peña?.Peña == null) return;

            try
            {
                var editWindow = new EditarPeñaWindow(peña.Peña);
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar peña: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeletePeña(PeñaExtendida? peña)
        {
            if (peña?.Peña == null) return;

            if (peña.TotalAbonados > 0)
            {
                MessageBox.Show($"No se puede eliminar la peña '{peña.Nombre}' porque tiene {peña.TotalAbonados} abonados asignados.\n\nPrimero reasigne o elimine los abonados.",
                              "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar la peña '{peña.Nombre}'?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Peñas.Remove(peña.Peña);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminada peña: {peña.Nombre}");
                    LoadData();

                    MessageBox.Show("Peña eliminada correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar peña: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewAbonados(PeñaExtendida? peña)
        {
            if (peña == null) return;

            MessageBox.Show($"Ver abonados de la peña '{peña.Nombre}' - Funcionalidad próximamente",
                          "Información", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void LoadData()
        {
            try
            {
                var peñas = await _dbContext.Peñas
                    .Include(p => p.Abonados)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                _peñas.Clear();
                foreach (var peña in peñas)
                {
                    var peñaExtendida = new PeñaExtendida(peña);
                    _peñas.Add(peñaExtendida);
                }

                ApplyFilters();
                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar peñas: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _peñas.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(p => p.Nombre.ToLower().Contains(searchLower));
                }

                PeñasFiltered.Clear();
                foreach (var peña in filtered)
                {
                    PeñasFiltered.Add(peña);
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
            var total = PeñasFiltered.Count;
            var conAbonados = PeñasFiltered.Count(p => p.TotalAbonados > 0);
            var totalAbonados = PeñasFiltered.Sum(p => p.TotalAbonados);
            SubTitle = $"{total} peñas ({conAbonados} con abonados) - Total: {totalAbonados} abonados";
        }

        private void UpdateCanEdit()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            var permissions = UserSession.Instance.CurrentPermissions;
            CanEdit = licenseInfo.IsValid && !licenseInfo.IsExpired &&
                     permissions?.CanAccessPeñas == true;
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
            catch { }
        }

        #endregion
    }

    // Clase extendida para mostrar información adicional
    public class PeñaExtendida : BaseViewModel
    {
        public Peña Peña { get; }

        public PeñaExtendida(Peña peña)
        {
            Peña = peña;
            CalculateExtendedProperties();
        }

        private void CalculateExtendedProperties()
        {
            TotalAbonados = Peña.Abonados?.Count ?? 0;
            AbonadosActivos = Peña.Abonados?.Count(a => a.Estado == EstadoAbonado.Activo) ?? 0;
            EstadoTexto = TotalAbonados > 0 ? "Activa" : "Sin abonados";
            ColorEstado = TotalAbonados > 0 ? "#4CAF50" : "#F44336";
        }

        // Propiedades de la peña
        public int Id => Peña.Id;
        public string Nombre => Peña.Nombre;

        // Propiedades calculadas
        public int TotalAbonados { get; private set; }
        public int AbonadosActivos { get; private set; }
        public string EstadoTexto { get; private set; } = "";
        public string ColorEstado { get; private set; } = "";
        public string EstadisticaTexto => $"{AbonadosActivos}/{TotalAbonados} activos";
        public double PorcentajeActivos => TotalAbonados > 0 ? (double)AbonadosActivos / TotalAbonados * 100 : 0;
    }
}