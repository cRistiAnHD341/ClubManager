using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClubManager.Data;
using ClubManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private DispatcherTimer _refreshTimer;

        // Estadísticas principales
        private int _totalAbonados;
        private int _abonadosActivos;
        private int _abonadosInactivos;
        private decimal _ingresosEstimados;

        // Estadísticas adicionales
        private int _peñasActivas;
        private int _tiposAbonoCount;
        private int _gestoresCount;
        private string _porcentajeActivos = "0%";

        // Datos para gráficos
        private ObservableCollection<EstadisticaMensual> _estadisticasMensuales;
        public ObservableCollection<PeñaEstadistica> _estadisticasPeñas;
        private ObservableCollection<ActividadReciente> _actividadReciente;

        public DashboardViewModel()
        {
            _dbContext = new ClubDbContext();
            _estadisticasMensuales = new ObservableCollection<EstadisticaMensual>();
            _estadisticasPeñas = new ObservableCollection<PeñaEstadistica>();
            _actividadReciente = new ObservableCollection<ActividadReciente>();

            InitializeTimer();
            _ = LoadDataAsync();
        }

        #region Properties

        public int TotalAbonados
        {
            get => _totalAbonados;
            set => SetProperty(ref _totalAbonados, value);
        }

        public int AbonadosActivos
        {
            get => _abonadosActivos;
            set => SetProperty(ref _abonadosActivos, value);
        }

        public int AbonadosInactivos
        {
            get => _abonadosInactivos;
            set => SetProperty(ref _abonadosInactivos, value);
        }

        public decimal IngresosEstimados
        {
            get => _ingresosEstimados;
            set => SetProperty(ref _ingresosEstimados, value);
        }

        public int PeñasActivas
        {
            get => _peñasActivas;
            set => SetProperty(ref _peñasActivas, value);
        }

        public int TiposAbonoCount
        {
            get => _tiposAbonoCount;
            set => SetProperty(ref _tiposAbonoCount, value);
        }

        public int GestoresCount
        {
            get => _gestoresCount;
            set => SetProperty(ref _gestoresCount, value);
        }

        public string PorcentajeActivos
        {
            get => _porcentajeActivos;
            set => SetProperty(ref _porcentajeActivos, value);
        }

        public ObservableCollection<EstadisticaMensual> EstadisticasMensuales
        {
            get => _estadisticasMensuales;
            set => SetProperty(ref _estadisticasMensuales, value);
        }

        public ObservableCollection<PeñaEstadistica> EstadisticasPeñas
        {
            get => _estadisticasPeñas;
            set => SetProperty(ref _estadisticasPeñas, value);
        }

        public ObservableCollection<ActividadReciente> ActividadReciente
        {
            get => _actividadReciente;
            set => SetProperty(ref _actividadReciente, value);
        }

        #endregion

        #region Methods

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadStatistics();
                await LoadChartData();
                await LoadRecentActivity();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando dashboard: {ex.Message}");
            }
        }

        private async Task LoadStatistics()
        {
            TotalAbonados = await _dbContext.Abonados.CountAsync();
            AbonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
            AbonadosInactivos = TotalAbonados - AbonadosActivos;
            PeñasActivas = await _dbContext.Peñas.CountAsync();
            TiposAbonoCount = await _dbContext.TiposAbono.CountAsync();
            GestoresCount = await _dbContext.Gestores.CountAsync();

            // Calcular porcentaje de activos
            if (TotalAbonados > 0)
            {
                var porcentaje = (double)AbonadosActivos / TotalAbonados * 100;
                PorcentajeActivos = $"{porcentaje:F1}%";
            }

            // Calcular ingresos estimados
            var ingresosPorTipo = await _dbContext.TiposAbono
                .Include(t => t.Abonados)
                .Select(t => new { t.Precio, Count = t.Abonados.Count(a => a.Estado == EstadoAbonado.Activo) })
                .ToListAsync();

            IngresosEstimados = ingresosPorTipo.Sum(x => x.Precio * x.Count);
        }

        private async Task LoadChartData()
        {
            // Estadísticas mensuales (últimos 12 meses)
            EstadisticasMensuales.Clear();
            for (int i = 11; i >= 0; i--)
            {
                var fecha = DateTime.Now.AddMonths(-i);
                var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
                var finMes = inicioMes.AddMonths(1).AddDays(-1);

                var abonados = await _dbContext.Abonados
                    .CountAsync(a => a.FechaCreacion >= inicioMes && a.FechaCreacion <= finMes);

                EstadisticasMensuales.Add(new EstadisticaMensual
                {
                    Mes = fecha.ToString("MMM yyyy"),
                    Abonados = abonados
                });
            }

            // Estadísticas por peña
            EstadisticasPeñas.Clear();
            var peñasStats = await _dbContext.Peñas
                .Include(p => p.Abonados)
                .Select(p => new PeñaEstadistica
                {
                    Nombre = p.Nombre,
                    Total = p.Abonados.Count,
                    Activos = p.Abonados.Count(a => a.Estado == EstadoAbonado.Activo)
                })
                .OrderByDescending(p => p.Total)
                .ToListAsync();

            foreach (var stat in peñasStats)
            {
                EstadisticasPeñas.Add(stat);
            }
        }

        private async Task LoadRecentActivity()
        {
            ActividadReciente.Clear();
            var actividades = await _dbContext.HistorialAcciones
                .Include(h => h.Usuario)
                .OrderByDescending(h => h.FechaHora)
                .Take(10)
                .Select(h => new ActividadReciente
                {
                    Usuario = h.Usuario.NombreUsuario,
                    Accion = h.Accion,
                    Fecha = h.FechaHora,
                    Tipo = h.TipoAccion ?? "General"
                })
                .ToListAsync();

            foreach (var actividad in actividades)
            {
                ActividadReciente.Add(actividad);
            }
        }

        private void InitializeTimer()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // Actualizar cada 5 minutos
            };
            _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
            _refreshTimer.Start();
        }

        #endregion
    }

    // Clases auxiliares para estadísticas
    public class EstadisticaMensual
    {
        public string Mes { get; set; } = "";
        public int Abonados { get; set; }
    }

    public class PeñaEstadistica
    {
        public string Nombre { get; set; } = "";
        public int Total { get; set; }
        public int Activos { get; set; }
        public double Porcentaje => Total > 0 ? (double)Activos / Total * 100 : 0;
    }

    public class ActividadReciente
    {
        public string Usuario { get; set; } = "";
        public string Accion { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = "";
        public string FechaTexto => Fecha.ToString("dd/MM HH:mm");
    }
}