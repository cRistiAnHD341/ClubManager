using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.ViewModels
{
    public class NuevaTemporadaViewModel : BaseViewModel
    {
        private readonly ClubDbContext _dbContext;

        private string _nombreTemporada = "";
        private DateTime _fechaInicio = DateTime.Today;
        private DateTime _fechaFin = DateTime.Today.AddYears(1);
        private bool _crearBackup = true;
        private bool _resetearImpresiones = true;
        private bool _desactivarAbonados = false;
        private string _statusMessage = "";

        public NuevaTemporadaViewModel()
        {
            _dbContext = new ClubDbContext();

            // Generar nombre por defecto
            _nombreTemporada = $"Temporada {DateTime.Now.Year}-{DateTime.Now.Year + 1}";

            InitializeCommands();
        }

        #region Properties

        public string NombreTemporada
        {
            get => _nombreTemporada;
            set => SetProperty(ref _nombreTemporada, value);
        }

        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set => SetProperty(ref _fechaInicio, value);
        }

        public DateTime FechaFin
        {
            get => _fechaFin;
            set => SetProperty(ref _fechaFin, value);
        }

        public bool CrearBackup
        {
            get => _crearBackup;
            set => SetProperty(ref _crearBackup, value);
        }

        public bool ResetearImpresiones
        {
            get => _resetearImpresiones;
            set => SetProperty(ref _resetearImpresiones, value);
        }

        public bool DesactivarAbonados
        {
            get => _desactivarAbonados;
            set => SetProperty(ref _desactivarAbonados, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Commands

        public ICommand IniciarTemporadaCommand { get; private set; } = null!;
        public ICommand CancelarCommand { get; private set; } = null!;
        public ICommand CrearBackupCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            IniciarTemporadaCommand = new RelayCommand(async () => await IniciarTemporada(), CanIniciarTemporada);
            CancelarCommand = new RelayCommand(Cancelar);
            CrearBackupCommand = new RelayCommand(async () => await CrearBackupManual());
        }

        #endregion

        #region Command Methods

        private bool CanIniciarTemporada()
        {
            return !string.IsNullOrWhiteSpace(NombreTemporada) &&
                   FechaInicio < FechaFin;
        }

        private async Task IniciarTemporada()
        {
            try
            {
                StatusMessage = "Iniciando nueva temporada...";

                var result = MessageBox.Show(
                    $"¿Está seguro de iniciar la nueva temporada '{NombreTemporada}'?\n\n" +
                    "Esta acción realizará los siguientes cambios:\n" +
                    (CrearBackup ? "• Crear copia de seguridad\n" : "") +
                    (ResetearImpresiones ? "• Resetear estado de impresión de tarjetas\n" : "") +
                    (DesactivarAbonados ? "• Desactivar todos los abonados\n" : "") +
                    "• Registrar el inicio de temporada\n\n" +
                    "Esta acción no se puede deshacer.",
                    "Confirmar Nueva Temporada",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    StatusMessage = "Operación cancelada";
                    return;
                }

                // 1. Crear backup si está habilitado
                if (CrearBackup)
                {
                    StatusMessage = "Creando copia de seguridad...";
                    await CreateBackupInternal();
                }

                // 2. Resetear impresiones si está habilitado
                if (ResetearImpresiones)
                {
                    StatusMessage = "Reseteando estado de impresión...";
                    await ResetPrintStatus();
                }

                // 3. Desactivar abonados si está habilitado
                if (DesactivarAbonados)
                {
                    StatusMessage = "Desactivando abonados...";
                    await DeactivateAllAbonados();
                }

                // 4. Registrar el inicio de temporada
                await LogSeasonStart();

                StatusMessage = "Nueva temporada iniciada correctamente";

                MessageBox.Show(
                    $"Nueva temporada '{NombreTemporada}' iniciada correctamente.\n\n" +
                    "Revise las configuraciones y prepare las nuevas tarjetas de abonado.",
                    "Temporada Iniciada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CloseWindow();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error al iniciar nueva temporada: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar()
        {
            CloseWindow();
        }

        private async Task CrearBackupManual()
        {
            try
            {
                StatusMessage = "Creando copia de seguridad...";
                await CreateBackupInternal();
                StatusMessage = "Copia de seguridad creada correctamente";

                MessageBox.Show("Copia de seguridad creada correctamente.", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creando backup: {ex.Message}";
                MessageBox.Show($"Error al crear copia de seguridad: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Methods

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
                                   $"Temporada: {NombreTemporada}\n" +
                                   $"Total Abonados: {await _dbContext.Abonados.CountAsync()}\n" +
                                   $"Total Usuarios: {await _dbContext.Usuarios.CountAsync()}\n" +
                                   $"Motivo: Inicio de nueva temporada";

                await File.WriteAllTextAsync(infoPath, backupInfo);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creando backup: {ex.Message}");
            }
        }

        private async Task ResetPrintStatus()
        {
            try
            {
                var abonados = await _dbContext.Abonados.ToListAsync();

                foreach (var abonado in abonados)
                {
                    abonado.Impreso = false;
                }

                await _dbContext.SaveChangesAsync();

                await LogAction($"Reseteado estado de impresión de {abonados.Count} abonados para nueva temporada");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reseteando impresiones: {ex.Message}");
            }
        }

        private async Task DeactivateAllAbonados()
        {
            try
            {
                var abonadosActivos = await _dbContext.Abonados
                    .Where(a => a.Estado == EstadoAbonado.Activo)
                    .ToListAsync();

                foreach (var abonado in abonadosActivos)
                {
                    abonado.Estado = EstadoAbonado.Inactivo;
                }

                await _dbContext.SaveChangesAsync();

                await LogAction($"Desactivados {abonadosActivos.Count} abonados para nueva temporada");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error desactivando abonados: {ex.Message}");
            }
        }

        private async Task LogSeasonStart()
        {
            try
            {
                var accion = $"Iniciada nueva temporada: {NombreTemporada} ({FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy})";
                await LogAction(accion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging season start: {ex.Message}");
            }
        }

        private async Task LogAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "Temporada",
                    FechaHora = DateTime.Now,
                    Detalles = $"Temporada: {NombreTemporada}"
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch { }
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is NuevaTemporadaWindow seasonWindow &&
                    ReferenceEquals(seasonWindow.DataContext, this))
                {
                    seasonWindow.Close();
                    break;
                }
            }
        }

        #endregion
    }
}