using ClubManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ClubManager.Services
{
    /// <summary>
    /// Servicio para gestionar plantillas de tarjetas
    /// </summary>
    public interface ITemplateService
    {
        Task<List<PlantillaTarjeta>> GetPlantillasAsync();
        Task<PlantillaTarjeta?> GetPlantillaByIdAsync(string id);
        Task<PlantillaTarjeta?> GetPlantillaPredeterminadaAsync();
        Task<bool> GuardarPlantillaAsync(PlantillaTarjeta plantilla);
        Task<bool> EliminarPlantillaAsync(string id);
        Task<bool> EstablecerPredeterminadaAsync(string id);
        Task<PlantillaTarjeta> DuplicarPlantillaAsync(string id, string nuevoNombre);
        Task<bool> ValidarPlantillaAsync(PlantillaTarjeta plantilla);
        Task<byte[]> ExportarPlantillaAsync(string id);
        Task<PlantillaTarjeta?> ImportarPlantillaAsync(byte[] data);
        Task<List<EstadisticasPlantilla>> GetEstadisticasAsync();
    }

    public class TemplateService : ITemplateService
    {
        private readonly string _templateDirectory;
        private readonly string _configFile;
        private readonly string _statisticsFile;

        public TemplateService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClubManager",
                "Templates"
            );

            _templateDirectory = appDataPath;
            _configFile = Path.Combine(appDataPath, "config.json");
            _statisticsFile = Path.Combine(appDataPath, "statistics.json");

            // Crear directorio si no existe
            if (!Directory.Exists(_templateDirectory))
            {
                Directory.CreateDirectory(_templateDirectory);
            }

            // Crear plantillas por defecto si no existen
            _ = Task.Run(CrearPlantillasPorDefectoAsync);
        }

        public async Task<List<PlantillaTarjeta>> GetPlantillasAsync()
        {
            try
            {
                var plantillas = new List<PlantillaTarjeta>();
                var archivos = Directory.GetFiles(_templateDirectory, "*.cardtemplate");

                foreach (var archivo in archivos)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(archivo);
                        var plantilla = JsonSerializer.Deserialize<PlantillaTarjeta>(json);
                        if (plantilla != null)
                        {
                            plantillas.Add(plantilla);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error cargando plantilla {archivo}: {ex.Message}");
                    }
                }

                return plantillas.OrderByDescending(p => p.FechaModificacion).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo plantillas: {ex.Message}");
                return new List<PlantillaTarjeta>();
            }
        }

        public async Task<PlantillaTarjeta?> GetPlantillaByIdAsync(string id)
        {
            try
            {
                var archivo = Path.Combine(_templateDirectory, $"{id}.cardtemplate");
                if (!File.Exists(archivo))
                    return null;

                var json = await File.ReadAllTextAsync(archivo);
                return JsonSerializer.Deserialize<PlantillaTarjeta>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo plantilla {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<PlantillaTarjeta?> GetPlantillaPredeterminadaAsync()
        {
            try
            {
                var plantillas = await GetPlantillasAsync();
                var predeterminada = plantillas.FirstOrDefault(p => p.EsPredeterminada);

                // Si no hay predeterminada, usar la primera disponible
                return predeterminada ?? plantillas.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo plantilla predeterminada: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> GuardarPlantillaAsync(PlantillaTarjeta plantilla)
        {
            try
            {
                // Validar plantilla
                if (!await ValidarPlantillaAsync(plantilla))
                    return false;

                plantilla.FechaModificacion = DateTime.Now;

                var archivo = Path.Combine(_templateDirectory, $"{plantilla.Id}.cardtemplate");
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(plantilla, options);
                await File.WriteAllTextAsync(archivo, json);

                // Actualizar estadísticas
                await ActualizarEstadisticasAsync(plantilla.Id, "Guardada");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando plantilla: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarPlantillaAsync(string id)
        {
            try
            {
                var archivo = Path.Combine(_templateDirectory, $"{id}.cardtemplate");
                if (File.Exists(archivo))
                {
                    File.Delete(archivo);

                    // Actualizar estadísticas
                    await ActualizarEstadisticasAsync(id, "Eliminada");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error eliminando plantilla: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EstablecerPredeterminadaAsync(string id)
        {
            try
            {
                var plantillas = await GetPlantillasAsync();

                // Remover predeterminada actual
                foreach (var plantilla in plantillas)
                {
                    if (plantilla.EsPredeterminada)
                    {
                        plantilla.EsPredeterminada = false;
                        await GuardarPlantillaAsync(plantilla);
                    }
                }

                // Establecer nueva predeterminada
                var nuevaPredeterminada = plantillas.FirstOrDefault(p => p.Id == id);
                if (nuevaPredeterminada != null)
                {
                    nuevaPredeterminada.EsPredeterminada = true;
                    return await GuardarPlantillaAsync(nuevaPredeterminada);
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error estableciendo predeterminada: {ex.Message}");
                return false;
            }
        }

        public async Task<PlantillaTarjeta> DuplicarPlantillaAsync(string id, string nuevoNombre)
        {
            try
            {
                var original = await GetPlantillaByIdAsync(id);
                if (original == null)
                    throw new ArgumentException("Plantilla no encontrada");

                var duplicada = original.Clonar(nuevoNombre);

                if (await GuardarPlantillaAsync(duplicada))
                {
                    await ActualizarEstadisticasAsync(duplicada.Id, "Duplicada");
                    return duplicada;
                }

                throw new Exception("Error al guardar plantilla duplicada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error duplicando plantilla: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidarPlantillaAsync(PlantillaTarjeta plantilla)
        {
            try
            {
                var errores = plantilla.Validar();
                return errores.Count == 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]> ExportarPlantillaAsync(string id)
        {
            try
            {
                var plantilla = await GetPlantillaByIdAsync(id);
                if (plantilla == null)
                    throw new ArgumentException("Plantilla no encontrada");

                var json = JsonSerializer.Serialize(plantilla, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return System.Text.Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exportando plantilla: {ex.Message}");
                throw;
            }
        }

        public async Task<PlantillaTarjeta?> ImportarPlantillaAsync(byte[] data)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var plantilla = JsonSerializer.Deserialize<PlantillaTarjeta>(json);

                if (plantilla == null)
                    return null;

                // Generar nuevo ID para evitar conflictos
                plantilla.Id = Guid.NewGuid().ToString();
                plantilla.FechaCreacion = DateTime.Now;
                plantilla.FechaModificacion = DateTime.Now;
                plantilla.EsPredeterminada = false;

                if (await GuardarPlantillaAsync(plantilla))
                {
                    await ActualizarEstadisticasAsync(plantilla.Id, "Importada");
                    return plantilla;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importando plantilla: {ex.Message}");
                return null;
            }
        }

        public async Task<List<EstadisticasPlantilla>> GetEstadisticasAsync()
        {
            try
            {
                if (!File.Exists(_statisticsFile))
                    return new List<EstadisticasPlantilla>();

                var json = await File.ReadAllTextAsync(_statisticsFile);
                var estadisticas = JsonSerializer.Deserialize<List<EstadisticasPlantilla>>(json);
                return estadisticas ?? new List<EstadisticasPlantilla>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
                return new List<EstadisticasPlantilla>();
            }
        }

        private async Task ActualizarEstadisticasAsync(string plantillaId, string accion)
        {
            try
            {
                var estadisticas = await GetEstadisticasAsync();
                var estadistica = estadisticas.FirstOrDefault(e => e.PlantillaId == plantillaId);

                if (estadistica == null)
                {
                    var plantilla = await GetPlantillaByIdAsync(plantillaId);
                    estadistica = new EstadisticasPlantilla
                    {
                        PlantillaId = plantillaId,
                        NombrePlantilla = plantilla?.Nombre ?? "Desconocida",
                        FechaCreacion = plantilla?.FechaCreacion ?? DateTime.Now,
                        EstaActiva = true
                    };
                    estadisticas.Add(estadistica);
                }

                estadistica.UltimaUtilizacion = DateTime.Now;

                switch (accion.ToLower())
                {
                    case "utilizada":
                    case "impresa":
                        estadistica.VecesUtilizada++;
                        if (accion.ToLower() == "impresa")
                            estadistica.TarjetasImpresas++;
                        break;
                    case "eliminada":
                        estadistica.EstaActiva = false;
                        break;
                }

                var json = JsonSerializer.Serialize(estadisticas, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_statisticsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando estadísticas: {ex.Message}");
            }
        }

        private async Task CrearPlantillasPorDefectoAsync()
        {
            try
            {
                var plantillas = await GetPlantillasAsync();
                if (plantillas.Any())
                    return; // Ya existen plantillas

                // Crear plantilla básica
                var plantillaBasica = new PlantillaTarjeta
                {
                    Id = Guid.NewGuid().ToString(),
                    Nombre = "Plantilla Básica",
                    Descripcion = "Plantilla básica con información esencial del abonado",
                    Ancho = 350,
                    Alto = 220,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    EsPredeterminada = true,
                    Elementos = new List<ElementoTarjeta>
                    {
                        // Título del club
                        new ElementoTexto
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Texto",
                            X = 20,
                            Y = 15,
                            Ancho = 310,
                            Alto = 25,
                            ZIndex = 1,
                            Texto = "CLUB DEPORTIVO",
                            FontFamily = "Arial",
                            FontSize = 16,
                            Color = Colors.DarkBlue,
                            IsBold = true,
                            TextAlignment = System.Windows.TextAlignment.Center
                        },
                        
                        // Nombre del abonado
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 50,
                            Ancho = 200,
                            Alto = 25,
                            ZIndex = 2,
                            CampoOrigen = "NombreCompleto",
                            Texto = "{NombreCompleto}",
                            FontFamily = "Arial",
                            FontSize = 14,
                            Color = Colors.Black,
                            IsBold = true
                        },
                        
                        // Número de socio
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 80,
                            Ancho = 150,
                            Alto = 20,
                            ZIndex = 3,
                            CampoOrigen = "NumeroSocio",
                            Texto = "Socio N°: {NumeroSocio}",
                            FontFamily = "Arial",
                            FontSize = 12,
                            Color = Colors.Black,
                            Prefijo = "Socio N°: "
                        },
                        
                        // DNI
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 105,
                            Ancho = 150,
                            Alto = 20,
                            ZIndex = 4,
                            CampoOrigen = "DNI",
                            Texto = "DNI: {DNI}",
                            FontFamily = "Arial",
                            FontSize = 12,
                            Color = Colors.Black,
                            Prefijo = "DNI: ",
                            TextoSiVacio = "DNI: No especificado"
                        },
                        
                        // Código de barras
                        new ElementoCodigoBarras
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Código de Barras",
                            X = 180,
                            Y = 140,
                            Ancho = 150,
                            Alto = 60,
                            ZIndex = 5,
                            TipoCodigo = "Code128",
                            CampoOrigen = "CodigoBarras",
                            MostrarTexto = true,
                            FontFamily = "Courier New",
                            FontSize = 8,
                            ColorTexto = Colors.Black,
                            ColorFondo = Colors.White
                        }
                    }
                };

                await GuardarPlantillaAsync(plantillaBasica);

                // Crear plantilla completa
                var plantillaCompleta = new PlantillaTarjeta
                {
                    Id = Guid.NewGuid().ToString(),
                    Nombre = "Plantilla Completa",
                    //Descripción = "Plantilla con toda la información del abonado",
                    Ancho = 350,
                    Alto = 220,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    EsPredeterminada = false,
                    Elementos = new List<ElementoTarjeta>
                    {
                        // Título del club
                        new ElementoTexto
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Texto",
                            X = 20,
                            Y = 10,
                            Ancho = 310,
                            Alto = 20,
                            ZIndex = 1,
                            Texto = "CLUB DEPORTIVO - TEMPORADA 2025",
                            FontFamily = "Arial",
                            FontSize = 12,
                            Color = Colors.DarkBlue,
                            IsBold = true,
                            TextAlignment = System.Windows.TextAlignment.Center
                        },
                        
                        // Nombre del abonado
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 35,
                            Ancho = 200,
                            Alto = 20,
                            ZIndex = 2,
                            CampoOrigen = "NombreCompleto",
                            Texto = "{NombreCompleto}",
                            FontFamily = "Arial",
                            FontSize = 13,
                            Color = Colors.Black,
                            IsBold = true
                        },
                        
                        // Número de socio
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 58,
                            Ancho = 100,
                            Alto = 18,
                            ZIndex = 3,
                            CampoOrigen = "NumeroSocio",
                            Texto = "N°: {NumeroSocio}",
                            FontFamily = "Arial",
                            FontSize = 11,
                            Color = Colors.Black,
                            Prefijo = "N°: "
                        },
                        
                        // DNI
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 125,
                            Y = 58,
                            Ancho = 120,
                            Alto = 18,
                            ZIndex = 4,
                            CampoOrigen = "DNI",
                            Texto = "DNI: {DNI}",
                            FontFamily = "Arial",
                            FontSize = 11,
                            Color = Colors.Black,
                            Prefijo = "DNI: ",
                            TextoSiVacio = "DNI: No especificado"
                        },
                        
                        // Peña
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 78,
                            Ancho = 200,
                            Alto = 18,
                            ZIndex = 5,
                            CampoOrigen = "Peña",
                            Texto = "Peña: {Peña}",
                            FontFamily = "Arial",
                            FontSize = 11,
                            Color = Colors.Black,
                            Prefijo = "Peña: ",
                            TextoSiVacio = "Peña: Sin asignar"
                        },
                        
                        // Tipo de abono
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 98,
                            Ancho = 200,
                            Alto = 18,
                            ZIndex = 6,
                            CampoOrigen = "TipoAbono",
                            Texto = "Abono: {TipoAbono}",
                            FontFamily = "Arial",
                            FontSize = 11,
                            Color = Colors.Black,
                            Prefijo = "Abono: ",
                            TextoSiVacio = "Abono: No especificado"
                        },
                        
                        // Estado
                        new ElementoCampoDinamico
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Campo Dinámico",
                            X = 20,
                            Y = 118,
                            Ancho = 100,
                            Alto = 18,
                            ZIndex = 7,
                            CampoOrigen = "Estado",
                            Texto = "{Estado}",
                            FontFamily = "Arial",
                            FontSize = 11,
                            Color = Colors.Green,
                            IsBold = true
                        },
                        
                        // Código de barras
                        new ElementoCodigoBarras
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Código de Barras",
                            X = 180,
                            Y = 150,
                            Ancho = 150,
                            Alto = 50,
                            ZIndex = 8,
                            TipoCodigo = "Code128",
                            CampoOrigen = "CodigoBarras",
                            MostrarTexto = true,
                            FontFamily = "Courier New",
                            FontSize = 7,
                            ColorTexto = Colors.Black,
                            ColorFondo = Colors.White
                        },
                        
                        // Texto de validez
                        new ElementoTexto
                        {
                            Id = Guid.NewGuid().ToString(),
                            Tipo = "Texto",
                            X = 20,
                            Y = 190,
                            Ancho = 310,
                            Alto = 15,
                            ZIndex = 9,
                            Texto = "Válido para temporada 2025 - Intransferible",
                            FontFamily = "Arial",
                            FontSize = 9,
                            Color = Colors.Gray,
                            IsItalic = true,
                            TextAlignment = System.Windows.TextAlignment.Center
                        }
                    }
                };

                await GuardarPlantillaAsync(plantillaCompleta);

                System.Diagnostics.Debug.WriteLine("Plantillas por defecto creadas exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando plantillas por defecto: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Extensiones para facilitar el trabajo con el servicio de plantillas
    /// </summary>
    public static class TemplateServiceExtensions
    {
        /// <summary>
        /// Obtiene las plantillas más utilizadas
        /// </summary>
        public static async Task<List<PlantillaTarjeta>> GetPlantillasMasUtilizadasAsync(this ITemplateService service, int cantidad = 5)
        {
            try
            {
                var estadisticas = await service.GetEstadisticasAsync();
                var plantillas = await service.GetPlantillasAsync();

                var masUtilizadas = estadisticas
                    .Where(e => e.EstaActiva)
                    .OrderByDescending(e => e.VecesUtilizada)
                    .Take(cantidad)
                    .ToList();

                var resultado = new List<PlantillaTarjeta>();
                foreach (var estadistica in masUtilizadas)
                {
                    var plantilla = plantillas.FirstOrDefault(p => p.Id == estadistica.PlantillaId);
                    if (plantilla != null)
                    {
                        resultado.Add(plantilla);
                    }
                }

                return resultado;
            }
            catch
            {
                return new List<PlantillaTarjeta>();
            }
        }

        /// <summary>
        /// Marca una plantilla como utilizada para estadísticas
        /// </summary>
        public static async Task MarcarComoUtilizadaAsync(this ITemplateService service, string plantillaId)
        {
            try
            {
                // El método ActualizarEstadisticasAsync es privado, pero se puede acceder a través de reflexión
                // o mejor aún, crear un método público en la interfaz
                var templateService = service as TemplateService;
                if (templateService != null)
                {
                    var method = typeof(TemplateService).GetMethod("ActualizarEstadisticasAsync",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        await (Task)method.Invoke(templateService, new object[] { plantillaId, "utilizada" });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marcando plantilla como utilizada: {ex.Message}");
            }
        }
    }
}