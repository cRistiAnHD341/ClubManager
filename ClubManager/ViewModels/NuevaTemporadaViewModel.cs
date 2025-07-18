using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;

namespace ClubManager.ViewModels
{
    public class NuevaTemporadaViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;

        // Estado actual
        private string _temporadaActual = "";
        private int _totalAbonados = 0;
        private int _abonadosActivos = 0;

        // Nueva temporada
        private string _nuevaTemporada = "";
        private bool _mantenerAbonados = true;
        private bool _empezarDesdeCero = false;

        // Opciones para mantener abonados
        private bool _ponerTodosInactivos = true;
        private bool _regenerarCodigosBarras = false;
        private bool _marcarComoNoImpresos = true;

        // Acciones adicionales
        private bool _crearBackup = true;
        private bool _actualizarNumerosSocio = false;

        // UI Properties
        private string _resumenCambios = "";

        public event EventHandler<bool>? SeasonCreated;

        public NuevaTemporadaViewModel()
        {
            _dbContext = new ClubDbContext();

            InitializeCommands();
            LoadCurrentSeasonData();
            GenerateDefaultSeasonName();
            UpdateResumen();
        }

        #region Properties

        public string TemporadaActual
        {
            get => _temporadaActual;
            set { _temporadaActual = value; OnPropertyChanged(); }
        }

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

        public string NuevaTemporada
        {
            get => _nuevaTemporada;
            set
            {
                _nuevaTemporada = value;
                OnPropertyChanged();
                UpdateResumen();
            }
        }

