using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClubManager.Data;
using ClubManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ClubManager.Services
{
    public interface IExportService
    {
        Task ExportAbonadosToCSV();
        Task ExportTiposAbonoToCSV();
        Task ExportGestoresToCSV();
        Task ExportPeñasToCSV();
        Task ExportUsuariosToCSV();
        Task ExportHistorialToCSV();
        Task ExportCompleteBackup();
        Task ExportCustomReport(string title, string data);
        Task ExportToExcel<T>(IEnumerable<T> data, string filePath);
        Task ExportToCsv<T>(IEnumerable<T> data, string filePath);
        Task ExportToPdf<T>(IEnumerable<T> data, string filePath, string title);
    }

    public class ExportService : IExportService
    {
        private readonly ClubDbContext _dbContext;

        public ExportService()
        {
            _dbContext = new ClubDbContext();
        }

        public async Task ExportToExcel<T>(IEnumerable<T> data, string filePath)
        {
            // Implementación básica - se puede expandir con librerías como EPPlus
            await Task.Run(() =>
            {
                var csv = ConvertToCsv(data);
                File.WriteAllText(filePath.Replace(".xlsx", ".csv"), csv, Encoding.UTF8);
            });
        }

        public async Task ExportToCsv<T>(IEnumerable<T> data, string filePath)
        {
            await Task.Run(() =>
            {
                var csv = ConvertToCsv(data);
                File.WriteAllText(filePath, csv, Encoding.UTF8);
            });
        }

        public async Task ExportToPdf<T>(IEnumerable<T> data, string filePath, string title)
        {
            // Implementación básica - se puede expandir con librerías como iText
            await Task.Run(() =>
            {
                var content = $"{title}\n\n{ConvertToCsv(data)}";
                File.WriteAllText(filePath.Replace(".pdf", ".txt"), content, Encoding.UTF8);
            });
        }

        private string ConvertToCsv<T>(IEnumerable<T> data)
        {
            if (!data.Any()) return "";

            var sb = new StringBuilder();
            var properties = typeof(T).GetProperties();

            // Headers con BOM UTF-8 para Excel
            sb.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

            // Data
            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? "";
                    // Escapar comillas dobles
                    value = value.Replace("\"", "\"\"");
                    return $"\"{value}\"";
                });
                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }

        public async Task ExportAbonadosToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Exportar Abonados",
                    FileName = $"Abonados_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var abonados = await _dbContext.Abonados
                        .Include(a => a.Gestor)
                        .Include(a => a.Peña)
                        .Include(a => a.TipoAbono)
                        .OrderBy(a => a.NumeroSocio)
                        .ToListAsync();

                    var csv = new StringBuilder();

                    // Header con UTF-8 BOM para Excel
                    csv.AppendLine("NumeroSocio,Nombre,Apellidos,DNI,CodigoBarras,Estado,Impreso,Gestor,Peña,TipoAbono,FechaCreacion");

                    foreach (var abonado in abonados)
                    {
                        csv.AppendLine($"{abonado.NumeroSocio}," +
                                     $"\"{EscapeCSV(abonado.Nombre)}\"," +
                                     $"\"{EscapeCSV(abonado.Apellidos)}\"," +
                                     $"{abonado.DNI}," +
                                     $"{abonado.CodigoBarras}," +
                                     $"{abonado.Estado}," +
                                     $"{abonado.Impreso}," +
                                     $"\"{EscapeCSV(abonado.Gestor?.Nombre ?? "")}\"," +
                                     $"\"{EscapeCSV(abonado.Peña?.Nombre ?? "")}\"," +
                                     $"\"{EscapeCSV(abonado.TipoAbono?.Nombre ?? "")}\"," +
                                     $"{abonado.FechaCreacion:yyyy-MM-dd HH:mm:ss}");
                    }

                    // Escribir con UTF-8 BOM para compatibilidad con Excel
                    var encoding = new UTF8Encoding(true);
                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), encoding);

                    await LogExportAction($"Exportados {abonados.Count} abonados a CSV");

                    MessageBox.Show($"Abonados exportados correctamente.\n\n" +
                                  $"Archivo: {Path.GetFileName(saveDialog.FileName)}\n" +
                                  $"Registros: {abonados.Count}\n" +
                                  $"Ubicación: {saveDialog.FileName}",
                                  "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar abonados: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExportTiposAbonoToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Exportar Tipos de Abono",
                    FileName = $"TiposAbono_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var tipos = await _dbContext.TiposAbono
                        .Include(t => t.Abonados)
                        .OrderBy(t => t.Nombre)
                        .ToListAsync();

                    var csv = new StringBuilder();
                    csv.AppendLine("Id,Nombre,Descripcion,Precio,TotalAbonados,AbonadosActivos,AbonadosInactivos,IngresosEstimados,PorcentajeActivacion,FechaCreacion");

                    foreach (var tipo in tipos)
                    {
                        var totalAbonados = tipo.Abonados.Count;
                        var abonadosActivos = tipo.Abonados.Count(a => a.Estado == EstadoAbonado.Activo);
                        var abonadosInactivos = totalAbonados - abonadosActivos;
                        var ingresos = abonadosActivos * tipo.Precio;
                        var porcentajeActivacion = totalAbonados > 0 ? (double)abonadosActivos / totalAbonados * 100 : 0;

                        csv.AppendLine($"{tipo.Id}," +
                                     $"\"{EscapeCSV(tipo.Nombre)}\"," +
                                     $"\"{EscapeCSV(tipo.Descripcion ?? "")}\"," +
                                     $"{tipo.Precio:F2}," +
                                     $"{totalAbonados}," +
                                     $"{abonadosActivos}," +
                                     $"{abonadosInactivos}," +
                                     $"{ingresos:F2}," +
                                     $"{porcentajeActivacion:F2}," +
                                     $"{tipo.FechaCreacion:yyyy-MM-dd HH:mm:ss}");
                    }

                    var encoding = new UTF8Encoding(true);
                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), encoding);

                    await LogExportAction($"Exportados {tipos.Count} tipos de abono a CSV");

                    MessageBox.Show($"Tipos de abono exportados correctamente.\n\n" +
                                  $"Archivo: {Path.GetFileName(saveDialog.FileName)}\n" +
                                  $"Registros: {tipos.Count}",
                                  "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar tipos de abono: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExportGestoresToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Exportar Gestores",
                    FileName = $"Gestores_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var gestores = await _dbContext.Gestores
                        .Include(g => g.Abonados)
                        .OrderBy(g => g.Nombre)
                        .ToListAsync();

                    var csv = new StringBuilder();
                    csv.AppendLine("Id,Nombre,TotalAbonados,AbonadosActivos,AbonadosInactivos,PorcentajeActivacion,FechaCreacion");

                    foreach (var gestor in gestores)
                    {
                        var totalAbonados = gestor.Abonados.Count;
                        var abonadosActivos = gestor.Abonados.Count(a => a.Estado == EstadoAbonado.Activo);
                        var abonadosInactivos = totalAbonados - abonadosActivos;
                        var porcentajeActivacion = totalAbonados > 0 ? (double)abonadosActivos / totalAbonados * 100 : 0;

                        csv.AppendLine($"{gestor.Id}," +
                                     $"\"{EscapeCSV(gestor.Nombre)}\"," +
                                     $"{totalAbonados}," +
                                     $"{abonadosActivos}," +
                                     $"{abonadosInactivos}," +
                                     $"{porcentajeActivacion:F2}," +
                                     $"{gestor.FechaCreacion:yyyy-MM-dd HH:mm:ss}");
                    }

                    var encoding = new UTF8Encoding(true);
                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), encoding);

                    await LogExportAction($"Exportados {gestores.Count} gestores a CSV");

                    MessageBox.Show($"Gestores exportados correctamente.\n\n" +
                                  $"Archivo: {Path.GetFileName(saveDialog.FileName)}\n" +
                                  $"Registros: {gestores.Count}",
                                  "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar gestores: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExportPeñasToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Exportar Peñas",
                    FileName = $"Peñas_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var peñas = await _dbContext.Peñas
                        .Include(p => p.Abonados)
                        .OrderBy(p => p.Nombre)
                        .ToListAsync();

                    var csv = new StringBuilder();
                    csv.AppendLine("Id,Nombre,TotalAbonados,AbonadosActivos,AbonadosInactivos,PorcentajeActivacion");

                    foreach (var peña in peñas)
                    {
                        var totalAbonados = peña.Abonados.Count;
                        var abonadosActivos = peña.Abonados.Count(a => a.Estado == EstadoAbonado.Activo);
                        var abonadosInactivos = totalAbonados - abonadosActivos;
                        var porcentajeActivacion = totalAbonados > 0 ? (double)abonadosActivos / totalAbonados * 100 : 0;

                        csv.AppendLine($"{peña.Id}," +
                                     $"\"{EscapeCSV(peña.Nombre)}\"," +
                                     $"{totalAbonados}," +
                                     $"{abonadosActivos}," +
                                     $"{abonadosInactivos}," +
                                     $"{porcentajeActivacion:F2}");
                    }

                    var encoding = new UTF8Encoding(true);
                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), encoding);

                    await LogExportAction($"Exportadas {peñas.Count} peñas a CSV");

                    MessageBox.Show($"Peñas exportadas correctamente.\n\n" +
                                  $"Archivo: {Path.GetFileName(saveDialog.FileName)}\n" +
                                  $"Registros: {peñas.Count}",
                                  "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar peñas: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExportUsuariosToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Exportar Usuarios",
                    FileName = $"Usuarios_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var usuarios = await _dbContext.Usuarios
                        .Include(u => u.HistorialAcciones)
                        .OrderBy(u => u.NombreUsuario)
                        .ToListAsync();

                    var csv = new StringBuilder();
                    csv.AppendLine("Id,NombreUsuario,NombreCompleto,Email,Rol,Activo,TotalAcciones,UltimoAcceso,FechaCreacion");

                    foreach (var usuario in usuarios)
                    {
                        var totalAcciones = usuario.HistorialAcciones?.Count ?? 0;
                        var ultimoAcceso = usuario.UltimoAcceso?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

                        csv.AppendLine($"{usuario.Id}," +
                                     $"\"{EscapeCSV(usuario.NombreUsuario)}\"," +
                                     $"\"{EscapeCSV(usuario.NombreCompleto ?? "")}\"," +
                                     $"\"{EscapeCSV(usuario.Email ?? "")}\"," +
                                     $"\"{EscapeCSV(usuario.Rol)}\"," +
                                     $"{usuario.Activo}," +
                                     $"{totalAcciones}," +
                                     $"\"{ultimoAcceso}\"," +
                                     $"{usuario.FechaCreacion:yyyy-MM-dd HH:mm:ss}");
                    }

                    var encoding = new UTF8Encoding(true);
                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), encoding);

                    await LogExportAction($"Exportados {usuarios.Count} usuarios a CSV");

                    MessageBox.Show($"Usuarios exportados correctamente.\n\n" +
                                  $"Archivo: {Path.GetFileName(saveDialog.FileName)}\n" +
                                  $"Registros: {usuarios.Count}",
                                  "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar usuarios: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExportHistorialToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Exportar Historial",
                    FileName = $"Historial_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var historial = await _dbContext.HistorialAcciones
                        .Include(h => h.Usuario)
                        .OrderByDescending(h => h.FechaHora)
                        .Take(10000) // Limitar a las últimas 10000 acciones
                        .ToListAsync();

                    var csv = new StringBuilder();
                    csv.AppendLine("Id,Usuario,Accion,TipoAccion,FechaHora,Detalles");

                    foreach (var accion in historial)
                    {
                        csv.AppendLine($"{accion.Id}," +
                                     $"\"{EscapeCSV(accion.Usuario?.NombreUsuario ?? "Desconocido")}\"," +
                                     $"\"{EscapeCSV(accion.Accion)}\"," +
                                     $"\"{EscapeCSV(accion.TipoAccion ?? "")}\"," +
                                     $"{accion.FechaHora:yyyy-MM-dd HH:mm:ss}," +
                                     $"\"{EscapeCSV(accion.Detalles ?? "")}\"");
                    }

                    var encoding = new UTF8Encoding(true);
                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), encoding);

                    await LogExportAction($"Exportadas {historial.Count} acciones del historial a CSV");

                    MessageBox.Show($"Historial exportado correctamente.\n\n" +
                                  $"Archivo: {Path.GetFileName(saveDialog.FileName)}\n" +
                                  $"Registros: {historial.Count}",
                                  "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar historial: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExportCompleteBackup()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "ZIP files (*.zip)|*.zip",
                    Title = "Crear Backup Completo",
                    FileName = $"ClubManager_Backup_Completo_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Crear carpeta temporal
                    var tempFolder = Path.Combine(Path.GetTempPath(), $"ClubManager_Backup_{Guid.NewGuid()}");
                    Directory.CreateDirectory(tempFolder);

                    try
                    {
                        // Exportar base de datos
                        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClubManager.db");
                        var dbBackupPath = Path.Combine(tempFolder, "ClubManager_Database.db");
                        if (File.Exists(dbPath))
                        {
                            File.Copy(dbPath, dbBackupPath);
                        }

                        // Exportar todas las entidades a CSV
                        await ExportEntityToCSVInternal<Abonado>(tempFolder, "abonados.csv");
                        await ExportEntityToCSVInternal<TipoAbono>(tempFolder, "tipos_abono.csv");
                        await ExportEntityToCSVInternal<Gestor>(tempFolder, "gestores.csv");
                        await ExportEntityToCSVInternal<Peña>(tempFolder, "peñas.csv");
                        await ExportEntityToCSVInternal<Usuario>(tempFolder, "usuarios.csv");
                        await ExportEntityToCSVInternal<HistorialAccion>(tempFolder, "historial.csv");

                        // Crear archivo de información del backup
                        var infoFile = Path.Combine(tempFolder, "backup_info.txt");
                        var totalAbonados = await _dbContext.Abonados.CountAsync();
                        var totalUsuarios = await _dbContext.Usuarios.CountAsync();
                        var totalGestores = await _dbContext.Gestores.CountAsync();
                        var totalPeñas = await _dbContext.Peñas.CountAsync();
                        var totalTipos = await _dbContext.TiposAbono.CountAsync();
                        var totalAcciones = await _dbContext.HistorialAcciones.CountAsync();

                        var info = $"ClubManager - Backup Completo\n" +
                                  $"===============================\n\n" +
                                  $"Fecha de creación: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"Versión del sistema: 1.0\n" +
                                  $"Usuario que creó el backup: {UserSession.Instance.CurrentUser?.NombreUsuario ?? "Sistema"}\n\n" +
                                  $"ESTADÍSTICAS DEL BACKUP:\n" +
                                  $"------------------------\n" +
                                  $"Total Abonados: {totalAbonados}\n" +
                                  $"Total Usuarios: {totalUsuarios}\n" +
                                  $"Total Gestores: {totalGestores}\n" +
                                  $"Total Peñas: {totalPeñas}\n" +
                                  $"Total Tipos de Abono: {totalTipos}\n" +
                                  $"Total Acciones en Historial: {totalAcciones}\n\n" +
                                  $"CONTENIDO DEL BACKUP:\n" +
                                  $"--------------------\n" +
                                  $"- ClubManager_Database.db (Base de datos completa)\n" +
                                  $"- abonados.csv (Datos de abonados en CSV)\n" +
                                  $"- tipos_abono.csv (Tipos de abono en CSV)\n" +
                                  $"- gestores.csv (Gestores en CSV)\n" +
                                  $"- peñas.csv (Peñas en CSV)\n" +
                                  $"- usuarios.csv (Usuarios en CSV)\n" +
                                  $"- historial.csv (Historial de acciones en CSV)\n" +
                                  $"- backup_info.txt (Este archivo)\n\n" +
                                  $"INSTRUCCIONES DE RESTAURACIÓN:\n" +
                                  $"------------------------------\n" +
                                  $"1. Extraer todos los archivos del ZIP\n" +
                                  $"2. Reemplazar ClubManager.db con ClubManager_Database.db\n" +
                                  $"3. Reiniciar la aplicación\n" +
                                  $"4. Los archivos CSV pueden importarse individualmente si es necesario\n\n" +
                                  $"¡IMPORTANTE! Guarde este backup en un lugar seguro.";

                        await File.WriteAllTextAsync(infoFile, info, Encoding.UTF8);

                        // Crear archivo README
                        var readmeFile = Path.Combine(tempFolder, "README.txt");
                        var readme = $"BACKUP CLUBMANAGER\n" +
                                   $"==================\n\n" +
                                   $"Este archivo contiene un backup completo del sistema ClubManager.\n\n" +
                                   $"Para restaurar:\n" +
                                   $"1. Cierre ClubManager completamente\n" +
                                   $"2. Haga una copia de seguridad de su base de datos actual\n" +
                                   $"3. Reemplace ClubManager.db con ClubManager_Database.db\n" +
                                   $"4. Reinicie ClubManager\n\n" +
                                   $"Para más información, consulte backup_info.txt";

                        await File.WriteAllTextAsync(readmeFile, readme, Encoding.UTF8);

                        // Crear ZIP
                        ZipFile.CreateFromDirectory(tempFolder, saveDialog.FileName);

                        await LogExportAction($"Creado backup completo del sistema");

                        MessageBox.Show($"Backup completo creado correctamente.\n\n" +
                                      $"Archivo: {Path.GetFileName(saveDialog.FileName)}\n" +
                                      $"Tamaño: {new FileInfo(saveDialog.FileName).Length / 1024 / 1024:F2} MB\n" +
                                      $"Ubicación: {saveDialog.FileName}\n\n" +
                                      $"El backup incluye:\n" +
                                      $"• Base de datos completa\n" +
                                      $"• Todos los datos en formato CSV\n" +
                                      $"• Información detallada del backup\n" +
                                      $"• Instrucciones de restauración",
                                      "Backup Completo Creado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    finally
                    {
                        // Limpiar carpeta temporal
                        if (Directory.Exists(tempFolder))
                        {
                            Directory.Delete(tempFolder, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear backup completo: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExportCustomReport(string title, string data)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
                    Title = $"Exportar {title}",
                    FileName = $"{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var content = $"{title}\n" +
                                 $"Generado el: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"Usuario: {UserSession.Instance.CurrentUser?.NombreUsuario ?? "Sistema"}\n\n" +
                                 $"{data}";

                    await File.WriteAllTextAsync(saveDialog.FileName, content, Encoding.UTF8);

                    await LogExportAction($"Exportado reporte personalizado: {title}");

                    MessageBox.Show($"Reporte exportado correctamente.\n\nArchivo: {saveDialog.FileName}",
                                  "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar reporte: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Private Methods

        private async Task ExportEntityToCSVInternal<T>(string folder, string fileName) where T : class
        {
            try
            {
                var entities = await _dbContext.Set<T>().ToListAsync();
                var filePath = Path.Combine(folder, fileName);

                var csv = new StringBuilder();

                // Obtener propiedades de la entidad
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
                    .ToList();

                // Header
                csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

                // Datos
                foreach (var entity in entities)
                {
                    var values = properties.Select(p =>
                    {
                        var value = p.GetValue(entity);
                        return EscapeCSV(value?.ToString() ?? "");
                    });
                    csv.AppendLine(string.Join(",", values.Select(v => $"\"{v}\"")));
                }

                var encoding = new UTF8Encoding(true);
                await File.WriteAllTextAsync(filePath, csv.ToString(), encoding);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exportando {fileName}: {ex.Message}");
            }
        }

        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(decimal) ||
                   type == typeof(Guid) ||
                   type.IsEnum ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    IsSimpleType(type.GetGenericArguments()[0]));
        }

        private string EscapeCSV(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Escapar comillas dobles duplicándolas
            return text.Replace("\"", "\"\"");
        }

        private async Task LogExportAction(string action)
        {
            try
            {
                var historial = new HistorialAccion
                {
                    UsuarioId = UserSession.Instance.CurrentUser?.Id ?? 1,
                    Accion = action,
                    TipoAccion = "Exportacion",
                    FechaHora = DateTime.Now
                };

                _dbContext.HistorialAcciones.Add(historial);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging export action: {ex.Message}");
            }
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}