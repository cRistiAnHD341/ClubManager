using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;

namespace ClubManager.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;
        private readonly IReportService _reportService;
        private readonly IExportService _exportService;
        private string _statusMessage = "Listo para generar reportes";

        public ReportsViewModel()
        {
            _dbContext = new ClubDbContext();
            _reportService = new ReportService();
            _exportService = new ExportService();

            InitializeCommands();
        }

        #region Properties

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Report Methods - Personalizados

        private async Task GenerateCustomQuery()
        {
            StatusMessage = "Funcionalidad en desarrollo...";
            await Task.Delay(1000);
            StatusMessage = "Listo";
        }

        private async Task GenerateDateRange()
        {
            StatusMessage = "Funcionalidad en desarrollo...";
            await Task.Delay(1000);
            StatusMessage = "Listo";
        }

        private async Task GenerateAdvancedFilter()
        {
            StatusMessage = "Funcionalidad en desarrollo...";
            await Task.Delay(1000);
            StatusMessage = "Listo";
        }

        private async Task GenerateDynamicCharts()
        {
            StatusMessage = "Funcionalidad en desarrollo...";
            await Task.Delay(1000);
            StatusMessage = "Listo";
        }

        #endregion

        #region Utility Methods

        public void ConfigureReports()
        {
            try
            {
                MessageBox.Show("Configuración de reportes disponible en próximas versiones.",
                              "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir configuración: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenReportsFolder()
        {
            try
            {
                string reportsFolder = GetReportsFolder();

                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = reportsFolder,
                    UseShellExecute = true
                });

                StatusMessage = "Carpeta de reportes abierta";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir carpeta de reportes: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetReportsFolder()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reportes");
        }

        public string GetReportPath(string fileName)
        {
            string reportsFolder = GetReportsFolder();

            if (!Directory.Exists(reportsFolder))
            {
                Directory.CreateDirectory(reportsFolder);
            }

            return Path.Combine(reportsFolder, fileName);
        }

        public async Task OpenGeneratedReport(string filePath)
        {
            try
            {
                var result = MessageBox.Show($"Reporte generado correctamente.\n\n¿Desea abrirlo ahora?",
                                           "Reporte Generado",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reporte generado en: {filePath}\n\nError al abrir: {ex.Message}",
                              "Reporte Generado",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private void HandleReportError(string operation, Exception ex)
        {
            StatusMessage = $"Error al {operation}";
            MessageBox.Show($"Error al {operation}: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #endregion

        #region Commands

        // Reportes de Abonados
        public ICommand GenerateAbonadosReportCommand { get; private set; } = null!;
        public ICommand GenerateEstadisticasEstadoCommand { get; private set; } = null!;
        public ICommand GenerateDistribucionPeñasCommand { get; private set; } = null!;
        public ICommand GenerateAbonadosTipoCommand { get; private set; } = null!;
        public ICommand GenerateAbonadosGestorCommand { get; private set; } = null!;
        public ICommand GenerateAltasPeriodoCommand { get; private set; } = null!;

        // Reportes Financieros
        public ICommand GenerateIngresosCommand { get; private set; } = null!;
        public ICommand GenerateEvolucionIngresosCommand { get; private set; } = null!;
        public ICommand GenerateResumenPreciosCommand { get; private set; } = null!;
        public ICommand GenerateComparativaCommand { get; private set; } = null!;

        // Reportes Administrativos
        public ICommand GenerateActividadCommand { get; private set; } = null!;
        public ICommand GenerateUsuariosCommand { get; private set; } = null!;
        public ICommand GenerateHistorialCommand { get; private set; } = null!;
        public ICommand GenerateDashboardCommand { get; private set; } = null!;

        // Reportes Personalizados
        public ICommand GenerateCustomQueryCommand { get; private set; } = null!;
        public ICommand GenerateDateRangeCommand { get; private set; } = null!;
        public ICommand GenerateAdvancedFilterCommand { get; private set; } = null!;
        public ICommand GenerateDynamicChartsCommand { get; private set; } = null!;

        // Utilidades
        public ICommand ConfigureReportsCommand { get; private set; } = null!;
        public ICommand OpenReportsFolderCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            // Reportes de Abonados
            GenerateAbonadosReportCommand = new RelayCommand(async () => await GenerateAbonadosReport());
            GenerateEstadisticasEstadoCommand = new RelayCommand(async () => await GenerateEstadisticasEstado());
            GenerateDistribucionPeñasCommand = new RelayCommand(async () => await GenerateDistribucionPeñas());
            GenerateAbonadosTipoCommand = new RelayCommand(async () => await GenerateAbonadosTipo());
            GenerateAbonadosGestorCommand = new RelayCommand(async () => await GenerateAbonadosGestor());
            GenerateAltasPeriodoCommand = new RelayCommand(async () => await GenerateAltasPeriodo());

            // Reportes Financieros
            GenerateIngresosCommand = new RelayCommand(async () => await GenerateIngresos());
            GenerateEvolucionIngresosCommand = new RelayCommand(async () => await GenerateEvolucionIngresos());
            GenerateResumenPreciosCommand = new RelayCommand(async () => await GenerateResumenPrecios());
            GenerateComparativaCommand = new RelayCommand(async () => await GenerateComparativa());

            // Reportes Administrativos
            GenerateActividadCommand = new RelayCommand(async () => await GenerateActividad());
            GenerateUsuariosCommand = new RelayCommand(async () => await GenerateUsuarios());
            GenerateHistorialCommand = new RelayCommand(async () => await GenerateHistorial());
            GenerateDashboardCommand = new RelayCommand(async () => await GenerateDashboard());

            // Reportes Personalizados
            GenerateCustomQueryCommand = new RelayCommand(async () => await GenerateCustomQuery());
            GenerateDateRangeCommand = new RelayCommand(async () => await GenerateDateRange());
            GenerateAdvancedFilterCommand = new RelayCommand(async () => await GenerateAdvancedFilter());
            GenerateDynamicChartsCommand = new RelayCommand(async () => await GenerateDynamicCharts());

            // Utilidades
            ConfigureReportsCommand = new RelayCommand(ConfigureReports);
            OpenReportsFolderCommand = new RelayCommand(OpenReportsFolder);
        }

        #endregion

        #region Report Methods - Abonados

        public async Task GenerateAbonadosReport()
        {
            try
            {
                StatusMessage = "Generando listado de abonados...";

                var abonados = await _dbContext.Abonados
                    .Include(a => a.Gestor)
                    .Include(a => a.Peña)
                    .Include(a => a.TipoAbono)
                    .OrderBy(a => a.NumeroSocio)
                    .ToListAsync();

                var fileName = $"Listado_Abonados_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(abonados.Select(a => new
                {
                    NumeroSocio = a.NumeroSocio,
                    NombreCompleto = a.NombreCompleto,
                    DNI = a.DNI,
                    Estado = a.EstadoTexto,
                    Peña = a.Peña?.Nombre ?? "Sin asignar",
                    TipoAbono = a.TipoAbono?.Nombre ?? "Sin asignar",
                    Gestor = a.Gestor?.Nombre ?? "Sin asignar",
                    FechaAlta = a.FechaCreacion.ToString("dd/MM/yyyy")
                }), filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Reporte generado: {abonados.Count} abonados";
            }
            catch (Exception ex)
            {
                HandleReportError("generar listado de abonados", ex);
            }
        }

        public async Task GenerateEstadisticasEstado()
        {
            try
            {
                StatusMessage = "Generando estadísticas por estado...";

                var estadisticas = await _dbContext.Abonados
                    .GroupBy(a => a.Estado)
                    .Select(g => new
                    {
                        Estado = g.Key.ToString(),
                        Cantidad = g.Count(),
                        Porcentaje = 0.0 // Se calculará después
                    })
                    .ToListAsync();

                var total = estadisticas.Sum(e => e.Cantidad);
                var estadisticasConPorcentaje = estadisticas.Select(e => new
                {
                    e.Estado,
                    e.Cantidad,
                    Porcentaje = total > 0 ? Math.Round((double)e.Cantidad / total * 100, 2) : 0
                });

                var fileName = $"Estadisticas_Estado_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(estadisticasConPorcentaje, filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Estadísticas generadas: {estadisticas.Count} estados";
            }
            catch (Exception ex)
            {
                HandleReportError("generar estadísticas por estado", ex);
            }
        }

        public async Task GenerateDistribucionPeñas()
        {
            try
            {
                StatusMessage = "Generando distribución por peñas...";

                var distribucion = await _dbContext.Abonados
                    .Include(a => a.Peña)
                    .GroupBy(a => a.Peña != null ? a.Peña.Nombre : "Sin peña")
                    .Select(g => new
                    {
                        Peña = g.Key,
                        Cantidad = g.Count(),
                        Activos = g.Count(a => a.Estado == EstadoAbonado.Activo),
                        Inactivos = g.Count(a => a.Estado != EstadoAbonado.Activo)
                    })
                    .OrderByDescending(d => d.Cantidad)
                    .ToListAsync();

                var fileName = $"Distribucion_Peñas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(distribucion, filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Distribución generada: {distribucion.Count} peñas";
            }
            catch (Exception ex)
            {
                HandleReportError("generar distribución por peñas", ex);
            }
        }

        public async Task GenerateAbonadosTipo()
        {
            try
            {
                StatusMessage = "Generando abonados por tipo...";

                var abonadosPorTipo = await _dbContext.Abonados
                    .Include(a => a.TipoAbono)
                    .GroupBy(a => a.TipoAbono != null ? a.TipoAbono.Nombre : "Sin tipo")
                    .Select(g => new
                    {
                        TipoAbono = g.Key,
                        Cantidad = g.Count(),
                        PrecioUnitario = g.FirstOrDefault() != null && g.FirstOrDefault()!.TipoAbono != null
                            ? g.FirstOrDefault()!.TipoAbono!.Precio : 0,
                        IngresoTotal = g.Count() * (g.FirstOrDefault() != null && g.FirstOrDefault()!.TipoAbono != null
                            ? g.FirstOrDefault()!.TipoAbono!.Precio : 0)
                    })
                    .OrderByDescending(t => t.Cantidad)
                    .ToListAsync();

                var fileName = $"Abonados_Por_Tipo_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(abonadosPorTipo, filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Reporte generado: {abonadosPorTipo.Count} tipos de abono";
            }
            catch (Exception ex)
            {
                HandleReportError("generar abonados por tipo", ex);
            }
        }

        public async Task GenerateAbonadosGestor()
        {
            try
            {
                StatusMessage = "Generando abonados por gestor...";

                var abonadosPorGestor = await _dbContext.Abonados
                    .Include(a => a.Gestor)
                    .GroupBy(a => a.Gestor != null ? a.Gestor.Nombre : "Sin gestor")
                    .Select(g => new
                    {
                        Gestor = g.Key,
                        TotalAbonados = g.Count(),
                        Activos = g.Count(a => a.Estado == EstadoAbonado.Activo),
                        Inactivos = g.Count(a => a.Estado != EstadoAbonado.Activo),
                        UltimaAlta = g.Max(a => a.FechaCreacion)
                    })
                    .OrderByDescending(g => g.TotalAbonados)
                    .ToListAsync();

                var fileName = $"Abonados_Por_Gestor_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(abonadosPorGestor.Select(g => new
                {
                    g.Gestor,
                    g.TotalAbonados,
                    g.Activos,
                    g.Inactivos,
                    UltimaAlta = g.UltimaAlta.ToString("dd/MM/yyyy")
                }), filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Reporte generado: {abonadosPorGestor.Count} gestores";
            }
            catch (Exception ex)
            {
                HandleReportError("generar abonados por gestor", ex);
            }
        }

        public async Task GenerateAltasPeriodo()
        {
            try
            {
                StatusMessage = "Generando altas por período...";

                var altasPorMes = await _dbContext.Abonados
                    .GroupBy(a => new { a.FechaCreacion.Year, a.FechaCreacion.Month })
                    .Select(g => new
                    {
                        Año = g.Key.Year,
                        Mes = g.Key.Month,
                        Cantidad = g.Count(),
                        Fecha = new DateTime(g.Key.Year, g.Key.Month, 1)
                    })
                    .OrderBy(a => a.Fecha)
                    .ToListAsync();

                var fileName = $"Altas_Por_Periodo_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(altasPorMes.Select(a => new
                {
                    Período = $"{a.Mes:00}/{a.Año}",
                    a.Cantidad,
                    Fecha = a.Fecha.ToString("MMMM yyyy")
                }), filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Reporte generado: {altasPorMes.Count} períodos";
            }
            catch (Exception ex)
            {
                HandleReportError("generar altas por período", ex);
            }
        }

        #endregion

        #region Report Methods - Financieros

        public async Task GenerateIngresos()
        {
            try
            {
                StatusMessage = "Generando reporte de ingresos...";

                var ingresos = await _dbContext.Abonados
                    .Include(a => a.TipoAbono)
                    .Where(a => a.Estado == EstadoAbonado.Activo && a.TipoAbono != null)
                    .GroupBy(a => a.TipoAbono!.Nombre)
                    .Select(g => new
                    {
                        TipoAbono = g.Key,
                        CantidadAbonados = g.Count(),
                        PrecioUnitario = g.First().TipoAbono!.Precio,
                        IngresoTotal = g.Count() * g.First().TipoAbono!.Precio
                    })
                    .OrderByDescending(i => i.IngresoTotal)
                    .ToListAsync();

                var fileName = $"Ingresos_Por_Tipo_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(ingresos, filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Reporte de ingresos generado: {ingresos.Sum(i => i.IngresoTotal):C}";
            }
            catch (Exception ex)
            {
                HandleReportError("generar reporte de ingresos", ex);
            }
        }

        public async Task GenerateEvolucionIngresos()
        {
            StatusMessage = "Funcionalidad en desarrollo...";
            await Task.Delay(1000);
            StatusMessage = "Listo";
        }

        public async Task GenerateResumenPrecios()
        {
            try
            {
                StatusMessage = "Generando resumen de precios...";

                var tiposAbono = await _dbContext.TiposAbono
                    .Include(t => t.Abonados)
                    .Select(t => new
                    {
                        Nombre = t.Nombre,
                        Precio = t.Precio,
                        AbonadosActivos = t.Abonados.Count(a => a.Estado == EstadoAbonado.Activo),
                        TotalAbonados = t.Abonados.Count,
                        Activo = t.Activo
                    })
                    .OrderBy(t => t.Precio)
                    .ToListAsync();

                var fileName = $"Resumen_Precios_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(tiposAbono, filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Resumen generado: {tiposAbono.Count} tipos de abono";
            }
            catch (Exception ex)
            {
                HandleReportError("generar resumen de precios", ex);
            }
        }

        public async Task GenerateComparativa()
        {
            StatusMessage = "Funcionalidad en desarrollo...";
            await Task.Delay(1000);
            StatusMessage = "Listo";
        }

        #endregion

        #region Report Methods - Administrativos

        public async Task GenerateActividad()
        {
            try
            {
                StatusMessage = "Generando reporte de actividad...";

                var actividad = await _dbContext.HistorialAcciones
                    .Include(h => h.Usuario)
                    .OrderByDescending(h => h.FechaHora)
                    .Take(1000)
                    .Select(h => new
                    {
                        Fecha = h.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                        Usuario = h.Usuario != null ? h.Usuario.NombreUsuario : "Sistema",
                        Accion = h.Accion
                    })
                    .ToListAsync();

                var fileName = $"Actividad_Sistema_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(actividad, filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Reporte generado: {actividad.Count} acciones";
            }
            catch (Exception ex)
            {
                HandleReportError("generar reporte de actividad", ex);
            }
        }

        public async Task GenerateUsuarios()
        {
            try
            {
                StatusMessage = "Generando reporte de usuarios...";

                var usuarios = await _dbContext.Usuarios
                    .Select(u => new
                    {
                        NombreUsuario = u.NombreUsuario,
                        NombreCompleto = u.NombreCompleto,
                        Email = u.Email,
                        Rol = u.Rol,
                        Activo = u.Activo,
                        UltimoAcceso = u.UltimoAcceso.HasValue ? u.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm") : "Nunca"
                    })
                    .ToListAsync();

                var fileName = $"Usuarios_Sistema_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = GetReportPath(fileName);

                await _exportService.ExportToCsv(usuarios, filePath);

                await OpenGeneratedReport(filePath);
                StatusMessage = $"Reporte generado: {usuarios.Count} usuarios";
            }
            catch (Exception ex)
            {
                HandleReportError("generar reporte de usuarios", ex);
            }
        }

        public async Task GenerateHistorial()
        {
            await GenerateActividad(); // Reutilizar la funcionalidad
        }

        public async Task GenerateDashboard()
        {
            StatusMessage = "Funcionalidad en desarrollo...";
            await Task.Delay(1000);
            StatusMessage = "Listo";
        }
    }
}
#endregion