        public bool MantenerAbonados
        {
            get => _mantenerAbonados;
            set
            {
                _mantenerAbonados = value;
                if (value) EmpezarDesdeCero = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MantenerAbonadosInfo));
                OnPropertyChanged(nameof(EmpezarDesdeCeroWarning));
                UpdateResumen();
            }
        }

        public bool EmpezarDesdeCero
        {
            get => _empezarDesdeCero;
            set
            {
                _empezarDesdeCero = value;
                if (value) MantenerAbonados = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MantenerAbonadosInfo));
                OnPropertyChanged(nameof(EmpezarDesdeCeroWarning));
                UpdateResumen();
            }
        }

        public bool PonerTodosInactivos
        {
            get => _ponerTodosInactivos;
            set
            {
                _ponerTodosInactivos = value;
                OnPropertyChanged();
                UpdateResumen();
            }
        }

        public bool RegenerarCodigosBarras
        {
            get => _regenerarCodigosBarras;
            set
            {
                _regenerarCodigosBarras = value;
                OnPropertyChanged();
                UpdateResumen();
            }
        }

        public bool MarcarComoNoImpresos
        {
            get => _marcarComoNoImpresos;
            set
            {
                _marcarComoNoImpresos = value;
                OnPropertyChanged();
                UpdateResumen();
            }
        }

        public bool CrearBackup
        {
            get => _crearBackup;
            set
            {
                _crearBackup = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowBackupButton));
                UpdateResumen();
            }
        }

        public bool ActualizarNumerosSocio
        {
            get => _actualizarNumerosSocio;
            set
            {
                _actualizarNumerosSocio = value;
                OnPropertyChanged();
                UpdateResumen();
            }
        }

        public string ResumenCambios
        {
            get => _resumenCambios;
            set { _resumenCambios = value; OnPropertyChanged(); }
        }

        // Visibility properties
        public Visibility MantenerAbonadosInfo => MantenerAbonados ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmpezarDesdeCeroWarning => EmpezarDesdeCero ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowBackupButton => CrearBackup ? Visibility.Visible : Visibility.Collapsed;

        #endregion

        #region Commands

        public ICommand CreateSeasonCommand { get; private set; }
        public ICommand CreateBackupCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            CreateSeasonCommand = new RelayCommand(CreateSeason, CanCreateSeason);
            CreateBackupCommand = new RelayCommand(CreateBackup);
        }

        private async void LoadCurrentSeasonData()
        {
            try
            {
                var config = await _dbContext.Configuracion.FirstOrDefaultAsync();
                TemporadaActual = config?.TemporadaActual ?? "Sin temporada";

                TotalAbonados = await _dbContext.Abonados.CountAsync();
                AbonadosActivos = await _dbContext.Abonados.CountAsync(a => a.Estado == EstadoAbonado.Activo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos de temporada actual: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateDefaultSeasonName()
        {
            var currentYear = DateTime.Now.Year;
            var nextYear = currentYear + 1;
            NuevaTemporada = $"{currentYear}-{nextYear}";
        }

        private void UpdateResumen()
        {
            var resumen = $"📋 Nueva temporada: {NuevaTemporada}\n\n";

            if (MantenerAbonados)
            {
                resumen += "✅ Mantener abonados existentes:\n";
                if (PonerTodosInactivos)
                    resumen += "   • Todos los abonados serán marcados como inactivos\n";
                if (RegenerarCodigosBarras)
                    resumen += "   • Se regenerarán todos los códigos de barras\n";
                if (MarcarComoNoImpresos)
                    resumen += "   • Se marcarán todos como no impresos\n";
            }
            else if (EmpezarDesdeCero)
            {
                resumen += "🗑️ Eliminar todos los abonados existentes\n";
                resumen += "   • Los gestores, peñas y tipos de abono se mantienen\n";
            }

            if (CrearBackup)
                resumen += "\n💾 Se creará una copia de seguridad antes de proceder";

            if (ActualizarNumerosSocio)
                resumen += "\n🔢 La numeración de socios se reiniciará desde 1";

            ResumenCambios = resumen;
        }

        private bool CanCreateSeason()
        {
            return !string.IsNullOrWhiteSpace(NuevaTemporada);
        }

        #endregion

        #region Command Methods

        private async void CreateSeason()
        {
            try
            {
                // Confirmar la acción
                var message = "¿Estás seguro de que quieres crear la nueva temporada?\n\n" +
                             "Esta acción modificará la base de datos.";

                if (EmpezarDesdeCero)
                {
                    message += "\n\n⚠️ ATENCIÓN: Se eliminarán TODOS los abonados existentes.";
                }

                var result = MessageBox.Show(message, "Confirmar Nueva Temporada",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Crear backup si está habilitado
                if (CrearBackup)
                {
                    await CreateBackupInternal();
                }

                // Ejecutar transición
                await ExecuteSeasonTransition();

                // Actualizar configuración
                await UpdateSeasonConfiguration();

                MessageBox.Show("Nueva temporada creada exitosamente!", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                SeasonCreated?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear nueva temporada: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                SeasonCreated?.Invoke(this, false);
            }
        }

        private async void CreateBackup()
        {
            try
            {
                await CreateBackupInternal();
                MessageBox.Show("Copia de seguridad creada exitosamente!", "Éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear copia de seguridad: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CreateBackupInternal()
        {
            try
            {
                // Crear carpeta de backups
                string backupsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(backupsPath))
                {
                    Directory.CreateDirectory(backupsPath);
                }

                // Nombre del archivo de backup
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"ClubManager_Backup_{timestamp}.db";
                string backupPath = Path.Combine(backupsPath, backupFileName);

                // Obtener ruta de la base de datos actual
                string currentDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClubManager.db");

                // Cerrar la conexión temporalmente
                await _dbContext.Database.CloseConnectionAsync();

                // Copiar archivo de base de datos
                File.Copy(currentDbPath, backupPath);

                // Crear archivo de información del backup
                string infoFileName = $"ClubManager_Backup_{timestamp}_info.txt";
                string infoPath = Path.Combine(backupsPath, infoFileName);

                string backupInfo = $"Backup creado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                                   $"Temporada: {TemporadaActual}\n" +
                                   $"Total Abonados: {TotalAbonados}\n" +
                                   $"Abonados Activos: {AbonadosActivos}\n" +
                                   $"Archivo: {backupFileName}";

                File.WriteAllText(infoPath, backupInfo);

                // Registrar en historial
                await LogAction($"Creó backup de base de datos: {backupFileName}");
            }
            catch (Exception ex)
            {
                throw new Exception($"No se pudo crear el backup: {ex.Message}");
            }
        }

        private async Task ExecuteSeasonTransition()
        {
            if (EmpezarDesdeCero)
            {
                await ExecuteStartFromScratch();
            }
            else if (MantenerAbonados)
            {
                await ExecuteMaintainSubscribers();
            }
        }

        private async Task ExecuteStartFromScratch()
        {
            // Eliminar todos los abonados
            var allAbonados = await _dbContext.Abonados.ToListAsync();
            _dbContext.Abonados.RemoveRange(allAbonados);
            await _dbContext.SaveChangesAsync();

            await LogAction($"Eliminó {allAbonados.Count} abonados para nueva temporada: {NuevaTemporada}");
        }

        private async Task ExecuteMaintainSubscribers()
        {
            var allAbonados = await _dbContext.Abonados.ToListAsync();
            int modificados = 0;

            foreach (var abonado in allAbonados)
            {
                bool modified = false;

                if (PonerTodosInactivos && abonado.Estado == EstadoAbonado.Activo)
                {
                    abonado.Estado = EstadoAbonado.Inactivo;
                    modified = true;
                }

                if (RegenerarCodigosBarras)
                {
                    abonado.CodigoBarras = GenerateNewBarcode();
                    modified = true;
                }

                if (MarcarComoNoImpresos && abonado.Impreso)
                {
                    abonado.Impreso = false;
                    modified = true;
                }

                if (ActualizarNumerosSocio)
                {
                    // Esta funcionalidad se implementaría después de guardar los cambios
                    modified = true;
                }

                if (modified)
                    modificados++;
            }

            await _dbContext.SaveChangesAsync();

            // Actualizar números de socio si es necesario
            if (ActualizarNumerosSocio)
            {
                await RenumberSubscribers();
            }

            await LogAction($"Actualizó {modificados} abonados para nueva temporada: {NuevaTemporada}");
        }

        private async Task RenumberSubscribers()
        {
            var abonados = await _dbContext.Abonados
                .OrderBy(a => a.FechaCreacion)
                .ToListAsync();

            for (int i = 0; i < abonados.Count; i++)
            {
                abonados[i].NumeroSocio = i + 1;
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateSeasonConfiguration()
        {
            var config = await _dbContext.Configuracion.FirstOrDefaultAsync();
            if (config != null)
            {
                config.TemporadaActual = NuevaTemporada;
                config.FechaUltimaTemporada = DateTime.Now;
                config.FechaModificacion = DateTime.Now;
            }
            else
            {
                config = new Configuracion
                {
                    Id = 1,
                    NombreClub = "Mi Club Deportivo",
                    TemporadaActual = NuevaTemporada,
                    FechaUltimaTemporada = DateTime.Now,
                    FechaModificacion = DateTime.Now
                };
                _dbContext.Configuracion.Add(config);
            }

            await _dbContext.SaveChangesAsync();
            await LogAction($"Creó nueva temporada: {NuevaTemporada}");
        }

        private string GenerateNewBarcode()
        {
            var timestamp = DateTime.Now.Ticks.ToString().Substring(8);
            var random = new Random().Next(1000, 9999);
            return $"CLB{timestamp}{random}";
        }

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

            // Revalidar comando cuando cambian las propiedades relevantes
            if (propertyName == nameof(NuevaTemporada))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #endregion
    }
}