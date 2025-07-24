using System;
using System.IO;
using System.Text.Json;
using ClubManager.Models;

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
        private const string CONFIG_FILE = "config.json";
        private readonly string _configPath;

        public ConfiguracionService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
        }

        public Configuracion GetConfiguracion()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<Configuracion>(json) ?? new Configuracion();
                }
            }
            catch
            {
                // Si hay error, retornar configuración por defecto
            }

            return new Configuracion();
        }

        public void SaveConfiguracion(Configuracion configuracion)
        {
            try
            {
                configuracion.FechaModificacion = DateTime.Now;
                string json = JsonSerializer.Serialize(configuracion, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_configPath, json);
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
                return JsonSerializer.Deserialize<Configuracion>(json) ?? new Configuracion();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al importar configuración: {ex.Message}");
            }
        }
    }
}