using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using ClubManager.Models;
using ClubManager.Data;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.Services
{
    public interface IConfiguracionService
    {
        Configuracion GetConfiguracion();
        void SaveConfiguracion(Configuracion configuracion);
        void ExportConfiguracion(Configuracion configuracion, string filePath);
        Configuracion ImportConfiguracion(string filePath);
    }

    public class ConfiguracionService : IConfiguracionService
    {
        private const string CONFIG_FILE = "config_backup.json";
        private readonly string _configPath;

        public ConfiguracionService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
        }

        public Configuracion GetConfiguracion()
        {
            try
            {
                using var context = new ClubDbContext();

                // Intentar obtener la configuración de la base de datos
                var configuracion = context.Configuracion.FirstOrDefault();

                if (configuracion != null)
                {
                    return configuracion;
                }

                // Si no existe, crear una nueva configuración por defecto
                configuracion = new Configuracion
                {
                    Id = 1,
                    NombreClub = "Mi Club Deportivo",
                    TemporadaActual = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now
                };

                // Guardar la configuración por defecto en la base de datos
                context.Configuracion.Add(configuracion);
                context.SaveChanges();

                return configuracion;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener configuración de BD: {ex.Message}");

                // Fallback: intentar cargar desde archivo JSON si existe
                try
                {
                    if (File.Exists(_configPath))
                    {
                        string json = File.ReadAllText(_configPath);
                        var configFromFile = JsonSerializer.Deserialize<Configuracion>(json);
                        if (configFromFile != null)
                        {
                            configFromFile.Id = 1; // Asegurar que tiene ID
                            return configFromFile;
                        }
                    }
                }
                catch
                {
                    // Ignorar errores del archivo
                }

                // Última opción: configuración por defecto
                return new Configuracion
                {
                    Id = 1,
                    NombreClub = "Mi Club Deportivo",
                    TemporadaActual = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now
                };
            }
        }

        public void SaveConfiguracion(Configuracion configuracion)
        {
            try
            {
                using var context = new ClubDbContext();

                configuracion.FechaModificacion = DateTime.Now;

                // Verificar si ya existe una configuración
                var existingConfig = context.Configuracion.FirstOrDefault();

                if (existingConfig != null)
                {
                    // Actualizar configuración existente
                    existingConfig.NombreClub = configuracion.NombreClub;
                    existingConfig.TemporadaActual = configuracion.TemporadaActual;
                    existingConfig.DireccionClub = configuracion.DireccionClub;
                    existingConfig.TelefonoClub = configuracion.TelefonoClub;
                    existingConfig.EmailClub = configuracion.EmailClub;
                    existingConfig.WebClub = configuracion.WebClub;
                    existingConfig.LogoClub = configuracion.LogoClub;
                    existingConfig.RutaEscudo = configuracion.RutaEscudo;
                    existingConfig.AutoBackup = configuracion.AutoBackup;
                    existingConfig.ConfirmarEliminaciones = configuracion.ConfirmarEliminaciones;
                    existingConfig.MostrarAyudas = configuracion.MostrarAyudas;
                    existingConfig.NumeracionAutomatica = configuracion.NumeracionAutomatica;
                    existingConfig.FormatoNumeroSocio = configuracion.FormatoNumeroSocio;
                    existingConfig.ConfiguracionImpresion = configuracion.ConfiguracionImpresion;
                    existingConfig.FechaModificacion = configuracion.FechaModificacion;
                    existingConfig.Version = configuracion.Version;

                    context.Configuracion.Update(existingConfig);
                }
                else
                {
                    // Crear nueva configuración
                    configuracion.Id = 1;
                    context.Configuracion.Add(configuracion);
                }

                context.SaveChanges();

                // También guardar copia de seguridad en archivo JSON
                try
                {
                    string json = JsonSerializer.Serialize(configuracion, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(_configPath, json);
                }
                catch
                {
                    // No crítico si falla el backup en archivo
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar configuración: {ex.Message}");
            }
        }

        public void ExportConfiguracion(Configuracion configuracion, string filePath)
        {
            try
            {
                string json = JsonSerializer.Serialize(configuracion, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al exportar configuración: {ex.Message}");
            }
        }

        public Configuracion ImportConfiguracion(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var configuracion = JsonSerializer.Deserialize<Configuracion>(json);

                if (configuracion != null)
                {
                    // Asegurar que tiene ID para la base de datos
                    configuracion.Id = 1;
                    configuracion.FechaModificacion = DateTime.Now;
                    return configuracion;
                }

                throw new Exception("El archivo no contiene una configuración válida.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al importar configuración: {ex.Message}");
            }
        }
    }
}