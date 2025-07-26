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
                if (predeterminada == null && plantillas.Any())
                {
                    predeterminada = plantillas.First();
                    predeterminada.EsPredeterminada = true;
                    await GuardarPlantillaAsync(predeterminada);
                }

                return predeterminada;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo plantilla predeterminada: {ex.Message}");

                // Crear plantilla básica si no hay ninguna
                var plantillaBasica = CrearPlantillaBasica();
                await GuardarPlantillaAsync(plantillaBasica);
                return plantillaBasica;
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

                System.Diagnostics.Debug.WriteLine($"Plantilla guardada: {plantilla.Nombre} (Predeterminada: {plantilla.EsPredeterminada})");
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
                    var resultado = await GuardarPlantillaAsync(nuevaPredeterminada);
                    System.Diagnostics.Debug.WriteLine($"Plantilla {nuevaPredeterminada.Nombre} establecida como predeterminada: {resultado}");
                    return resultado;
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
                    throw new ArgumentException($"Plantilla con ID {id} no encontrada");

                var duplicada = original.Clonar(nuevoNombre);
                duplicada.EsPredeterminada = false; // Las copias no son predeterminadas

                await GuardarPlantillaAsync(duplicada);
                return duplicada;
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
                if (errores.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Errores de validación en plantilla {plantilla.Nombre}: {string.Join(", ", errores)}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validando plantilla: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]> ExportarPlantillaAsync(string id)
        {
            try
            {
                var plantilla = await GetPlantillaByIdAsync(id);
                if (plantilla == null)
                    throw new ArgumentException($"Plantilla con ID {id} no encontrada");

                var json = JsonSerializer.Serialize(plantilla, new JsonSerializerOptions { WriteIndented = true });
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

                if (plantilla != null)
                {
                    // Generar nuevo ID para evitar conflictos
                    plantilla.Id = Guid.NewGuid().ToString();
                    plantilla.EsPredeterminada = false;
                    plantilla.FechaCreacion = DateTime.Now;
                    plantilla.FechaModificacion = DateTime.Now;

                    await GuardarPlantillaAsync(plantilla);
                }

                return plantilla;
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
                return JsonSerializer.Deserialize<List<EstadisticasPlantilla>>(json) ?? new List<EstadisticasPlantilla>();
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
                        FechaCreacion = plantilla?.FechaCreacion ?? DateTime.Now
                    };
                    estadisticas.Add(estadistica);
                }

                switch (accion)
                {
                    case "Guardada":
                        estadistica.UltimaUtilizacion = DateTime.Now;
                        break;
                    case "Utilizada":
                        estadistica.VecesUtilizada++;
                        estadistica.UltimaUtilizacion = DateTime.Now;
                        break;
                    case "Impresa":
                        estadistica.TarjetasImpresas++;
                        estadistica.VecesUtilizada++;
                        estadistica.UltimaUtilizacion = DateTime.Now;
                        break;
                    case "Eliminada":
                        estadistica.EstaActiva = false;
                        break;
                }

                var json = JsonSerializer.Serialize(estadisticas, new JsonSerializerOptions { WriteIndented = true });
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
                if (!plantillas.Any())
                {
                    // Crear plantilla básica por defecto
                    var plantillaBasica = CrearPlantillaBasica();
                    await GuardarPlantillaAsync(plantillaBasica);

                    System.Diagnostics.Debug.WriteLine("Plantilla básica creada automáticamente");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando plantillas por defecto: {ex.Message}");
            }
        }

        private PlantillaTarjeta CrearPlantillaBasica()
        {
            return new PlantillaTarjeta
            {
                Id = Guid.NewGuid().ToString(),
                Nombre = "Plantilla Básica",
                Descripcion = "Plantilla básica creada automáticamente",
                Ancho = 350,
                Alto = 220,
                EsPredeterminada = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now,
                Elementos = new List<ElementoTarjeta>
                {
                    new ElementoTexto
                    {
                        Id = Guid.NewGuid().ToString(),
                        Tipo = "Texto",
                        X = 20,
                        Y = 20,
                        Ancho = 310,
                        Alto = 30,
                        ZIndex = 1,
                        Texto = "CLUB DEPORTIVO",
                        FontFamily = "Arial",
                        FontSize = 18,
                        IsBold = true,
                        Color = Colors.DarkBlue,
                        TextAlignment = System.Windows.TextAlignment.Center
                    },
                    new ElementoCampoDinamico
                    {
                        Id = Guid.NewGuid().ToString(),
                        Tipo = "Campo Dinámico",
                        X = 20,
                        Y = 60,
                        Ancho = 200,
                        Alto = 25,
                        ZIndex = 2,
                        CampoOrigen = "NombreCompleto",
                        FontFamily = "Arial",
                        FontSize = 14,
                        IsBold = true,
                        Color = Colors.Black
                    },
                    new ElementoCampoDinamico
                    {
                        Id = Guid.NewGuid().ToString(),
                        Tipo = "Campo Dinámico",
                        X = 20,
                        Y = 90,
                        Ancho = 150,
                        Alto = 20,
                        ZIndex = 3,
                        CampoOrigen = "NumeroSocio",
                        Prefijo = "Socio Nº: ",
                        FontFamily = "Arial",
                        FontSize = 12,
                        Color = Colors.DarkGray
                    },
                    new ElementoCodigoBarras
                    {
                        Id = Guid.NewGuid().ToString(),
                        Tipo = "Código de Barras",
                        X = 20,
                        Y = 130,
                        Ancho = 200,
                        Alto = 60,
                        ZIndex = 4,
                        CampoOrigen = "CodigoBarras",
                        TipoCodigo = "Code128",
                        MostrarTexto = true,
                        FontSize = 8,
                        ColorTexto = Colors.Black,
                        ColorFondo = Colors.White
                    }
                }
            };
        }

        // Método público para crear una plantilla predeterminada si es necesario
        public async Task<PlantillaTarjeta> CrearYGuardarPlantillaBasicaAsync()
        {
            var plantilla = CrearPlantillaBasica();
            await GuardarPlantillaAsync(plantilla);
            return plantilla;
        }
    }
}