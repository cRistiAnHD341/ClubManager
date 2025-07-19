using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager.ViewModels
{
    public class GestoresViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<GestorExtendido> _gestores;
        private ObservableCollection<GestorExtendido> _gestoresFiltered;
        private GestorExtendido? _selectedGestor;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;
        private bool _hasSelectedGestor = false;

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
            set { _gestoresFiltered = value; OnPropertyChanged(); }
        }

        public GestorExtendido? SelectedGestor
        {
            get => _selectedGestor;
            set
            {
                _selectedGestor = value;
                OnPropertyChanged();
                HasSelectedGestor = value != null;
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

        public bool HasSelectedGestor
        {
            get => _hasSelectedGestor;
            set { _hasSelectedGestor = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand NewGestorCommand { get; private set; }
        public ICommand EditGestorCommand { get; private set; }
        public ICommand DeleteGestorCommand { get; private set; }
        public ICommand ViewAbonadosCommand { get; private set; }
        public ICommand ShowStatsCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            NewGestorCommand = new RelayCommand(NewGestor);
            EditGestorCommand = new RelayCommand<GestorExtendido>(EditGestor);
            DeleteGestorCommand = new RelayCommand<GestorExtendido>(DeleteGestor);
            ViewAbonadosCommand = new RelayCommand<GestorExtendido>(ViewAbonados);
            ShowStatsCommand = new RelayCommand(ShowStats);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
        }

        private async void LoadData()
        {
            try
            {
                var gestores = await _dbContext.Gestores
                    .Include(g => g.Abonados)
                        .ThenInclude(a => a.TipoAbono)
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
                var filtered = _gestores.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(g =>
                        g.Nombre.ToLower().Contains(searchLower));
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
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSubTitle()
        {
            var total = _gestores.Count;
            var filtered = GestoresFiltered.Count;
            var totalAbonados = GestoresFiltered.Sum(g => g.TotalAbonados);

            if (total == filtered)
            {
                SubTitle = $"Total: {total} gestores • Gestionan: {totalAbonados} abonados";
            }
            else
            {
                SubTitle = $"Mostrando: {filtered} de {total} gestores • Gestionan: {totalAbonados} abonados";
            }
        }

        #endregion

        #region Command Methods

        private void NewGestor()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para crear gestores. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new GestorEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditGestor(GestorExtendido? gestor)
        {
            if (gestor == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para editar gestores. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var editWindow = new GestorEditWindow(gestor.Gestor);
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private async void DeleteGestor(GestorExtendido? gestor)
        {
            if (gestor == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para eliminar gestores. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            if (gestor.TotalAbonados > 0)
            {
                MessageBox.Show($"No se puede eliminar el gestor '{gestor.Nombre}' porque tiene {gestor.TotalAbonados} abonados asignados.\n\n" +
                               "Primero reasigna o elimina los abonados.",
                               "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Estás seguro de que quieres eliminar al gestor '{gestor.Nombre}'?\n\n" +
                "Esta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Gestores.Remove(gestor.Gestor);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminó gestor: {gestor.Nombre}");

                    LoadData();

                    MessageBox.Show("Gestor eliminado correctamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar el gestor: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewAbonados(GestorExtendido? gestor)
        {
            if (gestor == null) return;

            // Aquí podrías abrir una ventana que muestre los abonados de este gestor
            MessageBox.Show($"Ver abonados del gestor '{gestor.Nombre}'\n\n" +
                           $"Total: {gestor.TotalAbonados} abonados\n" +
                           $"Activos: {gestor.AbonadosActivos}\n" +
                           $"Ingresos: {gestor.IngresosEstimados:C}",
                           "Abonados del Gestor",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowStats()
        {
            var totalGestores = _gestores.Count;
            var totalAbonados = _gestores.Sum(g => g.TotalAbonados);
            var totalActivos = _gestores.Sum(g => g.AbonadosActivos);
            var totalIngresos = _gestores.Sum(g => g.IngresosEstimados);

            var gestorMasAbonados = _gestores.OrderByDescending(g => g.TotalAbonados).FirstOrDefault();

            var stats = $"📊 ESTADÍSTICAS DE GESTORES\n\n" +
                       $"Total gestores: {totalGestores}\n" +
                       $"Total abonados gestionados: {totalAbonados}\n" +
                       $"Abonados activos: {totalActivos}\n" +
                       $"Ingresos totales: {totalIngresos:C}\n\n";

            if (gestorMasAbonados != null)
            {
                stats += $"🏆 Gestor con más abonados:\n" +
                         $"{gestorMasAbonados.Nombre} ({gestorMasAbonados.TotalAbonados} abonados)";
            }

            MessageBox.Show(stats, "Estadísticas de Gestores",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearFilters()
        {
            SearchText = "";
        }

        private void ExportData()
        {
            try
            {
                var exportWindow = new Views.ExportWindow();
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

    public class GestorExtendido : INotifyPropertyChanged
    {
        public Gestor Gestor { get; }

        public GestorExtendido(Gestor gestor)
        {
            Gestor = gestor;
            CalculateProperties();
        }

        private void CalculateProperties()
        {
            TotalAbonados = Gestor.Abonados?.Count ?? 0;
            AbonadosActivos = Gestor.Abonados?.Count(a => a.Estado == EstadoAbonado.Activo) ?? 0;
            IngresosEstimados = Gestor.Abonados?
                .Where(a => a.Estado == EstadoAbonado.Activo && a.TipoAbono != null)
                .Sum(a => a.TipoAbono!.Precio) ?? 0;
        }

        // Propiedades delegadas
        public int Id => Gestor.Id;
        public string Nombre => Gestor.Nombre;
        public DateTime FechaCreacion => Gestor.FechaCreacion;

        // Propiedades calculadas
        public int TotalAbonados { get; private set; }
        public int AbonadosActivos { get; private set; }
        public decimal IngresosEstimados { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Placeholder window
    public class GestorEditWindow : Window
    {
        public GestorEditWindow(Gestor? gestor = null)
        {
            Title = gestor == null ? "Nuevo Gestor" : "Editar Gestor";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));

            Content = new System.Windows.Controls.TextBlock
            {
                Text = gestor == null ? "Ventana Nuevo Gestor - Próximamente" : $"Editar Gestor: {gestor.Nombre} - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16
            };
        }
    }

    #endregion
}