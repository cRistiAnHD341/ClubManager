using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ClubManager.Commands;
using ClubManager.Services;

namespace ClubManager.ViewModels
{
    public class CsvImportViewModel : BaseViewModel, IDisposable
    {
        private readonly ICsvImportService _importService;
        private CsvImportPreview? _preview;
        private string _filePath = "";
        private bool _hasHeaders = true;
        private bool _validateUnique = true;
        private bool _isLoading = false;
        private string _statusMessage = "";
        private bool _disposed = false;

        public CsvImportViewModel()
        {
            try
            {
                _importService = new CsvImportService();
                FieldMappings = new ObservableCollection<CsvFieldMappingViewModel>();
                AvailableFields = new ObservableCollection<string>(_importService.GetAvailableFields());
                InitializeCommands();
                StatusMessage = "Seleccione un archivo CSV para comenzar";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al inicializar: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error en constructor CsvImportViewModel: {ex}");

                // Inicializar colecciones vacías para evitar errores
                FieldMappings = new ObservableCollection<CsvFieldMappingViewModel>();
                AvailableFields = new ObservableCollection<string>();
                InitializeCommands();
            }
        }

        #region Properties

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    OnPropertyChanged(nameof(CanPreview));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool HasHeaders
        {
            get => _hasHeaders;
            set => SetProperty(ref _hasHeaders, value);
        }

