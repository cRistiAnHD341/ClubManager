using System.IO;
using System.Text.Json;
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
                System.Diagnostics.Debug.WriteLine("Buscando plantilla predeterminada...");

                var plantillas = await GetPlantillasAsync();
                var predeterminada = plantillas.FirstOrDefault(p => p.EsPredeterminada);

                if (predeterminada != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Plantilla predeterminada encontrada: {predeterminada.Nombre}");
                    return predeterminada;
                }

                System.Diagnostics.Debug.WriteLine("No hay plantilla predeterminada, buscando cualquier plantilla...");

                // Si no hay predeterminada, usar la primera disponible
                if (plantillas.Any())
                {
                    predeterminada = plantillas.OrderByDescending(p => p.FechaModificacion).First();
                    predeterminada.EsPredeterminada = true;
                    await GuardarPlantillaAsync(predeterminada);
                    System.Diagnostics.Debug.WriteLine($"Plantilla {predeterminada.Nombre} establecida como predeterminada");
                    return predeterminada;
                }

                System.Diagnostics.Debug.WriteLine("No hay plantillas disponibles, creando plantilla básica...");

                // Si no hay plantillas, crear una básica
                var plantillaBasica = await CrearYGuardarPlantillaBasicaAsync();
                System.Diagnostics.Debug.WriteLine($"Plantilla básica creada: {plantillaBasica.Nombre}");
                return plantillaBasica;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo plantilla predeterminada: {ex.Message}");

                // Como último recurso, crear plantilla básica
                try
                {
                    var plantillaEmergencia = await CrearYGuardarPlantillaBasicaAsync();
                    System.Diagnostics.Debug.WriteLine("Plantilla de emergencia creada");
                    return plantillaEmergencia;
                }
                catch (Exception emergencyEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error crítico creando plantilla de emergencia: {emergencyEx.Message}");
                    return null;
                }
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
                System.Diagnostics.Debug.WriteLine($"Estableciendo plantilla {id} como predeterminada...");

                var plantillas = await GetPlantillasAsync();

                // Remover predeterminada actual
                var cambiosRealizados = false;
                foreach (var plantilla in plantillas.Where(p => p.EsPredeterminada))
                {
                    plantilla.EsPredeterminada = false;
                    await GuardarPlantillaAsync(plantilla);
                    cambiosRealizados = true;
                    System.Diagnostics.Debug.WriteLine($"Removida marca predeterminada de: {plantilla.Nombre}");
                }

                // Establecer nueva predeterminada
                var nuevaPredeterminada = plantillas.FirstOrDefault(p => p.Id == id);
                if (nuevaPredeterminada != null)
                {
                    nuevaPredeterminada.EsPredeterminada = true;
                    var resultado = await GuardarPlantillaAsync(nuevaPredeterminada);

                    if (resultado)
                    {
                        System.Diagnostics.Debug.WriteLine($"Plantilla {nuevaPredeterminada.Nombre} establecida como predeterminada exitosamente");

                        // Actualizar estadísticas
                        await ActualizarEstadisticasAsync(id, "Establecida como predeterminada");

                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Error guardando plantilla {nuevaPredeterminada.Nombre} como predeterminada");
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No se encontró plantilla con ID: {id}");
                    return false;
                }
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
            var plantilla = new PlantillaTarjeta
            {
                Id = Guid.NewGuid().ToString(),
                Nombre = "Plantilla Predeterminada",
                Descripcion = "Plantilla predeterminada del sistema con elementos básicos",
                Ancho = 350,
                Alto = 220,
                EsPredeterminada = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now,
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
                IsBold = true,
                Color = Colors.DarkBlue,
                TextAlignment = System.Windows.TextAlignment.Center
            },
            
            // Nombre completo del abonado
            new ElementoCampoDinamico
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = "Campo Dinámico",
                X = 20,
                Y = 50,
                Ancho = 200,
                Alto = 22,
                ZIndex = 2,
                CampoOrigen = "NombreCompleto",
                FontFamily = "Arial",
                FontSize = 14,
                IsBold = true,
                Color = Colors.Black,
                TextAlignment = System.Windows.TextAlignment.Left,
                Prefijo = "",
                Sufijo = "",
                TextoSiVacio = "NOMBRE NO DISPONIBLE"
            },
            
            // Número de socio
            new ElementoCampoDinamico
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = "Campo Dinámico",
                X = 20,
                Y = 78,
                Ancho = 150,
                Alto = 18,
                ZIndex = 3,
                CampoOrigen = "NumeroSocio",
                Prefijo = "Socio Nº: ",
                Sufijo = "",
                FontFamily = "Arial",
                FontSize = 12,
                IsBold = false,
                Color = Colors.DarkGray,
                TextAlignment = System.Windows.TextAlignment.Left,
                TextoSiVacio = "N/A"
            },
            
            // DNI
            new ElementoCampoDinamico
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = "Campo Dinámico",
                X = 180,
                Y = 78,
                Ancho = 150,
                Alto = 18,
                ZIndex = 4,
                CampoOrigen = "DNI",
                Prefijo = "DNI: ",
                Sufijo = "",
                FontFamily = "Arial",
                FontSize = 12,
                IsBold = false,
                Color = Colors.DarkGray,
                TextAlignment = System.Windows.TextAlignment.Left,
                TextoSiVacio = "No disponible"
            },
            
            // Tipo de abono
            new ElementoCampoDinamico
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = "Campo Dinámico",
                X = 20,
                Y = 102,
                Ancho = 200,
                Alto = 18,
                ZIndex = 5,
                CampoOrigen = "TipoAbono",
                Prefijo = "Tipo: ",
                Sufijo = "",
                FontFamily = "Arial",
                FontSize = 11,
                IsBold = false,
                Color = Colors.Black,
                TextAlignment = System.Windows.TextAlignment.Left,
                TextoSiVacio = "General"
            },
            
            // Código de barras
            new ElementoCodigoBarras
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = "Código de Barras",
                X = 20,
                Y = 130,
                Ancho = 200,
                Alto = 60,
                ZIndex = 6,
                CampoOrigen = "CodigoBarras",
                TipoCodigo = "Code128",
                MostrarTexto = true,
                FontFamily = "Courier New",
                FontSize = 8,
                ColorTexto = Colors.Black,
                ColorFondo = Colors.White,
                FormatoTexto = ""
            },
            
            // Estado del abonado
            new ElementoCampoDinamico
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = "Campo Dinámico",
                X = 250,
                Y = 50,
                Ancho = 80,
                Alto = 20,
                ZIndex = 7,
                CampoOrigen = "Estado",
                Prefijo = "",
                Sufijo = "",
                FontFamily = "Arial",
                FontSize = 10,
                IsBold = true,
                Color = Colors.Green,
                TextAlignment = System.Windows.TextAlignment.Center,
                TextoSiVacio = "ACTIVO"
            },
            
            // Fecha de validez (footer)
            new ElementoTexto
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = "Texto",
                X = 20,
                Y = 195,
                Ancho = 310,
                Alto = 15,
                ZIndex = 8,
                Texto = $"Válida temporada {DateTime.Now.Year}/{DateTime.Now.Year + 1}",
                FontFamily = "Arial",
                FontSize = 9,
                IsBold = false,
                Color = Colors.Gray,
                TextAlignment = System.Windows.TextAlignment.Center
            }
        }
            };

            var guardada = await GuardarPlantillaAsync(plantilla);
            if (!guardada)
            {
                throw new Exception("No se pudo guardar la plantilla básica");
            }

            System.Diagnostics.Debug.WriteLine($"Plantilla básica creada y guardada: {plantilla.Nombre}");
            return plantilla;
        }
        public async Task<bool> ValidarIntegridadPlantillasAsync()
        {
            try
            {
                var plantillas = await GetPlantillasAsync();

                // Verificar que hay al menos una plantilla
                if (!plantillas.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No hay plantillas disponibles, creando plantilla básica");
                    await CrearYGuardarPlantillaBasicaAsync();
                    return true;
                }

                // Verificar que hay una plantilla predeterminada
                var predeterminadas = plantillas.Where(p => p.EsPredeterminada).ToList();

                if (!predeterminadas.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No hay plantilla predeterminada, estableciendo la primera como predeterminada");
                    var primera = plantillas.OrderByDescending(p => p.FechaModificacion).First();
                    await EstablecerPredeterminadaAsync(primera.Id);
                    return true;
                }

                if (predeterminadas.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"Hay {predeterminadas.Count} plantillas predeterminadas, corrigiendo...");

                    // Dejar solo la más reciente como predeterminada
                    var masReciente = predeterminadas.OrderByDescending(p => p.FechaModificacion).First();

                    foreach (var plantilla in predeterminadas.Where(p => p.Id != masReciente.Id))
                    {
                        plantilla.EsPredeterminada = false;
                        await GuardarPlantillaAsync(plantilla);
                    }

                    System.Diagnostics.Debug.WriteLine($"Plantilla {masReciente.Nombre} mantenida como única predeterminada");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validando integridad de plantillas: {ex.Message}");
                return false;
            }
        }

        // Método para obtener información detallada de la plantilla predeterminada

        public async Task<string> GetInfoPlantillaPredeterminadaAsync()
        {
            try
            {
                var plantilla = await GetPlantillaPredeterminadaAsync();

                if (plantilla == null)
                {
                    return "❌ No hay plantilla predeterminada configurada";
                }

                var info = $"✅ Plantilla Predeterminada Activa:\n\n" +
                           $"📄 Nombre: {plantilla.Nombre}\n" +
                           $"📝 Descripción: {plantilla.Descripcion}\n" +
                           $"📐 Tamaño: {plantilla.Ancho} x {plantilla.Alto} px\n" +
                           $"🔢 Elementos: {plantilla.Elementos.Count}\n" +
                           $"📅 Creada: {plantilla.FechaCreacion:dd/MM/yyyy}\n" +
                           $"🔄 Modificada: {plantilla.FechaModificacion:dd/MM/yyyy HH:mm}";

                return info;
            }
            catch (Exception ex)
            {
                return $"❌ Error obteniendo información: {ex.Message}";
            }
        }
    }
}