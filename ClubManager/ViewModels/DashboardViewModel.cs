using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using ClubManager.Services;
using ClubManager.Views;

namespace ClubManager.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private readonly ILicenseService _licenseService;
        private DispatcherTimer _refreshTimer;

        // Estadísticas principales
        private int _totalAbonados = 0;
        private int _abonadosActivos = 0;
        private int _abonadosInactivos = 0;
        private string _ingresosEstimados = "€0";

        // Estadísticas secundarias
        private int _abonadosImpresos = 0;
        private int _peñasActivas = 0;
        private int _tiposAbonoCount = 0;
        private string _porcentajePago = "0%";

        // UI Properties
        private string _welcomeMessage = "";
        private string _lastUpdateText = "";
        private double _barraActivos = 0;
        private double _barraInactivos = 0;

        // Collections
        private ObservableCollection<ActividadRecenteItem> _actividadReciente;
        private ObservableCollection<PeñaStatsItem> _topPeñas;

        public DashboardViewModel()
        {
            _dbContext = new ClubDbContext();
            _licenseService = new LicenseService();
            _actividadReciente = new ObservableCollection<ActividadRecenteItem>();
            _topPeñas = new ObservableCollection<PeñaStatsItem>();

            InitializeCommands();
            InitializeTimer();
            UpdateWelcomeMessage();
            LoadData();
        }

        #region Properties

        public int TotalAbonados
        {
            get => _totalAbonados;
            set { _totalAbonados = value; OnPropertyChanged(); }
        }

        public int AbonadosActivos
        {
            get => _abonadosActivos;
            set { _abonadosActivos = value; OnPropertyChanged(); }
        }

        public int AbonadosInactivos
        {
            get => _abonadosInactivos;
            set { _abonadosInactivos = value; OnPropertyChanged(); }
        }

        public string IngresosEstimados
        {
            get => _ingresosEstimados;
            set { _ingresosEstimados = value; OnPropertyChanged(); }
        }

        public int AbonadosImpresos
        {
            get => _abonadosImpresos;
            set { _abonadosImpresos = value; OnPropertyChanged(); }
        }

        public int PeñasActivas
        {
            get => _peñasActivas;
            set { _peñasActivas = value; OnPropertyChanged(); }
        }

        public int TiposAbonoCount
        {
            get => _tiposAbonoCount;
            set { _tiposAbonoCount = value; OnPropertyChanged(); }
        }

        public string PorcentajePago
        {
            get => _porcentajePago;
            set { _porcentajePago = value; OnPropertyChanged(); }
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set { _welcomeMessage = value; OnPropertyChanged(); }
        }

        public string LastUpdateText
        {
            get => _lastUpdateText;
            set { _lastUpdateText = value; OnPropertyChanged(); }
        }

        public double BarraActivos
        {
            get => _barraActivos;
            set { _barraActivos = value; OnPropertyChanged(); }
        }

        public double BarraInactivos
        {
            get => _barraInactivos;
            set { _barraInactivos = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ActividadRecenteItem> ActividadReciente
        {
            get => _actividadReciente;
            set { _actividadReciente = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PeñaStatsItem> TopPeñas
        {
            get => _topPeñas;
            set { _topPeñas = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; private set; }
        public ICommand ShowSummaryCommand { get; private set; }
        public ICommand ShowHistorialCommand { get; private set; }
        public ICommand NewAbonadoCommand { get; private set; }
        public ICommand ShowStatsCommand { get; private set; }
        public ICommand ShowPendientesCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand(RefreshData);
            ShowSummaryCommand = new RelayCommand(ShowSummary);
            ShowHistorialCommand = new RelayCommand(ShowHistorial);
            NewAbonadoCommand = new RelayCommand(NewAbonado);
            ShowStatsCommand = new RelayCommand(ShowStats);
            ShowPendientesCommand = new RelayCommand(ShowPendientes);
            ExportCommand = new RelayCommand(Export);
        }

        private void InitializeTimer()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _refreshTimer.Tick += (s, e) => RefreshData();
            _refreshTimer.Start();
        }

        private void UpdateWelcomeMessage()
        {
            var now = DateTime.Now;
            string saludo;

            if (now.Hour < 12)
                saludo = "Buenos días";
            else if (now.Hour < 18)
                saludo = "Buenas tardes";
            else
                saludo = "Buenas noches";

            _licenseService.LoadSavedLicense();
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();

            if (licenseInfo.IsValid && !licenseInfo.IsExpired)
            {
                WelcomeMessage = $"{saludo}! Bienvenido al panel de control.";
            }
            else if (licenseInfo.IsValid && licenseInfo.IsExpired)
            {
                WelcomeMessage = $"{saludo}! Modo solo lectura - Licencia expirada.";
            }
            else
            {
                WelcomeMessage = $"{saludo}! Sin licencia válida - Modo solo lectura.";
            }
        }

        #endregion

        #region Data Loading

        private async void LoadData()
        {
            try
            {
                await LoadMainStats();
                await LoadSecondaryStats();
                await LoadActivityRecent();
                await LoadTopPeñas();
                CalculateProgressBars();

                LastUpdateText = $"Última actualización: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos del dashboard: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMainStats()
        {
            try
            {
                TotalAbonados = await _dbContext.Abonados.CountAsync();
                AbonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
                AbonadosInactivos = TotalAbonados - AbonadosActivos;

                var ingresos = await _dbContext.Abonados
                    .Where(a => a.Estado == EstadoAbonado.Activo && a.TipoAbono != null)
                    .Include(a => a.TipoAbono)
                    .SumAsync(a => a.TipoAbono!.Precio);

                IngresosEstimados = $"€{ingresos:N0}";

                if (TotalAbonados > 0)
                {
                    var porcentaje = (double)AbonadosActivos / TotalAbonados * 100;
                    PorcentajePago = $"{porcentaje:F1}%";
                }
                else
                {
                    PorcentajePago = "0%";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading main stats: {ex.Message}");
            }
        }

        private async Task LoadSecondaryStats()
        {
            try
            {
                AbonadosImpresos = await _dbContext.Abonados.CountAsync(a => a.Impreso);

                PeñasActivas = await _dbContext.Peñas
                    .CountAsync(p => p.Abonados.Any());

                TiposAbonoCount = await _dbContext.TiposAbono.CountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading secondary stats: {ex.Message}");
            }
        }

        private async Task LoadActivityRecent()
        {
            try
            {
                var historial = await _dbContext.HistorialAcciones
                    .Include(h => h.Usuario)
                    .OrderByDescending(h => h.FechaHora)
                    .Take(10)
                    .ToListAsync();

                ActividadReciente.Clear();
                foreach (var item in historial)
                {
                    var actividadItem = new ActividadRecenteItem
                    {
                        Accion = item.Accion,
                        Detalle = item.Detalle ?? "",
                        FechaHora = item.FechaHora,
                        Icono = GetIconoParaAccion(item.Accion)
                    };
                    ActividadReciente.Add(actividadItem);
                }

                if (!ActividadReciente.Any())
                {
                    ActividadReciente.Add(new ActividadRecenteItem
                    {
                        Accion = "Sistema iniciado",
                        Detalle = "Dashboard cargado correctamente",
                        FechaHora = DateTime.Now,
                        Icono = "🚀"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading recent activity: {ex.Message}");
            }
        }

        private async Task LoadTopPeñas()
        {
            try
            {
                var peñasStats = await _dbContext.Peñas
                    .Select(p => new
                    {
                        p.Nombre,
                        Count = p.Abonados.Count()
                    })
                    .Where(p => p.Count > 0)
                    .OrderByDescending(p => p.Count)
                    .Take(5)
                    .ToListAsync();

                TopPeñas.Clear();
                var maxCount = peñasStats.FirstOrDefault()?.Count ?? 1;
                var colors = new[] { "#FF4CAF50", "#FF2196F3", "#FFFF9800", "#FF9C27B0", "#FFFF5722" };

                for (int i = 0; i < peñasStats.Count; i++)
                {
                    var peña = peñasStats[i];
                    TopPeñas.Add(new PeñaStatsItem
                    {
                        Nombre = peña.Nombre,
                        Count = peña.Count,
                        BarWidth = (double)peña.Count / maxCount * 200,
                        Color = colors[i % colors.Length]
                    });
                }

                if (!TopPeñas.Any())
                {
                    TopPeñas.Add(new PeñaStatsItem
                    {
                        Nombre = "Sin datos",
                        Count = 0,
                        BarWidth = 0,
                        Color = "#FF666666"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading top peñas: {ex.Message}");
            }
        }

        private void CalculateProgressBars()
        {
            if (TotalAbonados > 0)
            {
                BarraActivos = (double)AbonadosActivos / TotalAbonados * 200;
                BarraInactivos = (double)AbonadosInactivos / TotalAbonados * 200;
            }
            else
            {
                BarraActivos = 0;
                BarraInactivos = 0;
            }
        }

        private string GetIconoParaAccion(string accion)
        {
            var accionLower = accion.ToLower();

            if (accionLower.Contains("creó") || accionLower.Contains("nuevo"))
                return "➕";
            else if (accionLower.Contains("editó") || accionLower.Contains("modificó"))
                return "✏️";
            else if (accionLower.Contains("eliminó"))
                return "🗑️";
            else if (accionLower.Contains("impreso"))
                return "🖨️";
            else if (accionLower.Contains("activó"))
                return "✅";
            else if (accionLower.Contains("desactivó"))
                return "❌";
            else
                return "📝";
        }

        #endregion

        #region Command Methods

        private void RefreshData()
        {
            LoadData();
        }

        private void ShowSummary()
        {
            MessageBox.Show("Resumen ejecutivo - Funcionalidad próximamente", "Información",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHistorial()
        {
            MessageBox.Show("Historial completo - Funcionalidad próximamente", "Información",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NewAbonado()
        {
            try
            {
                _licenseService.LoadSavedLicense();
                var licenseInfo = _licenseService.GetCurrentLicenseInfo();

                if (!licenseInfo.IsValid || licenseInfo.IsExpired)
                {
                    MessageBox.Show("No tienes permisos para crear abonados. Licencia expirada o inválida.",
                                   "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var editWindow = new AbonadoEditWindow();
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir ventana de nuevo abonado: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowStats()
        {
            MessageBox.Show("Estadísticas detalladas - Funcionalidad próximamente", "Información",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowPendientes()
        {
            MessageBox.Show($"Abonados pendientes de pago: {AbonadosInactivos}", "Pendientes",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Export()
        {
            try
            {
                var exportWindow = new Views.ExportWindow();
                exportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir ventana de exportación: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
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
            _refreshTimer?.Stop();
            _dbContext?.Dispose();
        }

        #endregion
    }

    #region Helper Classes

    public class ActividadRecenteItem
    {
        public string Accion { get; set; } = "";
        public string Detalle { get; set; } = "";
        public DateTime FechaHora { get; set; }
        public string Icono { get; set; } = "📝";

        public string TiempoTranscurrido
        {
            get
            {
                var timeSpan = DateTime.Now - FechaHora;

                if (timeSpan.TotalMinutes < 1)
                    return "Ahora";
                else if (timeSpan.TotalHours < 1)
                    return $"{(int)timeSpan.TotalMinutes}m";
                else if (timeSpan.TotalDays < 1)
                    return $"{(int)timeSpan.TotalHours}h";
                else if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d";
                else
                    return FechaHora.ToString("dd/MM");
            }
        }
    }

    public class PeñaStatsItem
    {
        public string Nombre { get; set; } = "";
        public int Count { get; set; }
        public double BarWidth { get; set; }
        public string Color { get; set; } = "#FF007ACC";
    }

    #endregion
}