        public bool ValidateUnique
        {
            get => _validateUnique;
            set => SetProperty(ref _validateUnique, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public CsvImportPreview? Preview
        {
            get => _preview;
            set
            {
                if (SetProperty(ref _preview, value))
                {
                    OnPropertyChanged(nameof(HasPreview));
                    OnPropertyChanged(nameof(PreviewHeaders));
                    OnPropertyChanged(nameof(PreviewRows));
                    OnPropertyChanged(nameof(CanImport));
                    UpdateFieldMappings();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool HasPreview => _preview != null;
        public bool CanPreview => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath) && !IsLoading;
        public bool CanImport => HasPreview && !IsLoading && FieldMappings.Any(f => f.IsSelected && f.HasMapping);

        public string PreviewHeaders => _preview != null ? string.Join(" | ", _preview.Headers) : "";

        public string PreviewRows
        {
            get
            {
                if (_preview == null || !_preview.SampleRows.Any())
                    return "";

                var result = "";
                foreach (var row in _preview.SampleRows.Take(3))
                {
                    result += string.Join(" | ", row.Select(cell => cell.Length > 20 ? cell.Substring(0, 20) + "..." : cell)) + "\n";
                }
                return result.TrimEnd();
            }
        }

        public ObservableCollection<CsvFieldMappingViewModel> FieldMappings { get; }
        public ObservableCollection<string> AvailableFields { get; }

        #endregion

        #region Commands

        public ICommand SelectFileCommand { get; private set; } = null!;
        public ICommand PreviewFileCommand { get; private set; } = null!;
        public ICommand ImportDataCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SelectFileCommand = new RelayCommand(SelectFile, () => !IsLoading);
            PreviewFileCommand = new AsyncRelayCommand(PreviewFileAsync, () => CanPreview);
            ImportDataCommand = new AsyncRelayCommand(ImportDataAsync, () => CanImport);
            CancelCommand = new RelayCommand(Cancel);
        }

        #endregion

        #region Command Methods

        private void SelectFile()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Archivos CSV (*.csv)|*.csv|Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                    Title = "Seleccionar archivo CSV para importar",
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (openDialog.ShowDialog() == true)
                {
                    FilePath = openDialog.FileName;
                    StatusMessage = $"Archivo seleccionado: {Path.GetFileName(FilePath)}";

                    // Limpiar vista previa anterior
                    Preview = null;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al seleccionar archivo: {ex.Message}";
                MessageBox.Show($"Error al seleccionar el archivo:\n{ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PreviewFileAsync()
        {
            if (!CanPreview)
                return;

            IsLoading = true;
            StatusMessage = "📂 Analizando archivo...";

            try
            {
                Preview = await _importService.PreviewCsvFileAsync(FilePath);
                StatusMessage = $"✅ Archivo analizado: {Preview.TotalRows} filas encontradas, {Preview.Headers.Count} columnas detectadas";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error al analizar archivo: {ex.Message}";
                Preview = null;

                MessageBox.Show($"Error al analizar el archivo:\n\n{ex.Message}", "Error de Análisis",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ImportDataAsync()
        {
            if (!CanImport || Preview == null)
                return;

            var selectedMappings = FieldMappings.Where(f => f.IsSelected && f.HasMapping).ToList();
            if (!selectedMappings.Any())
            {
                StatusMessage = "❌ Seleccione al menos un campo para importar";
                return;
            }

            // Verificar campos obligatorios (solo Nombre y NumeroSocio)
            var requiredFields = new[] { "Nombre", "NumeroSocio" };
            var missingFields = requiredFields.Where(rf => !selectedMappings.Any(sm => sm.AbonadoField == rf)).ToArray();

            if (missingFields.Any())
            {
                StatusMessage = $"❌ Campos obligatorios faltantes: {string.Join(", ", missingFields)}";
                MessageBox.Show($"Los siguientes campos son obligatorios y deben ser mapeados:\n\n• {string.Join("\n• ", missingFields)}",
                              "Campos Obligatorios Faltantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Confirmar importación
            var confirmMessage = $"¿Confirma la importación de {Preview.TotalRows} filas?\n\n" +
                               $"📊 Campos seleccionados: {selectedMappings.Count}\n" +
                               $"🔍 Validar únicos: {(ValidateUnique ? "Sí" : "No")}\n" +
                               $"📁 Archivo: {Path.GetFileName(FilePath)}\n\n" +
                               $"Campos a importar:\n• {string.Join("\n• ", selectedMappings.Select(sm => sm.DisplayName))}";

            var result = MessageBox.Show(confirmMessage, "Confirmar Importación",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            StatusMessage = "📥 Importando datos...";

            try
            {
                var mapping = new CsvImportMapping
                {
                    FilePath = FilePath,
                    Delimiter = Preview.Delimiter,
                    HasHeaders = HasHeaders,
                    ValidateUnique = ValidateUnique,
                    FieldMappings = selectedMappings.Select(sm => new CsvFieldMapping
                    {
                        AbonadoField = sm.AbonadoField,
                        CsvColumnIndex = sm.CsvColumnIndex,
                        CsvColumnName = sm.CsvColumnName
                    }).ToList()
                };

                var importResult = await _importService.ImportDataAsync(mapping);

                if (importResult.IsSuccess)
                {
                    StatusMessage = $"✅ Importación completada: {importResult.Summary}";

                    var successMessage = $"🎉 ¡Importación completada exitosamente!\n\n📈 {importResult.Summary}";

                    if (importResult.Errors.Any())
                    {
                        successMessage += $"\n\n⚠️ Se encontraron algunos errores:\n";
                        successMessage += string.Join("\n", importResult.Errors.Take(5));

                        if (importResult.Errors.Count > 5)
                            successMessage += $"\n... y {importResult.Errors.Count - 5} errores más";
                    }

                    MessageBox.Show(successMessage, "Importación Completada",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Cerrar ventana si fue exitosa
                    if (importResult.SuccessCount > 0)
                    {
                        CloseWindow(true);
                    }
                }
                else
                {
                    StatusMessage = "❌ Error en la importación";

                    var errorMessage = "❌ La importación falló:\n\n";
                    errorMessage += string.Join("\n", importResult.Errors.Take(10));

                    if (importResult.Errors.Count > 10)
                        errorMessage += $"\n... y {importResult.Errors.Count - 10} errores más";

                    MessageBox.Show(errorMessage, "Error en Importación",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Error durante la importación:\n\n{ex.Message}", "Error Crítico",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            CloseWindow(false);
        }

        #endregion

        #region Methods

        private void UpdateFieldMappings()
        {
            FieldMappings.Clear();

            if (Preview == null)
                return;

            // Crear mapeos automáticos basados en nombres similares
            foreach (var field in AvailableFields)
            {
                var mapping = new CsvFieldMappingViewModel
                {
                    AbonadoField = field,
                    IsSelected = false
                };

                // Buscar columna CSV similar
                var csvColumnIndex = FindSimilarColumn(field, Preview.Headers);
                if (csvColumnIndex >= 0)
                {
                    mapping.CsvColumnIndex = csvColumnIndex;
                    mapping.CsvColumnName = Preview.Headers[csvColumnIndex];

                    // Auto-seleccionar campos obligatorios y comunes
                    var requiredFields = new[] { "Nombre", "NumeroSocio" };
                    var commonFields = new[] { "Apellidos", "DNI", "Telefono", "Email", "Estado" };

                    if (requiredFields.Contains(field) || commonFields.Contains(field))
                    {
                        mapping.IsSelected = true;
                    }
                }

                FieldMappings.Add(mapping);
            }

            // Asegurar que los campos obligatorios estén seleccionados si tienen mapeo
            var requiredFieldNames = new[] { "Nombre", "NumeroSocio" };
            foreach (var fieldName in requiredFieldNames)
            {
                var mapping = FieldMappings.FirstOrDefault(f => f.AbonadoField == fieldName);
                if (mapping != null && mapping.HasMapping)
                {
                    mapping.IsSelected = true;
                }
            }
        }

        private int FindSimilarColumn(string fieldName, List<string> headers)
        {
            // Mapeos exactos y similares
            var mappings = new Dictionary<string, string[]>
            {
                { "NumeroSocio", new[] { "numero", "socio", "num_socio", "numero_socio", "member_number", "id_socio", "socio_id", "numero socio" } },
                { "Nombre", new[] { "nombre", "name", "first_name", "primer_nombre", "nombres" } },
                { "Apellidos", new[] { "apellidos", "apellido", "surname", "last_name", "apellido paterno", "apellidos completos" } },
                { "DNI", new[] { "dni", "nif", "cedula", "documento", "id", "identificacion", "doc_identidad" } },
                { "Telefono", new[] { "telefono", "phone", "tel", "movil", "celular", "teléfono", "telephone", "mobile" } },
                { "Email", new[] { "email", "correo", "mail", "e-mail", "correo_electronico", "e_mail" } },
                { "Direccion", new[] { "direccion", "address", "domicilio", "dirección", "calle", "domicilio completo" } },
                { "FechaNacimiento", new[] { "fecha_nacimiento", "birth_date", "nacimiento", "fecha_nac", "fecha de nacimiento", "birthday" } },
                { "TallaCamiseta", new[] { "talla", "size", "talla_camiseta", "talla camiseta", "shirt_size", "tallaje" } },
                { "Estado", new[] { "estado", "status", "activo", "situacion", "situación", "pagado" } },
                { "Gestor", new[] { "gestor", "manager", "responsable", "vendedor" } },
                { "Peña", new[] { "peña", "pena", "group", "grupo", "club", "asociacion" } },
                { "TipoAbono", new[] { "tipo_abono", "abono", "subscription", "tipo abono", "modalidad", "categoria", "tipo" } },
                { "Observaciones", new[] { "observaciones", "notas", "comments", "comentarios", "remarks", "obs" } }
            };

            if (mappings.TryGetValue(fieldName, out string[]? keywords))
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i].ToLower()
                        .Replace(" ", "_")
                        .Replace("-", "_")
                        .Replace(".", "_")
                        .Trim();

                    // Buscar coincidencia exacta primero
                    if (keywords.Any(keyword => header == keyword.ToLower()))
                        return i;

                    // Buscar coincidencia parcial
                    if (keywords.Any(keyword => header.Contains(keyword.ToLower())))
                        return i;
                }
            }

            return -1;
        }

        private void CloseWindow(bool dialogResult)
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        if (window is Window w)
                        {
                            w.DialogResult = dialogResult;
                            w.Close();
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar ventana: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _importService?.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    public class CsvFieldMappingViewModel : BaseViewModel
    {
        private bool _isSelected;
        private int _csvColumnIndex = -1;
        private string _csvColumnName = "";

        public string AbonadoField { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int CsvColumnIndex
        {
            get => _csvColumnIndex;
            set
            {
                if (SetProperty(ref _csvColumnIndex, value))
                {
                    OnPropertyChanged(nameof(HasMapping));
                }
            }
        }

        public string CsvColumnName
        {
            get => _csvColumnName;
            set => SetProperty(ref _csvColumnName, value);
        }

        public bool HasMapping => CsvColumnIndex >= 0;

        public string DisplayName => GetDisplayName(AbonadoField);

        public bool IsRequired => GetRequiredFields().Contains(AbonadoField);

        private string GetDisplayName(string fieldName)
        {
            return fieldName switch
            {
                "NumeroSocio" => "Número de Socio *",
                "Nombre" => "Nombre *",
                "Apellidos" => "Apellidos",
                "DNI" => "DNI",
                "Telefono" => "Teléfono",
                "Email" => "Email",
                "Direccion" => "Dirección",
                "FechaNacimiento" => "Fecha de Nacimiento",
                "TallaCamiseta" => "Talla de Camiseta",
                "Observaciones" => "Observaciones",
                "Estado" => "Estado",
                "Gestor" => "Gestor",
                "Peña" => "Peña",
                "TipoAbono" => "Tipo de Abono",
                _ => fieldName
            };
        }

        private string[] GetRequiredFields()
        {
            return new[] { "NumeroSocio", "Nombre" };
        }
    }

    // AsyncRelayCommand para comandos asíncronos
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AsyncRelayCommand: {ex.Message}");
                MessageBox.Show($"Error al ejecutar comando: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}