using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ClubManager.Commands;
using ClubManager.Data;
using ClubManager.Models;

namespace ClubManager.ViewModels
{
    public class ExportViewModel : INotifyPropertyChanged
    {
        private readonly ClubDbContext _dbContext;
        private List<Abonado> _abonadosOriginales;

        // Configuración de exportación
        private string _formatoSeleccionado = "excel";
        private string _nombreArchivo = "abonados_export";
        private string _carpetaDestino = "";

        // Datos a incluir
        private bool _incluirNumeroSocio = true;
        private bool _incluirNombreCompleto = true;
        private bool _incluirDNI = true;
        private bool _incluirEstado = true;
        private bool _incluirPeña = true;
        private bool _incluirTipoAbono = true;
        private bool _incluirPrecio = false;
        private bool _incluirGestor = false;
        private bool _incluirCodigoBarras = false;
        private bool _incluirFechaCreacion = false;
        private bool _incluirEstadoImpresion = false;

        // Filtros
        private int? _filtroEstadoSeleccionado = null;
        private int? _filtroPeñaSeleccionado = null;
        private int? _filtroTipoAbonoSeleccionado = null;

        // Opciones adicionales
        private bool _incluirEncabezados = true;
        private bool _abrirDespuesExportar = true;
        private bool _incluirFechaEnNombre = true;

        // UI Properties
        private string _resumenExportacion = "";
        private double _progressValue = 0;
        private string _progressText = "";
        private Visibility _showProgress = Visibility.Collapsed;

        // Collections
        private ObservableCollection<FormatoExportacion> _formatosExportacion;
        private ObservableCollection<ExportFiltroItem> _filtrosEstado;
        private ObservableCollection<Peña> _filtrosPeña;
        private ObservableCollection<TipoAbono> _filtrosTipoAbono;
        private ObservableCollection<string> _columnasSeleccionadas;

        public event EventHandler<bool>? ExportCompleted;

        public ExportViewModel(List<Abonado>? abonados = null)
        {
            _dbContext = new ClubDbContext();
            _abonadosOriginales = abonados ?? new List<Abonado>();

            _formatosExportacion = new ObservableCollection<FormatoExportacion>();
            _filtrosEstado = new ObservableCollection<ExportFiltroItem>();
            _filtrosPeña = new ObservableCollection<Peña>();
            _filtrosTipoAbono = new ObservableCollection<TipoAbono>();
            _columnasSeleccionadas = new ObservableCollection<string>();

            InitializeCommands();
            InitializeFormatos();
            InitializeFiltros();
            SetDefaultFolder();

            if (_abonadosOriginales.Count == 0)
            {
                LoadAllAbonados();
            }

            UpdateResumen();
            UpdateColumnasSeleccionadas();
        }

        #region Properties

        public string FormatoSeleccionado
        {
            get => _formatoSeleccionado;
            set
            {
                _formatoSeleccionado = value;
                OnPropertyChanged();
                UpdateNombreArchivo();
                UpdateResumen();
            }
        }

        public string NombreArchivo
        {
            get => _nombreArchivo;
            set
            {
                _nombreArchivo = value;
                OnPropertyChanged();
                UpdateResumen();
            }
        }

        public string CarpetaDestino
        {
            get => _carpetaDestino;
            set
            {
                _carpetaDestino = value;
                OnPropertyChanged();
                UpdateResumen();
            }
        }

