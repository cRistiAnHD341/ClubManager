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
    public class ReportWindowViewModel : BaseViewModel, IDisposable
    {
        private readonly ClubDbContext _dbContext;
        private readonly ReportsViewModel _reportsViewModel;
        private readonly IExportService _exportService;

        // Properties para filtros y configuración
        private bool _usarFiltrosFecha = false;
        private DateTime _fechaDesde = DateTime.Today.AddMonths(-1);
        private DateTime _fechaHasta = DateTime.Today;
        private string _formatoSeleccionado = "CSV (Excel)";
        private bool _incluirEncabezados = true;
        private bool _incluirTotales = true;
        private bool _abrirAutomaticamente = true;
        private string _estadoFiltro = "Todos los estados";
        private string _statusMessage = "Listo para generar reportes";
        private string _vistaPrevia = "Selecciona un reporte para ver la vista previa aquí...";
        private string _rutaReportes = "";

        // Estadísticas
        private int _totalAbonados;
        private int _abonadosActivos;
        private int _totalPeñas;
        private int _totalGestores;
        private string _ingresosEstimados = "0,00 €";
        private string _ultimaActualizacion = "";

        // Collections
        private ObservableCollection<Peña> _peñasDisponibles;
        private ObservableCollection<Gestor> _gestoresDisponibles;
        private Peña? _peñaFiltro;
        private Gestor? _gestorFiltro;

        public ReportWindowViewModel()
        {
            _dbContext = new ClubDbContext();
            _reportsViewModel = new ReportsViewModel();
            _exportService = new ExportService();

            _peñasDisponibles = new ObservableCollection<Peña>();
            _gestoresDisponibles = new ObservableCollection<Gestor>();

            InitializeCommands();
            LoadInitialData();
            UpdateStatistics();

            _rutaReportes = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reportes");
        }

        #region Properties

        public bool UsarFiltrosFecha
        {
            get => _usarFiltrosFecha;
            set => SetProperty(ref _usarFiltrosFecha, value);
        }

        public DateTime FechaDesde
        {
            get => _fechaDesde;
            set => SetProperty(ref _fechaDesde, value);
        }

        public DateTime FechaHasta
        {
            get => _fechaHasta;
            set => SetProperty(ref _fechaHasta, value);
        }

        public string FormatoSeleccionado
        {
            get => _formatoSeleccionado;
            set => SetProperty(ref _formatoSeleccionado, value);
        }

        public bool IncluirEncabezados
        {
            get => _incluirEncabezados;
            set => SetProperty(ref _incluirEncabezados, value);
        }

        public bool IncluirTotales
        {
            get => _incluirTotales;
            set => SetProperty(ref _incluirTotales, value);
        }

        public bool AbrirAutomaticamente
        {
            get => _abrirAutomaticamente;
            set => SetProperty(ref _abrirAutomaticamente, value);
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set => SetProperty(ref _estadoFiltro, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string VistaPrevia
        {
            get => _vistaPrevia;
            set => SetProperty(ref _vistaPrevia, value);
        }

        public string RutaReportes
        {
            get => _rutaReportes;
            set => SetProperty(ref _rutaReportes, value);
        }

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

        public int TotalPeñas
        {
            get => _totalPeñas;
            set => SetProperty(ref _totalPeñas, value);
        }

        public int TotalGestores
        {
            get => _totalGestores;
            set => SetProperty(ref _totalGestores, value);
        }

        public string IngresosEstimados
        {
            get => _ingresosEstimados;
            set => SetProperty(ref _ingresosEstimados, value);
        }

        public string UltimaActualizacion
        {
            get => _ultimaActualizacion;
            set => SetProperty(ref _ultimaActualizacion, value);
        }

        public ObservableCollection<Peña> PeñasDisponibles => _peñasDisponibles;
        public ObservableCollection<Gestor> GestoresDisponibles => _gestoresDisponibles;

        public Peña? PeñaFiltro
        {
            get => _peñaFiltro;
            set => SetProperty(ref _peñaFiltro, value);
        }

        public Gestor? GestorFiltro
        {
            get => _gestorFiltro;
            set => SetProperty(ref _gestorFiltro, value);
        }

        #endregion

        #region Commands

        // Comandos de Reportes Rápidos
        public ICommand GenerateSummaryReportCommand { get; private set; } = null!;
        public ICommand GenerateDashboardCommand { get; private set; } = null!;
        public ICommand GenerateFinancialSummaryCommand { get; private set; } = null!;

        // Comandos de Reportes de Abonados
        public ICommand GenerateAbonadosReportCommand { get; private set; } = null!;
        public ICommand GenerateEstadisticasEstadoCommand { get; private set; } = null!;
        public ICommand GenerateDistribucionPeñasCommand { get; private set; } = null!;
        public ICommand GenerateAbonadosTipoCommand { get; private set; } = null!;
        public ICommand GenerateAbonadosGestorCommand { get; private set; } = null!;
        public ICommand GenerateAltasPeriodoCommand { get; private set; } = null!;

        // Comandos de Reportes Financieros
        public ICommand GenerateIngresosCommand { get; private set; } = null!;
        public ICommand GenerateEvolucionIngresosCommand { get; private set; } = null!;
        public ICommand GenerateResumenPreciosCommand { get; private set; } = null!;
        public ICommand GenerateComparativaCommand { get; private set; } = null!;

        // Comandos de Reportes Administrativos
        public ICommand GenerateActividadCommand { get; private set; } = null!;
        public ICommand GenerateUsuariosCommand { get; private set; } = null!;
        public ICommand GenerateHistorialCommand { get; private set; } = null!;

        // Comandos de Exportación
        public ICommand ExportAbonadosCommand { get; private set; } = null!;
        public ICommand ExportTiposAbonoCommand { get; private set; } = null!;
        public ICommand ExportGestoresCommand { get; private set; } = null!;
        public ICommand ExportPeñasCommand { get; private set; } = null!;
        public ICommand ExportCompleteBackupCommand { get; private set; } = null!;

        // Comandos de Configuración
        public ICommand SetFechaHoyCommand { get; private set; } = null!;
        public ICommand SetFechaSemanaCommand { get; private set; } = null!;
        public ICommand SetFechaMesCommand { get; private set; } = null!;
        public ICommand SetFechaAñoCommand { get; private set; } = null!;
        public ICommand OpenReportsFolderCommand { get; private set; } = null!;
        public ICommand ConfigureReportsCommand { get; private set; } = null!;
        public ICommand RefreshDataCommand { get; private set; } = null!;
        public ICommand ShowHelpCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            // Reportes Rápidos
            GenerateSummaryReportCommand = new RelayCommand(async () => await GenerateSummaryReport());
            GenerateDashboardCommand = new RelayCommand(async () => await _reportsViewModel.GenerateDashboard());
            GenerateFinancialSummaryCommand = new RelayCommand(async () => await GenerateFinancialSummary());

            // Reportes de Abonados
            GenerateAbonadosReportCommand = new RelayCommand(async () => await _reportsViewModel.GenerateAbonadosReport());
            GenerateEstadisticasEstadoCommand = new RelayCommand(async () => await _reportsViewModel.GenerateEstadisticasEstado());
            GenerateDistribucionPeñasCommand = new RelayCommand(async () => await _reportsViewModel.GenerateDistribucionPeñas());
            GenerateAbonadosTipoCommand = new RelayCommand(async () => await _reportsViewModel.GenerateAbonadosTipo());
            GenerateAbonadosGestorCommand = new RelayCommand(async () => await _reportsViewModel.GenerateAbonadosGestor());
            GenerateAltasPeriodoCommand = new RelayCommand(async () => await _reportsViewModel.GenerateAltasPeriodo());

            // Reportes Financieros
            GenerateIngresosCommand = new RelayCommand(async () => await _reportsViewModel.GenerateIngresos());
            GenerateEvolucionIngresosCommand = new RelayCommand(async () => await _reportsViewModel.GenerateEvolucionIngresos());
            GenerateResumenPreciosCommand = new RelayCommand(async () => await _reportsViewModel.GenerateResumenPrecios());
            GenerateComparativaCommand = new RelayCommand(async () => await _reportsViewModel.GenerateComparativa());

            // Reportes Administrativos
            GenerateActividadCommand = new RelayCommand(async () => await _reportsViewModel.GenerateActividad());
            GenerateUsuariosCommand = new RelayCommand(async () => await _reportsViewModel.GenerateUsuarios());
            GenerateHistorialCommand = new RelayCommand(async () => await _reportsViewModel.GenerateHistorial());

            // Exportación
            ExportAbonadosCommand = new RelayCommand(async () => await _exportService.ExportAbonadosToCSV());
            ExportTiposAbonoCommand = new RelayCommand(async () => await _exportService.ExportTiposAbonoToCSV());
            ExportGestoresCommand = new RelayCommand(async () => await _exportService.ExportGestoresToCSV());
            ExportPeñasCommand = new RelayCommand(async () => await _exportService.ExportPeñasToCSV());
            ExportCompleteBackupCommand = new RelayCommand(async () => await _exportService.ExportCompleteBackup());

            // Configuración
            SetFechaHoyCommand = new RelayCommand(SetFechaHoy);
            SetFechaSemanaCommand = new RelayCommand(SetFechaSemana);
            SetFechaMesCommand = new RelayCommand(SetFechaMes);
            SetFechaAñoCommand = new RelayCommand(SetFechaAño);
            OpenReportsFolderCommand = new RelayCommand(async () => _reportsViewModel.OpenReportsFolder());
            ConfigureReportsCommand = new RelayCommand(_reportsViewModel.ConfigureReports);
            RefreshDataCommand = new RelayCommand(async () => await UpdateStatistics());
            ShowHelpCommand = new RelayCommand(ShowHelp);
        }

        #endregion

        #region Command Methods

        private async Task GenerateSummaryReport()
        {
            try
            {
                StatusMessage = "Generando resumen general...";

                var totalAbonados = await _dbContext.Abonados.CountAsync();
                var abonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
                var totalPeñas = await _dbContext.Peñas.CountAsync();
                var totalGestores = await _dbContext.Gestores.CountAsync();
                var totalTiposAbono = await _dbContext.TiposAbono.CountAsync(t => t.Activo);

                var ingresosTotales = await _dbContext.Abonados
                    .Include(a => a.TipoAbono)
                    .Where(a => a.Estado == EstadoAbonado.Activo && a.TipoAbono != null)
                    .SumAsync(a => a.TipoAbono!.Precio);

                var resumen = $"RESUMEN GENERAL DEL SISTEMA CLUBMANAGER\n" +
                             $"=========================================\n\n" +
                             $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                             $"Usuario: {UserSession.Instance.CurrentUser?.NombreCompleto ?? "Sistema"}\n\n" +
                             $"ESTADÍSTICAS PRINCIPALES:\n" +
                             $"-------------------------\n" +
                             $"Total de Abonados: {totalAbonados:N0}\n" +
                             $"Abonados Activos: {abonadosActivos:N0} ({(totalAbonados > 0 ? (double)abonadosActivos / totalAbonados * 100 : 0):F1}%)\n" +
                             $"Abonados Inactivos: {totalAbonados - abonadosActivos:N0}\n" +
                             $"Total Peñas: {totalPeñas:N0}\n" +
                             $"Total Gestores: {totalGestores:N0}\n" +
                             $"Tipos de Abono Activos: {totalTiposAbono:N0}\n\n" +
                             $"INFORMACIÓN FINANCIERA:\n" +
                             $"-----------------------\n" +
                             $"Ingresos Estimados (Abonados Activos): {ingresosTotales:C}\n" +
                             $"Ingreso Promedio por Abonado: {(abonadosActivos > 0 ? ingresosTotales / abonadosActivos : 0):C}\n\n" +
                             $"ESTADO DEL SISTEMA:\n" +
                             $"-------------------\n" +
                             $"Sistema: Operativo\n" +
                             $"Base de Datos: Conectada\n" +
                             $"Última Actualización: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n";

                await _exportService.ExportCustomReport("Resumen General", resumen);
                StatusMessage = "Resumen general generado correctamente";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar resumen general: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error al generar resumen general";
            }
        }

        private async Task GenerateFinancialSummary()
        {
            try
            {
                StatusMessage = "Generando resumen financiero...";

                var abonadosActivos = await _dbContext.Abonados
                    .Include(a => a.TipoAbono)
                    .Where(a => a.Estado == EstadoAbonado.Activo && a.TipoAbono != null)
                    .ToListAsync();

                var ingresosPorTipo = abonadosActivos
                    .GroupBy(a => a.TipoAbono!.Nombre)
                    .Select(g => new
                    {
                        TipoAbono = g.Key,
                        Cantidad = g.Count(),
                        PrecioUnitario = g.First().TipoAbono!.Precio,
                        IngresoTotal = g.Count() * g.First().TipoAbono!.Precio
                    })
                    .OrderByDescending(x => x.IngresoTotal);

                var totalIngresos = ingresosPorTipo.Sum(x => x.IngresoTotal);

                var resumen = $"RESUMEN FINANCIERO\n" +
                             $"==================\n\n" +
                             $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n\n" +
                             $"INGRESOS POR TIPO DE ABONO:\n" +
                             $"---------------------------\n";

                foreach (var tipo in ingresosPorTipo)
                {
                    var porcentaje = totalIngresos > 0 ? (double)tipo.IngresoTotal / (double)totalIngresos * 100 : 0;
                    resumen += $"{tipo.TipoAbono}:\n" +
                              $"  - Abonados: {tipo.Cantidad:N0}\n" +
                              $"  - Precio: {tipo.PrecioUnitario:C}\n" +
                              $"  - Ingreso Total: {tipo.IngresoTotal:C} ({porcentaje:F1}%)\n\n";
                }

                resumen += $"RESUMEN TOTAL:\n" +
                          $"--------------\n" +
                          $"Total Abonados Activos: {abonadosActivos.Count:N0}\n" +
                          $"Ingreso Total: {totalIngresos:C}\n" +
                          $"Ingreso Promedio: {(abonadosActivos.Count > 0 ? totalIngresos / abonadosActivos.Count : 0):C}\n";

                await _exportService.ExportCustomReport("Resumen Financiero", resumen);
                StatusMessage = $"Resumen financiero generado - Total: {totalIngresos:C}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar resumen financiero: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error al generar resumen financiero";
            }
        }

        private void SetFechaHoy()
        {
            FechaDesde = DateTime.Today;
            FechaHasta = DateTime.Today;
        }

        private void SetFechaSemana()
        {
            var hoy = DateTime.Today;
            var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek);
            FechaDesde = inicioSemana;
            FechaHasta = inicioSemana.AddDays(6);
        }

        private void SetFechaMes()
        {
            var hoy = DateTime.Today;
            FechaDesde = new DateTime(hoy.Year, hoy.Month, 1);
            FechaHasta = FechaDesde.AddMonths(1).AddDays(-1);
        }

        private void SetFechaAño()
        {
            var hoy = DateTime.Today;
            FechaDesde = new DateTime(hoy.Year, 1, 1);
            FechaHasta = new DateTime(hoy.Year, 12, 31);
        }

        private void ShowHelp()
        {
            MessageBox.Show("🔍 AYUDA - GENERADOR DE REPORTES\n\n" +
                           "📋 TIPOS DE REPORTES:\n" +
                           "• Reportes Rápidos: Información general del sistema\n" +
                           "• Abonados: Listados y estadísticas de abonados\n" +
                           "• Financieros: Análisis de ingresos y precios\n" +
                           "• Administrativos: Actividad y usuarios del sistema\n" +
                           "• Exportación: Backup y datos en CSV\n\n" +
                           "⚙️ CONFIGURACIÓN:\n" +
                           "• Use filtros de fecha para reportes específicos\n" +
                           "• Seleccione formato de salida (CSV, PDF, TXT)\n" +
                           "• Configure filtros por estado, peña o gestor\n\n" +
                           "📁 ARCHIVOS:\n" +
                           "• Los reportes se guardan automáticamente\n" +
                           "• Use 'Abrir Carpeta' para ver los archivos\n" +
                           "• Los archivos incluyen fecha y hora\n\n" +
                           "❓ Para más ayuda, consulte la documentación.",
                           "Ayuda - Generador de Reportes",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Data Methods

        private async void LoadInitialData()
        {
            try
            {
                // Cargar peñas
                var peñas = await _dbContext.Peñas.OrderBy(p => p.Nombre).ToListAsync();
                _peñasDisponibles.Clear();
                _peñasDisponibles.Add(new Peña { Id = 0, Nombre = "Todas las peñas" });
                foreach (var peña in peñas)
                {
                    _peñasDisponibles.Add(peña);
                }

                // Cargar gestores
                var gestores = await _dbContext.Gestores.OrderBy(g => g.Nombre).ToListAsync();
                _gestoresDisponibles.Clear();
                _gestoresDisponibles.Add(new Gestor { Id = 0, Nombre = "Todos los gestores" });
                foreach (var gestor in gestores)
                {
                    _gestoresDisponibles.Add(gestor);
                }

                // Seleccionar "Todos" por defecto
                PeñaFiltro = _peñasDisponibles.FirstOrDefault();
                GestorFiltro = _gestoresDisponibles.FirstOrDefault();

                UpdateVistaPrevia();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cargando datos: {ex.Message}";
            }
        }

        private async Task UpdateStatistics()
        {
            try
            {
                StatusMessage = "Actualizando estadísticas...";

                TotalAbonados = await _dbContext.Abonados.CountAsync();
                AbonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
                TotalPeñas = await _dbContext.Peñas.CountAsync();
                TotalGestores = await _dbContext.Gestores.CountAsync();

                var ingresos = await _dbContext.Abonados
                    .Include(a => a.TipoAbono)
                    .Where(a => a.Estado == EstadoAbonado.Activo && a.TipoAbono != null)
                    .SumAsync(a => a.TipoAbono!.Precio);

                IngresosEstimados = ingresos.ToString("C");
                UltimaActualizacion = DateTime.Now.ToString("HH:mm:ss");

                StatusMessage = "Estadísticas actualizadas correctamente";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error actualizando estadísticas: {ex.Message}";
            }
        }

        private void UpdateVistaPrevia()
        {
            var preview = $"CONFIGURACIÓN ACTUAL DE REPORTES\n" +
                         $"================================\n\n" +
                         $"📅 Filtros de Fecha: {(UsarFiltrosFecha ? "Activados" : "Desactivados")}\n";

            if (UsarFiltrosFecha)
            {
                preview += $"   Desde: {FechaDesde:dd/MM/yyyy}\n" +
                          $"   Hasta: {FechaHasta:dd/MM/yyyy}\n";
            }

            preview += $"📄 Formato: {FormatoSeleccionado}\n" +
                      $"📋 Encabezados: {(IncluirEncabezados ? "Sí" : "No")}\n" +
                      $"📊 Totales: {(IncluirTotales ? "Sí" : "No")}\n" +
                      $"👁️ Abrir automáticamente: {(AbrirAutomaticamente ? "Sí" : "No")}\n\n" +
                      $"🎯 FILTROS ADICIONALES:\n" +
                      $"Estado: {EstadoFiltro}\n" +
                      $"Peña: {PeñaFiltro?.Nombre ?? "Todas"}\n" +
                      $"Gestor: {GestorFiltro?.Nombre ?? "Todos"}\n\n" +
                      $"📁 Ruta de guardado:\n{RutaReportes}\n\n" +
                      $"Los reportes se generarán con esta configuración.";

            VistaPrevia = preview;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                _dbContext?.Dispose();
                _reportsViewModel?.Dispose();
                _peñasDisponibles?.Clear();
                _gestoresDisponibles?.Clear();
            }
            catch
            {
                // Ignorar errores al limpiar recursos
            }
        }

        #endregion
    }
}