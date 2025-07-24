using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;
using ClubManager.Data;
using ClubManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.Services
{
    public interface ICsvImportService : IDisposable
    {
        Task<CsvImportPreview> PreviewCsvFileAsync(string filePath);
        Task<CsvImportResult> ImportDataAsync(CsvImportMapping mapping);
        List<string> GetAvailableFields();
    }

    public class CsvImportService : ICsvImportService
    {
        private ClubDbContext? _dbContext;
        private bool _disposed = false;

        public CsvImportService()
        {
            try
            {
                _dbContext = new ClubDbContext();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear ClubDbContext: {ex.Message}");
                throw new InvalidOperationException("No se pudo inicializar la conexión a la base de datos", ex);
            }
        }

        public List<string> GetAvailableFields()
        {
            return new List<string>
            {
                "NumeroSocio",
                "Nombre",
                "Apellidos",
                "DNI",
                "Telefono",
                "Email",
                "Direccion",
                "FechaNacimiento",
                "TallaCamiseta",
                "Observaciones",
                "Estado",
                "Gestor",
                "Peña",
                "TipoAbono"
            };
        }

        public async Task<CsvImportPreview> PreviewCsvFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"El archivo {filePath} no existe.");

            try
            {
                // Leer archivo con encoding apropiado
                string[] lines;
                var encoding = DetectEncoding(filePath);
                lines = await File.ReadAllLinesAsync(filePath, encoding);

                if (lines.Length == 0)
                    throw new Exception("El archivo CSV está vacío.");

                // Detectar delimitador
                var delimiter = DetectDelimiter(lines[0]);

                // Obtener encabezados
                var headers = ParseCsvLine(lines[0], delimiter).ToList();

                // Obtener primeras 5 filas como muestra (saltando encabezados)
                var sampleRows = new List<string[]>();
                int startRow = 1;
                int maxSampleRows = Math.Min(6, lines.Length);

                for (int i = startRow; i < maxSampleRows; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        var row = ParseCsvLine(lines[i], delimiter);
                        sampleRows.Add(row);
                    }
                }

                return new CsvImportPreview
                {
                    FilePath = filePath,
                    Headers = headers,
                    SampleRows = sampleRows,
                    TotalRows = Math.Max(0, lines.Length - 1), // Excluyendo encabezados
                    Delimiter = delimiter
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al leer el archivo CSV: {ex.Message}", ex);
            }
        }

        public async Task<CsvImportResult> ImportDataAsync(CsvImportMapping mapping)
        {
            var result = new CsvImportResult();

            if (_dbContext == null)
            {
                result.Errors.Add("No hay conexión a la base de datos");
                return result;
            }

            try
            {
                var encoding = DetectEncoding(mapping.FilePath);
                var lines = await File.ReadAllLinesAsync(mapping.FilePath, encoding);
                var startIndex = mapping.HasHeaders ? 1 : 0;

                // Pre-cargar datos relacionados para optimizar consultas
                var gestores = await _dbContext.Gestores.ToDictionaryAsync(g => g.Nombre, g => g);
                var peñas = await _dbContext.Peñas.ToDictionaryAsync(p => p.Nombre, p => p);
                var tiposAbono = await _dbContext.TiposAbono.ToDictionaryAsync(t => t.Nombre, t => t);
                var existingNumerosSocio = new HashSet<int>();

                if (mapping.ValidateUnique)
                {
                    existingNumerosSocio = (await _dbContext.Abonados
                        .Select(a => a.NumeroSocio)
                        .ToListAsync())
                        .ToHashSet();
                }

                // Procesar cada registro individualmente para evitar fallos en lote
                for (int i = startIndex; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    try
                    {
                        var fields = ParseCsvLine(lines[i], mapping.Delimiter);
                        var abonado = CreateAbonadoFromFields(fields, mapping, gestores, peñas, tiposAbono);

                        // Validar datos requeridos
                        var validation = ValidateAbonado(abonado, existingNumerosSocio, mapping.ValidateUnique);
                        if (!validation.IsValid)
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"Fila {i + 1}: {validation.ErrorMessage}");
                            continue;
                        }

                        // Agregar a la lista de números existentes si se valida unicidad
                        if (mapping.ValidateUnique)
                        {
                            existingNumerosSocio.Add(abonado.NumeroSocio);
                        }

                        // Procesar individualmente cada abonado
                        try
                        {
                            _dbContext.Abonados.Add(abonado);
                            await _dbContext.SaveChangesAsync();
                            result.SuccessCount++;

                            // Debug para seguimiento
                            System.Diagnostics.Debug.WriteLine($"Importado exitosamente: Fila {i + 1} - {abonado.NombreCompleto}");
                        }
                        catch (Exception saveEx)
                        {
                            // Si falla al guardar, remover de context y registrar error
                            _dbContext.Entry(abonado).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                            result.ErrorCount++;
                            result.Errors.Add($"Fila {i + 1}: Error al guardar - {saveEx.Message}");

                            System.Diagnostics.Debug.WriteLine($"Error al guardar fila {i + 1}: {saveEx.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Fila {i + 1}: Error al procesar - {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Error al procesar fila {i + 1}: {ex.Message}");
                    }
                }

                result.IsSuccess = result.SuccessCount > 0;

                // Log de la acción
                if (result.IsSuccess)
                {
                    await LogImportAction(result);
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"Error general durante la importación: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error general en ImportDataAsync: {ex}");
            }

            return result;
        }

        private async Task ProcessBatch(List<Abonado> batch, CsvImportResult result)
        {
            try
            {
                _dbContext!.Abonados.AddRange(batch);
                await _dbContext.SaveChangesAsync();
                result.SuccessCount += batch.Count;
            }
            catch (Exception ex)
            {
                result.ErrorCount += batch.Count;
                result.Errors.Add($"Error al guardar lote: {ex.Message}");
            }
        }

        private Encoding DetectEncoding(string filePath)
        {
            try
            {
                // Leer los primeros bytes para detectar BOM
                var bytes = File.ReadAllBytes(filePath);

                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    return Encoding.UTF8;

                if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                    return Encoding.Unicode;

                if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                    return Encoding.BigEndianUnicode;

                // Por defecto, usar UTF-8
                return Encoding.UTF8;
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        private char DetectDelimiter(string firstLine)
        {
            var delimiters = new[] { ',', ';', '\t', '|' };
            var counts = delimiters.ToDictionary(d => d, d => firstLine.Count(c => c == d));
            return counts.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private string[] ParseCsvLine(string line, char delimiter)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Comillas escapadas
                        currentField.Append('"');
                        i++; // Saltar la siguiente comilla
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString().Trim());
            return fields.ToArray();
        }

        private Abonado CreateAbonadoFromFields(string[] fields, CsvImportMapping mapping,
            Dictionary<string, Gestor> gestores, Dictionary<string, Peña> peñas,
            Dictionary<string, TipoAbono> tiposAbono)
        {
            var abonado = new Abonado();

            foreach (var fieldMapping in mapping.FieldMappings)
            {
                if (fieldMapping.CsvColumnIndex < 0 || fieldMapping.CsvColumnIndex >= fields.Length)
                    continue;

                var value = fields[fieldMapping.CsvColumnIndex]?.Trim();
                if (string.IsNullOrEmpty(value))
                    continue;

                try
                {
                    switch (fieldMapping.AbonadoField)
                    {
                        case "NumeroSocio":
                            if (int.TryParse(value, out int numeroSocio))
                                abonado.NumeroSocio = numeroSocio;
                            break;

                        case "Nombre":
                            abonado.Nombre = value;
                            break;

                        case "Apellidos":
                            abonado.Apellidos = string.IsNullOrWhiteSpace(value) ? "" : value;
                            break;

                        case "DNI":
                            // Si está vacío, asignar valor por defecto
                            abonado.DNI = string.IsNullOrWhiteSpace(value) ? "00000000A" : value;
                            break;

                        case "Telefono":
                            abonado.Telefono = value;
                            break;

                        case "Email":
                            abonado.Email = value;
                            break;

                        case "Direccion":
                            abonado.Direccion = value;
                            break;

                        case "FechaNacimiento":
                            if (TryParseDate(value, out DateTime fecha))
                                abonado.FechaNacimiento = fecha;
                            break;

                        case "TallaCamiseta":
                            abonado.TallaCamiseta = ParseTallaCamiseta(value);
                            break;

                        case "Observaciones":
                            abonado.Observaciones = value;
                            break;

                        case "Estado":
                            abonado.Estado = ParseEstadoAbonado(value);
                            break;

                        case "Gestor":
                            if (gestores.TryGetValue(value, out Gestor? gestor))
                                abonado.GestorId = gestor.Id;
                            break;

                        case "Peña":
                            if (peñas.TryGetValue(value, out Peña? peña))
                                abonado.PeñaId = peña.Id;
                            break;

                        case "TipoAbono":
                            if (tiposAbono.TryGetValue(value, out TipoAbono? tipoAbono))
                                abonado.TipoAbonoId = tipoAbono.Id;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al mapear campo {fieldMapping.AbonadoField}: {ex.Message}");
                }
            }

            // Generar código de barras si no existe
            if (string.IsNullOrEmpty(abonado.CodigoBarras))
            {
                abonado.CodigoBarras = GenerateBarcode(abonado.NumeroSocio);
            }

            return abonado;
        }

        private string ParseTallaCamiseta(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Sin especificar";

            var normalizedValue = value.Trim().ToUpper();

            // Mapear diferentes formatos a las tallas válidas de la BD
            return normalizedValue switch
            {
                // Tallas exactas
                "XS" => "XS",
                "S" => "S",
                "M" => "M",
                "L" => "L",
                "XL" => "XL",
                "XXL" => "XXL",
                "3XL" => "3XL",
                "4XL" => "4XL",
                "5XL" => "5XL",
                "6XL" => "6XL",

                // Variaciones comunes
                "EXTRA SMALL" or "EXTRA-SMALL" or "EXTRASMALL" => "XS",
                "SMALL" => "S",
                "MEDIUM" => "M",
                "LARGE" => "L",
                "EXTRA LARGE" or "EXTRA-LARGE" or "EXTRALARGE" => "XL",
                "EXTRA EXTRA LARGE" or "XX-LARGE" or "XXLARGE" => "XXL",
                "2XL" => "XXL",
                "3X" => "3XL",
                "4X" => "4XL",
                "5X" => "5XL",
                "6X" => "6XL",

                // Formatos numéricos europeos
                "34" or "36" => "XS",
                "38" or "40" => "S",
                "42" or "44" => "M",
                "46" or "48" => "L",
                "50" or "52" => "XL",
                "54" or "56" => "XXL",
                "58" or "60" => "3XL",
                "62" or "64" => "4XL",
                "66" or "68" => "5XL",
                "70" or "72" => "6XL",

                // Otros formatos
                "N/A" or "NA" or "NO" or "NINGUNA" or "NINGUNO" or "-" or "" => "Sin especificar",

                // Si no coincide con ninguno, devolver tal como viene pero validar que esté en la lista
                _ => ValidateTalla(normalizedValue)
            };
        }

        private string ValidateTalla(string talla)
        {
            var tallasValidas = new[] { "Sin especificar", "XS", "S", "M", "L", "XL", "XXL", "3XL", "4XL", "5XL", "6XL" };

            // Buscar coincidencia exacta (case insensitive)
            var tallaEncontrada = tallasValidas.FirstOrDefault(t =>
                string.Equals(t, talla, StringComparison.OrdinalIgnoreCase));

            if (tallaEncontrada != null)
                return tallaEncontrada;

            // Si no se encuentra, devolver "Sin especificar"
            System.Diagnostics.Debug.WriteLine($"Talla no reconocida: '{talla}', asignando 'Sin especificar'");
            return "Sin especificar";
        }

        private EstadoAbonado ParseEstadoAbonado(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return EstadoAbonado.Activo; // Por defecto activo

            var normalizedValue = value.Trim().ToLower();

            // Valores para Activo (basado en el enum que solo tiene Activo=1, Inactivo=0)
            if (normalizedValue == "activo" ||
                normalizedValue == "active" ||
                normalizedValue == "si" ||
                normalizedValue == "sí" ||
                normalizedValue == "yes" ||
                normalizedValue == "true" ||
                normalizedValue == "1" ||
                normalizedValue == "habilitado" ||
                normalizedValue == "enabled")
            {
                return EstadoAbonado.Activo;
            }

            // Valores para Inactivo
            if (normalizedValue == "inactivo" ||
                normalizedValue == "inactive" ||
                normalizedValue == "no" ||
                normalizedValue == "false" ||
                normalizedValue == "0" ||
                normalizedValue == "deshabilitado" ||
                normalizedValue == "disabled" ||
                normalizedValue == "suspendido" ||
                normalizedValue == "suspended" ||
                normalizedValue == "bloqueado" ||
                normalizedValue == "blocked" ||
                normalizedValue == "baja" ||
                normalizedValue == "deleted" ||
                normalizedValue == "eliminado" ||
                normalizedValue == "removed")
            {
                return EstadoAbonado.Inactivo;
            }

            // Si no reconoce el valor, intentar parsear como enum
            if (Enum.TryParse<EstadoAbonado>(value, true, out EstadoAbonado estado))
                return estado;

            // Por defecto, activo
            return EstadoAbonado.Activo;
        }

        private bool TryParseDate(string dateString, out DateTime date)
        {
            date = default;

            if (string.IsNullOrWhiteSpace(dateString))
                return false;

            var formats = new[]
            {
                "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
                "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "M/d/yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    return true;
            }

            return DateTime.TryParse(dateString, out date);
        }

        private ValidationResult ValidateAbonado(Abonado abonado, HashSet<int> existingNumeros, bool validateUnique)
        {
            // Campos obligatorios para la importación (solo Nombre y NumeroSocio)
            if (abonado.NumeroSocio <= 0)
                return new ValidationResult { IsValid = false, ErrorMessage = "Número de socio es obligatorio y debe ser mayor a 0" };

            if (string.IsNullOrWhiteSpace(abonado.Nombre))
                return new ValidationResult { IsValid = false, ErrorMessage = "El nombre es obligatorio" };

            // Apellidos no es obligatorio para la importación, pero si está vacío asignar string vacío para la BD
            if (string.IsNullOrWhiteSpace(abonado.Apellidos))
                abonado.Apellidos = "";

            // Validar unicidad
            if (validateUnique && existingNumeros.Contains(abonado.NumeroSocio))
                return new ValidationResult { IsValid = false, ErrorMessage = $"El número de socio {abonado.NumeroSocio} ya existe" };

            return new ValidationResult { IsValid = true };
        }

        private string GenerateBarcode(int numeroSocio)
        {
            return $"SOC{numeroSocio:D6}";
        }

        private async Task LogImportAction(CsvImportResult result)
        {
            try
            {
                if (_dbContext == null || UserSession.Instance.CurrentUser == null)
                    return;

                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser.Id,
                    Accion = $"Importación CSV completada",
                    TipoAccion = "Importación",
                    FechaHora = DateTime.Now,
                    Detalles = $"Éxitos: {result.SuccessCount}, Errores: {result.ErrorCount}"
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al registrar historial: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dbContext?.Dispose();
                _disposed = true;
            }
        }
    }

    #region Data Transfer Objects

    public class CsvImportPreview
    {
        public string FilePath { get; set; } = "";
        public List<string> Headers { get; set; } = new();
        public List<string[]> SampleRows { get; set; } = new();
        public int TotalRows { get; set; }
        public char Delimiter { get; set; }
    }

    public class CsvImportMapping
    {
        public string FilePath { get; set; } = "";
        public char Delimiter { get; set; }
        public bool HasHeaders { get; set; } = true;
        public bool ValidateUnique { get; set; } = true;
        public List<CsvFieldMapping> FieldMappings { get; set; } = new();
    }

    public class CsvFieldMapping
    {
        public string AbonadoField { get; set; } = "";
        public int CsvColumnIndex { get; set; } = -1;
        public string CsvColumnName { get; set; } = "";
    }

    public class CsvImportResult
    {
        public bool IsSuccess { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public string Summary => $"Importados: {SuccessCount}, Errores: {ErrorCount}";
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    #endregion
}