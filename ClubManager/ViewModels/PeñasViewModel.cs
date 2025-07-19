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
    public class PeñasViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;

        private ObservableCollection<PeñaExtendida> _peñas;
        private ObservableCollection<PeñaExtendida> _peñasFiltered;
        private PeñaExtendida? _selectedPeña;
        private string _searchText = "";
        private string _subTitle = "";
        private bool _canEdit = false;
        private bool _hasSelectedPeña = false;

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
            set { _peñasFiltered = value; OnPropertyChanged(); }
        }

        public PeñaExtendida? SelectedPeña
        {
            get => _selectedPeña;
            set
            {
                _selectedPeña = value;
                OnPropertyChanged();
                HasSelectedPeña = value != null;
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

        public bool HasSelectedPeña
        {
            get => _hasSelectedPeña;
            set { _hasSelectedPeña = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand NewPeñaCommand { get; private set; }
        public ICommand EditPeñaCommand { get; private set; }
        public ICommand DeletePeñaCommand { get; private set; }
        public ICommand ViewAbonadosCommand { get; private set; }
        public ICommand ShowStatsCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            NewPeñaCommand = new RelayCommand(NewPeña);
            EditPeñaCommand = new RelayCommand<PeñaExtendida>(EditPeña);
            DeletePeñaCommand = new RelayCommand<PeñaExtendida>(DeletePeña);
            ViewAbonadosCommand = new RelayCommand<PeñaExtendida>(ViewAbonados);
            ShowStatsCommand = new RelayCommand(ShowStats);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new RelayCommand(ExportData);
        }

        private async void LoadData()
        {
            try
            {
                var peñas = await _dbContext.Peñas
                    .Include(p => p.Abonados)
                        .ThenInclude(a => a.TipoAbono)
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
                var filtered = _peñas.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(p =>
                        p.Nombre.ToLower().Contains(searchLower));
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
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSubTitle()
        {
            var total = _peñas.Count;
            var filtered = PeñasFiltered.Count;
            var totalAbonados = PeñasFiltered.Sum(p => p.TotalAbonados);
            var peñasActivas = PeñasFiltered.Count(p => p.TotalAbonados > 0);

            if (total == filtered)
            {
                SubTitle = $"Total: {total} peñas • Activas: {peñasActivas} • Miembros: {totalAbonados}";
            }
            else
            {
                SubTitle = $"Mostrando: {filtered} de {total} peñas • Activas: {peñasActivas} • Miembros: {totalAbonados}";
            }
        }

        #endregion

        #region Command Methods

        private void NewPeña()
        {
            if (!CanEdit)
            {
                MessageBox.Show("No tienes permisos para crear peñas. Licencia expirada o inválida.",
                               "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new PeñaEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditPeña(PeñaExtendida? peña)
        {
            if (peña == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para editar peñas. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var editWindow = new PeñaEditWindow(peña.Peña);
            if (editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private async void DeletePeña(PeñaExtendida? peña)
        {
            if (peña == null || !CanEdit)
            {
                if (!CanEdit)
                {
                    MessageBox.Show("No tienes permisos para eliminar peñas. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            if (peña.TotalAbonados > 0)
            {
                MessageBox.Show($"No se puede eliminar la peña '{peña.Nombre}' porque tiene {peña.TotalAbonados} abonados.\n\n" +
                               "Primero reasigna o elimina los abonados.",
                               "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Estás seguro de que quieres eliminar la peña '{peña.Nombre}'?\n\n" +
                "Esta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Peñas.Remove(peña.Peña);
                    await _dbContext.SaveChangesAsync();

                    await LogAction($"Eliminó peña: {peña.Nombre}");

                    LoadData();

                    MessageBox.Show("Peña eliminada correctamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar la peña: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewAbonados(PeñaExtendida? peña)
        {
            if (peña == null) return;

            MessageBox.Show($"Miembros de la peña '{peña.Nombre}'\n\n" +
                           $"Total miembros: {peña.TotalAbonados}\n" +
                           $"Miembros activos: {peña.AbonadosActivos}\n" +
                           $"Ingresos generados: {peña.IngresosGenerados:C}\n" +
                           $"Porcentaje del club: {peña.PorcentajeDelClub:F1}%",
                           "Información de la Peña",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowStats()
        {
            var totalPeñas = _peñas.Count;
            var peñasActivas = _peñas.Count(p => p.TotalAbonados > 0);
            var totalMiembros = _peñas.Sum(p => p.TotalAbonados);
            var miembrosActivos = _peñas.Sum(p => p.AbonadosActivos);
            var totalIngresos = _peñas.Sum(p => p.IngresosGenerados);

            var peñaMasGrande = _peñas.OrderByDescending(p => p.TotalAbonados).FirstOrDefault();
            var peñaMasIngresos = _peñas.OrderByDescending(p => p.IngresosGenerados).FirstOrDefault();

            var stats = $"🎪 ESTADÍSTICAS DE PEÑAS\n\n" +
                       $"Total peñas: {totalPeñas}\n" +
                       $"Peñas activas: {peñasActivas}\n" +
                       $"Total miembros: {totalMiembros}\n" +
                       $"Miembros activos: {miembrosActivos}\n" +
                       $"Ingresos totales: {totalIngresos:C}\n\n";

            if (peñaMasGrande != null)
            {
                stats += $"🏆 Peña más grande:\n" +
                         $"{peñaMasGrande.Nombre} ({peñaMasGrande.TotalAbonados} miembros)\n\n";
            }

            if (peñaMasIngresos != null && peñaMasIngresos != peñaMasGrande)
            {
                stats += $"💰 Peña con más ingresos:\n" +
                         $"{peñaMasIngresos.Nombre} ({peñaMasIngresos.IngresosGenerados:C})";
            }

            MessageBox.Show(stats, "Estadísticas de Peñas",
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
                    UsuarioId = 1,
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

    public class PeñaExtendida : INotifyPropertyChanged
    {
        public Peña Peña { get; }

        public PeñaExtendida(Peña peña)
        {
            Peña = peña;
            CalculateProperties();
        }

        private void CalculateProperties()
        {
            TotalAbonados = Peña.Abonados?.Count ?? 0;
            AbonadosActivos = Peña.Abonados?.Count(a => a.Estado == EstadoAbonado.Activo) ?? 0;
            IngresosGenerados = Peña.Abonados?
                .Where(a => a.Estado == EstadoAbonado.Activo && a.TipoAbono != null)
                .Sum(a => a.TipoAbono!.Precio) ?? 0;

            // Para calcular el porcentaje necesitarías el total del club
            // Por ahora lo dejamos en 0, se puede calcular después
            PorcentajeDelClub = 0; // Se podría calcular con una consulta adicional
        }

        // Propiedades delegadas
        public int Id => Peña.Id;
        public string Nombre => Peña.Nombre;

        // Propiedades calculadas
        public int TotalAbonados { get; private set; }
        public int AbonadosActivos { get; private set; }
        public decimal IngresosGenerados { get; private set; }
        public double PorcentajeDelClub { get; private set; }

        // Propiedades de estado
        public string EstadoPeña => TotalAbonados > 0 ? "Activa" : "Sin miembros";
        public string ColorEstado => TotalAbonados > 0 ? "#FF4CAF50" : "#FF9E9E9E";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Placeholder window
    public class PeñaEditWindow : Window
    {
        public PeñaEditWindow(Peña? peña = null)
        {
            Title = peña == null ? "Nueva Peña" : "Editar Peña";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));

            Content = new System.Windows.Controls.TextBlock
            {
                Text = peña == null ? "Ventana Nueva Peña - Próximamente" : $"Editar Peña: {peña.Nombre} - Próximamente",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16
            };
        }
    }

    #endregion
}