        // Datos a incluir
        public bool IncluirNumeroSocio
        {
            get => _incluirNumeroSocio;
            set { _incluirNumeroSocio = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirNombreCompleto
        {
            get => _incluirNombreCompleto;
            set { _incluirNombreCompleto = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirDNI
        {
            get => _incluirDNI;
            set { _incluirDNI = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirEstado
        {
            get => _incluirEstado;
            set { _incluirEstado = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirPeña
        {
            get => _incluirPeña;
            set { _incluirPeña = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirTipoAbono
        {
            get => _incluirTipoAbono;
            set { _incluirTipoAbono = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirPrecio
        {
            get => _incluirPrecio;
            set { _incluirPrecio = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirGestor
        {
            get => _incluirGestor;
            set { _incluirGestor = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirCodigoBarras
        {
            get => _incluirCodigoBarras;
            set { _incluirCodigoBarras = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirFechaCreacion
        {
            get => _incluirFechaCreacion;
            set { _incluirFechaCreacion = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        public bool IncluirEstadoImpresion
        {
            get => _incluirEstadoImpresion;
            set { _incluirEstadoImpresion = value; OnPropertyChanged(); UpdateColumnasSeleccionadas(); UpdateResumen(); }
        }

        // Filtros
        public int? FiltroEstadoSeleccionado
        {
            get => _filtroEstadoSeleccionado;
            set { _filtroEstadoSeleccionado = value; OnPropertyChanged(); UpdateResumen(); }
        }

        public int? FiltroPeñaSeleccionado
        {
            get => _filtroPeñaSeleccionado;
            set { _filtroPeñaSeleccionado = value; OnPropertyChanged(); UpdateResumen(); }
        }

        public int? FiltroTipoAbonoSeleccionado
        {
            get => _filtroTipoAbonoSeleccionado;
            set { _filtroTipoAbonoSeleccionado = value; OnPropertyChanged(); UpdateResumen(); }
        }

        // Opciones adicionales
        public bool IncluirEncabezados
        {
            get => _incluirEncabezados;
            set { _incluirEncabezados = value; OnPropertyChanged(); UpdateResumen(); }
        }

        public bool AbrirDespuesExportar
        {
            get => _abrirDespuesExportar;
            set { _abrirDespuesExportar = value; OnPropertyChanged(); }
        }

        public bool IncluirFechaEnNombre
        {
            get => _incluirFechaEnNombre;
            set
            {
                _incluirFechaEnNombre = value;
                OnPropertyChanged();
                UpdateNombreArchivo();
                UpdateResumen();
            }
        }

        // UI Properties
        public string ResumenExportacion
        {
            get => _resumenExportacion;
            set { _resumenExportacion = value; OnPropertyChanged(); }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public string ProgressText
        {
            get => _progressText;
            set { _progressText = value; OnPropertyChanged(); }
        }

        public Visibility ShowProgress
        {
            get => _showProgress;
            set { _showProgress = value; OnPropertyChanged(); }
        }

        // Collections
        public ObservableCollection<FormatoExportacion> FormatosExportacion
        {
            get => _formatosExportacion;
            set { _formatosExportacion = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ExportFiltroItem> FiltrosEstado
        {
            get => _filtrosEstado;
            set { _filtrosEstado = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Peña> FiltrosPeña
        {
            get => _filtrosPeña;
            set { _filtrosPeña = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TipoAbono> FiltrosTipoAbono
        {
            get => _filtrosTipoAbono;
            set { _filtrosTipoAbono = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ColumnasSeleccionadas
        {
            get => _columnasSeleccionadas;
            set { _columnasSeleccionadas = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand ExportCommand { get; private set; }
        public ICommand PreviewCommand { get; private set; }
        public ICommand SelectFolderCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            ExportCommand = new RelayCommand(ExportData, CanExport);
            PreviewCommand = new RelayCommand(PreviewData);
            SelectFolderCommand = new RelayCommand(SelectFolder);
        }

        private void InitializeFormatos()
        {
            FormatosExportacion.Add(new FormatoExportacion
            {
                Value = "excel",
                Display = "📊 Excel (.xlsx)",
                Extension = ".xlsx"
            });
            FormatosExportacion.Add(new FormatoExportacion
            {
                Value = "csv",
                Display = "📄 CSV (.csv)",
                Extension = ".csv"
            });
            FormatosExportacion.Add(new FormatoExportacion
            {
                Value = "txt",
                Display = "📝 Texto (.txt)",
                Extension = ".txt"
            });
            FormatosExportacion.Add(new FormatoExportacion
            {
                Value = "pdf",
                Display = "📋 PDF (.pdf)",
                Extension = ".pdf"
            });
        }

        private async void InitializeFiltros()
        {
            try
            {
                // Filtros de estado
                FiltrosEstado.Add(new ExportFiltroItem { Display = "Todos", Value = null });
                FiltrosEstado.Add(new ExportFiltroItem { Display = "Solo Activos", Value = (int)EstadoAbonado.Activo });
                FiltrosEstado.Add(new ExportFiltroItem { Display = "Solo Inactivos", Value = (int)EstadoAbonado.Inactivo });

                // Filtros de peñas
                var peñas = await _dbContext.Peñas.OrderBy(p => p.Nombre).ToListAsync();
                FiltrosPeña.Add(new Peña { Id = 0, Nombre = "Todas las peñas" });
                foreach (var peña in peñas)
                {
                    FiltrosPeña.Add(peña);
                }

                // Filtros de tipos de abono
                var tiposAbono = await _dbContext.TiposAbono.OrderBy(t => t.Nombre).ToListAsync();
                FiltrosTipoAbono.Add(new TipoAbono { Id = 0, Nombre = "Todos los tipos" });
                foreach (var tipo in tiposAbono)
                {
                    FiltrosTipoAbono.Add(tipo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading filters: {ex.Message}");
            }
        }

        private void SetDefaultFolder()
        {
            CarpetaDestino = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private async void LoadAllAbonados()
        {
            try
            {
                _abonadosOriginales = await _dbContext.Abonados
                    .Include(a => a.Gestor)
                    .Include(a => a.Peña)
                    .Include(a => a.TipoAbono)
                    .OrderBy(a => a.NumeroSocio)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar abonados: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateNombreArchivo()
        {
            var baseFileName = "abonados_export";

            if (IncluirFechaEnNombre)
            {
                baseFileName += $"_{DateTime.Now:yyyyMMdd}";
            }

            var formato = FormatosExportacion.FirstOrDefault(f => f.Value == FormatoSeleccionado);
            if (formato != null)
            {
                NombreArchivo = baseFileName + formato.Extension;
            }
        }

        private void UpdateColumnasSeleccionadas()
        {
            ColumnasSeleccionadas.Clear();

            if (IncluirNumeroSocio) ColumnasSeleccionadas.Add("• Número de Socio");
            if (IncluirNombreCompleto) ColumnasSeleccionadas.Add("• Nombre Completo");
            if (IncluirDNI) ColumnasSeleccionadas.Add("• DNI");
            if (IncluirEstado) ColumnasSeleccionadas.Add("• Estado");
            if (IncluirPeña) ColumnasSeleccionadas.Add("• Peña");
            if (IncluirTipoAbono) ColumnasSeleccionadas.Add("• Tipo de Abono");
            if (IncluirPrecio) ColumnasSeleccionadas.Add("• Precio");
            if (IncluirGestor) ColumnasSeleccionadas.Add("• Gestor");
            if (IncluirCodigoBarras) ColumnasSeleccionadas.Add("• Código de Barras");
            if (IncluirFechaCreacion) ColumnasSeleccionadas.Add("• Fecha de Alta");
            if (IncluirEstadoImpresion) ColumnasSeleccionadas.Add("• Estado de Impresión");

            if (ColumnasSeleccionadas.Count == 0)
            {
                ColumnasSeleccionadas.Add("• Ninguna columna seleccionada");
            }
        }

        private void UpdateResumen()
        {
            try
            {
                var abonadosFiltrados = GetAbonadosFiltrados();
                var formatoInfo = FormatosExportacion.FirstOrDefault(f => f.Value == FormatoSeleccionado);

                var resumen = $"📊 Formato: {formatoInfo?.Display ?? "No seleccionado"}\n" +
                             $"📁 Archivo: {NombreArchivo}\n" +
                             $"📂 Destino: {Path.GetFileName(CarpetaDestino)}\n" +
                             $"👥 Registros: {abonadosFiltrados.Count} abonados\n" +
                             $"📋 Columnas: {ColumnasSeleccionadas.Count(c => !c.Contains("Ninguna"))}\n";

                if (FiltroEstadoSeleccionado.HasValue)
                {
                    var filtroEstado = FiltrosEstado.FirstOrDefault(f => f.Value == FiltroEstadoSeleccionado);
                    resumen += $"🔍 Filtro Estado: {filtroEstado?.Display}\n";
                }

                if (FiltroPeñaSeleccionado.HasValue && FiltroPeñaSeleccionado > 0)
                {
                    var filtroPeña = FiltrosPeña.FirstOrDefault(p => p.Id == FiltroPeñaSeleccionado);
                    resumen += $"🔍 Filtro Peña: {filtroPeña?.Nombre}\n";
                }

                if (FiltroTipoAbonoSeleccionado.HasValue && FiltroTipoAbonoSeleccionado > 0)
                {
                    var filtroTipo = FiltrosTipoAbono.FirstOrDefault(t => t.Id == FiltroTipoAbonoSeleccionado);
                    resumen += $"🔍 Filtro Tipo: {filtroTipo?.Nombre}\n";
                }

                ResumenExportacion = resumen.TrimEnd('\n');
            }
            catch (Exception ex)
            {
                ResumenExportacion = $"Error al generar resumen: {ex.Message}";
            }
        }

        private List<Abonado> GetAbonadosFiltrados()
        {
            var filtrados = _abonadosOriginales.AsEnumerable();

            // Aplicar filtros
            if (FiltroEstadoSeleccionado.HasValue)
            {
                filtrados = filtrados.Where(a => (int)a.Estado == FiltroEstadoSeleccionado.Value);
            }

            if (FiltroPeñaSeleccionado.HasValue && FiltroPeñaSeleccionado.Value > 0)
            {
                filtrados = filtrados.Where(a => a.PeñaId == FiltroPeñaSeleccionado.Value);
            }

            if (FiltroTipoAbonoSeleccionado.HasValue && FiltroTipoAbonoSeleccionado.Value > 0)
            {
                filtrados = filtrados.Where(a => a.TipoAbonoId == FiltroTipoAbonoSeleccionado.Value);
            }

            return filtrados.ToList();
        }

        private bool CanExport()
        {
            return !string.IsNullOrWhiteSpace(NombreArchivo) &&
                   !string.IsNullOrWhiteSpace(CarpetaDestino) &&
                   ColumnasSeleccionadas.Any(c => !c.Contains("Ninguna"));
        }

        #endregion

        #region Command Methods

        private async void ExportData()
        {
            try
            {
                ShowProgress = Visibility.Visible;
                ProgressValue = 0;
                ProgressText = "Iniciando exportación...";

                var abonadosFiltrados = GetAbonadosFiltrados();

                if (abonadosFiltrados.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar con los filtros seleccionados.", "Sin Datos",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowProgress = Visibility.Collapsed;
                    return;
                }

                var fullPath = Path.Combine(CarpetaDestino, NombreArchivo);

                ProgressValue = 25;
                ProgressText = "Preparando datos...";

                // Simular delay para mostrar progreso
                await Task.Delay(500);

                switch (FormatoSeleccionado)
                {
                    case "csv":
                        await ExportToCsv(abonadosFiltrados, fullPath);
                        break;
                    case "txt":
                        await ExportToTxt(abonadosFiltrados, fullPath);
                        break;
                    case "excel":
                        await ExportToExcel(abonadosFiltrados, fullPath);
                        break;
                    case "pdf":
                        await ExportToPdf(abonadosFiltrados, fullPath);
                        break;
                    default:
                        throw new NotSupportedException($"Formato {FormatoSeleccionado} no soportado");
                }

                ProgressValue = 100;
                ProgressText = "Exportación completada";

                await Task.Delay(500);

                MessageBox.Show($"Datos exportados correctamente a:\n{fullPath}", "Exportación Exitosa",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                if (AbrirDespuesExportar && File.Exists(fullPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                }

                await LogAction($"Exportó {abonadosFiltrados.Count} abonados a {FormatoSeleccionado.ToUpper()}");

                ShowProgress = Visibility.Collapsed;
                ExportCompleted?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ShowProgress = Visibility.Collapsed;
                MessageBox.Show($"Error durante la exportación: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                ExportCompleted?.Invoke(this, false);
            }
        }

        private async Task ExportToCsv(List<Abonado> abonados, string filePath)
        {
            ProgressText = "Generando archivo CSV...";
            ProgressValue = 50;

            var csv = new StringBuilder();

            // Encabezados
            if (IncluirEncabezados)
            {
                var headers = new List<string>();
                if (IncluirNumeroSocio) headers.Add("Número Socio");
                if (IncluirNombreCompleto) headers.Add("Nombre Completo");
                if (IncluirDNI) headers.Add("DNI");
                if (IncluirEstado) headers.Add("Estado");
                if (IncluirPeña) headers.Add("Peña");
                if (IncluirTipoAbono) headers.Add("Tipo Abono");
                if (IncluirPrecio) headers.Add("Precio");
                if (IncluirGestor) headers.Add("Gestor");
                if (IncluirCodigoBarras) headers.Add("Código Barras");
                if (IncluirFechaCreacion) headers.Add("Fecha Alta");
                if (IncluirEstadoImpresion) headers.Add("Impreso");

                csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));
            }

            ProgressValue = 60;

            // Datos
            foreach (var abonado in abonados)
            {
                var row = new List<string>();
                if (IncluirNumeroSocio) row.Add($"\"{abonado.NumeroSocio}\"");
                if (IncluirNombreCompleto) row.Add($"\"{abonado.NombreCompleto}\"");
                if (IncluirDNI) row.Add($"\"{abonado.DNI}\"");
                if (IncluirEstado) row.Add($"\"{abonado.EstadoTexto}\"");
                if (IncluirPeña) row.Add($"\"{abonado.Peña?.Nombre ?? "Sin asignar"}\"");
                if (IncluirTipoAbono) row.Add($"\"{abonado.TipoAbono?.Nombre ?? "Sin asignar"}\"");
                if (IncluirPrecio) row.Add($"\"{abonado.TipoAbono?.Precio ?? 0:C}\"");
                if (IncluirGestor) row.Add($"\"{abonado.Gestor?.Nombre ?? "Sin asignar"}\"");
                if (IncluirCodigoBarras) row.Add($"\"{abonado.CodigoBarras}\"");
                if (IncluirFechaCreacion) row.Add($"\"{abonado.FechaCreacion:dd/MM/yyyy}\"");
                if (IncluirEstadoImpresion) row.Add($"\"{(abonado.Impreso ? "Sí" : "No")}\"");

                csv.AppendLine(string.Join(",", row));
            }

            ProgressValue = 90;
            ProgressText = "Guardando archivo...";

            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
        }

        private async Task ExportToTxt(List<Abonado> abonados, string filePath)
        {
            ProgressText = "Generando archivo de texto...";
            ProgressValue = 50;

            var txt = new StringBuilder();

            if (IncluirEncabezados)
            {
                txt.AppendLine("LISTADO DE ABONADOS");
                txt.AppendLine("===================");
                txt.AppendLine($"Fecha de exportación: {DateTime.Now:dd/MM/yyyy HH:mm}");
                txt.AppendLine($"Total de registros: {abonados.Count}");
                txt.AppendLine();
            }

            ProgressValue = 60;

            foreach (var abonado in abonados)
            {
                txt.AppendLine($"==========================================");
                if (IncluirNumeroSocio) txt.AppendLine($"Número de Socio: {abonado.NumeroSocio}");
                if (IncluirNombreCompleto) txt.AppendLine($"Nombre: {abonado.NombreCompleto}");
                if (IncluirDNI) txt.AppendLine($"DNI: {abonado.DNI}");
                if (IncluirEstado) txt.AppendLine($"Estado: {abonado.EstadoTexto}");
                if (IncluirPeña) txt.AppendLine($"Peña: {abonado.Peña?.Nombre ?? "Sin asignar"}");
                if (IncluirTipoAbono) txt.AppendLine($"Tipo de Abono: {abonado.TipoAbono?.Nombre ?? "Sin asignar"}");
                if (IncluirPrecio) txt.AppendLine($"Precio: {abonado.TipoAbono?.Precio ?? 0:C}");
                if (IncluirGestor) txt.AppendLine($"Gestor: {abonado.Gestor?.Nombre ?? "Sin asignar"}");
                if (IncluirCodigoBarras) txt.AppendLine($"Código de Barras: {abonado.CodigoBarras}");
                if (IncluirFechaCreacion) txt.AppendLine($"Fecha de Alta: {abonado.FechaCreacion:dd/MM/yyyy}");
                if (IncluirEstadoImpresion) txt.AppendLine($"Impreso: {(abonado.Impreso ? "Sí" : "No")}");
                txt.AppendLine();
            }

            ProgressValue = 90;
            ProgressText = "Guardando archivo...";

            await File.WriteAllTextAsync(filePath, txt.ToString(), Encoding.UTF8);
        }

        private async Task ExportToExcel(List<Abonado> abonados, string filePath)
        {
            ProgressText = "Generando archivo Excel...";
            ProgressValue = 50;

            // Por ahora, exportar como CSV con extensión .xlsx
            // En una implementación real se usaría una librería como EPPlus o ClosedXML
            await ExportToCsv(abonados, filePath.Replace(".xlsx", ".csv"));

            ProgressValue = 90;
            MessageBox.Show("Nota: El archivo se ha exportado en formato CSV.\nPara Excel completo se requiere una librería adicional.",
                           "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExportToPdf(List<Abonado> abonados, string filePath)
        {
            ProgressText = "Generando archivo PDF...";
            ProgressValue = 50;

            // Por ahora, exportar como texto plano
            // En una implementación real se usaría una librería como iTextSharp o similar
            await ExportToTxt(abonados, filePath.Replace(".pdf", ".txt"));

            ProgressValue = 90;
            MessageBox.Show("Nota: El archivo se ha exportado en formato TXT.\nPara PDF completo se requiere una librería adicional.",
                           "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PreviewData()
        {
            try
            {
                var abonadosFiltrados = GetAbonadosFiltrados();
                var preview = new StringBuilder();

                preview.AppendLine($"VISTA PREVIA - {abonadosFiltrados.Count} registros");
                preview.AppendLine("================================");

                var maxPreview = Math.Min(5, abonadosFiltrados.Count);
                for (int i = 0; i < maxPreview; i++)
                {
                    var abonado = abonadosFiltrados[i];
                    preview.AppendLine($"{i + 1}. {abonado.NombreCompleto} ({abonado.DNI}) - {abonado.EstadoTexto}");
                }

                if (abonadosFiltrados.Count > 5)
                {
                    preview.AppendLine($"... y {abonadosFiltrados.Count - 5} registros más");
                }

                MessageBox.Show(preview.ToString(), "Vista Previa",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en vista previa: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = CarpetaDestino;
                dialog.Description = "Seleccionar carpeta de destino";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    CarpetaDestino = dialog.SelectedPath;
                }
            }
        }

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

            if (propertyName == nameof(NombreArchivo) || propertyName == nameof(CarpetaDestino))
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

    #region Helper Classes

    public class FormatoExportacion
    {
        public string Value { get; set; } = "";
        public string Display { get; set; } = "";
        public string Extension { get; set; } = "";
    }

    public class ExportFiltroItem
    {
        public string Display { get; set; } = "";
        public int? Value { get; set; }
    }

    #endregion
}