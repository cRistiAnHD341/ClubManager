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
    public class GestoresViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<GestorExtendido> _gestores;
        private ObservableCollection<GestorExtendido> _gestoresFiltered;
        private GestorExtendido? _selectedGestor;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;

        public GestoresViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();

            _gestores = new ObservableCollection<GestorExtendido>();
            _gestoresFiltered = new ObservableCollection<GestorExtendido>();

            InitializeCommands();
            LoadData();
            UpdateCanEdit();
        }

        #region Properties

        public ObservableCollection<GestorExtendido> GestoresFiltered
        {
            get => _gestoresFiltered;
            set => SetProperty(ref _gestoresFiltered, value);
        }

        public GestorExtendido? SelectedGestor
        {
            get => _selectedGestor;
            set => SetProperty(ref _selectedGestor, value);
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

        public ICommand NewGestorCommand { get; private set; } = null!;
        public ICommand EditGestorCommand { get; private set; } = null!;
        public ICommand DeleteGestorCommand { get; private set; } = null!;
        public ICommand ViewAbonadosCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;
        public ICommand ExportCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            NewGestorCommand = new RelayCommand(NewGestor, () => CanEdit);
            EditGestorCommand = new RelayCommand<GestorExtendido>(EditGestor, g => CanEdit && g != null);
            DeleteGestorCommand = new RelayCommand<GestorExtendido>(DeleteGestor, g => CanEdit && g != null);
            ViewAbonadosCommand = new RelayCommand<GestorExtendido>(ViewAbonados, g => g != null);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
        }

        #endregion

        #region Command Methods

        private void NewGestor()
        {
            try
            {
                var editWindow = new EditarGestorWindow();
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear gestor: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditGestor(GestorExtendido? gestor)
        {
            if (gestor?.Gestor == null) return;

            try
            {
                var editWindow = new EditarGestorWindow(gestor.Gestor);
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar gestor: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteGestor(GestorExtendido? gestor)
        {
            if (gestor?.Gestor == null) return;

            if (gestor.TotalAbonados > 0)
            {
                MessageBox.Show($"No se puede eliminar el gestor '{gestor.Nombre}' porque tiene {gestor.TotalAbonados} abonados asignados.\n\nPrimero reasigne o elimine los abonados.",
                              "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar el gestor '{gestor.Nombre}'?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Gestores.Remove(gestor.Gestor);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminado gestor: {gestor.Nombre}");
                    LoadData();

                    MessageBox.Show("Gestor eliminado correctamente.", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar gestor: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewAbonados(GestorExtendido? gestor)
        {
            if (gestor == null) return;

            MessageBox.Show($"Ver abonados del gestor '{gestor.Nombre}' - Funcionalidad próximamente",
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
                var gestores = await _dbContext.Gestores
                    .Include(g => g.Abonados)
                    .OrderBy(g => g.Nombre)
                    .ToListAsync();

                _gestores.Clear();
                foreach (var gestor in gestores)
                {
                    var gestorExtendido = new GestorExtendido(gestor);
                    _gestores.Add(gestorExtendido);
                }

                ApplyFilters();
                UpdateSubTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar gestores: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _gestores.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(g => g.Nombre.ToLower().Contains(searchLower));
                }

                GestoresFiltered.Clear();
                foreach (var gestor in filtered)
                {
                    GestoresFiltered.Add(gestor);
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
            var total = GestoresFiltered.Count;
            var conAbonados = GestoresFiltered.Count(g => g.TotalAbonados > 0);
            var totalAbonados = GestoresFiltered.Sum(g => g.TotalAbonados);
            SubTitle = $"{total} gestores ({conAbonados} con abonados) - Total: {totalAbonados} abonados";
        }

        private void UpdateCanEdit()
        {
            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();
            var permissions = UserSession.Instance.CurrentPermissions;
            CanEdit = licenseInfo.IsValid && !licenseInfo.IsExpired &&
                     permissions?.CanAccessGestores == true;
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
            catch { }
        }

        #endregion
    }

    // Clase extendida para mostrar información adicional
    public class GestorExtendido : BaseViewModel
    {
        public Gestor Gestor { get; }

        public GestorExtendido(Gestor gestor)
        {
            Gestor = gestor;
            CalculateExtendedProperties();
        }

        private void CalculateExtendedProperties()
        {
            TotalAbonados = Gestor.Abonados?.Count ?? 0;
            AbonadosActivos = Gestor.Abonados?.Count(a => a.Estado == EstadoAbonado.Activo) ?? 0;
            EstadoTexto = TotalAbonados > 0 ? "Activo" : "Sin abonados";
            ColorEstado = TotalAbonados > 0 ? "#4CAF50" : "#F44336";
        }

        // Propiedades del gestor
        public int Id => Gestor.Id;
        public string Nombre => Gestor.Nombre;
        public DateTime FechaCreacion => Gestor.FechaCreacion;

        // Propiedades calculadas
        public int TotalAbonados { get; private set; }
        public int AbonadosActivos { get; private set; }
        public string EstadoTexto { get; private set; } = "";
        public string ColorEstado { get; private set; } = "";
        public string EstadisticaTexto => $"{AbonadosActivos}/{TotalAbonados} activos";
    }
}