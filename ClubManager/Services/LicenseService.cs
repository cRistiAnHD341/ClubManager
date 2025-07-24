using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClubManager.Models;

namespace ClubManager.Services
{
    public class LicenseService : ILicenseService
    {
        private const string LICENSE_FILE = "club.license";
        private const string PUBLIC_KEY_FILE = "public_key.xml";
        private LicenseInfo? _currentLicense;
        private RSACryptoServiceProvider? _rsa;

        public LicenseService()
        {
            InitializeRSA();
        }

        public LicenseInfo GetCurrentLicenseInfo()
        {
            if (_currentLicense == null)
            {
                LoadSavedLicense();
            }

            return _currentLicense ?? new LicenseInfo
            {
                IsValid = false,
                IsExpired = true,
                Status = "Sin licencia",
                ErrorMessage = "No hay licencia instalada"
            };
        }

        public bool ActivateLicense(string licenseKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    _currentLicense = new LicenseInfo
                    {
                        IsValid = false,
                        Status = "Clave vacía",
                        ErrorMessage = "La clave de licencia no puede estar vacía"
                    };
                    return false;
                }

                // Intentar validar con el nuevo formato RSA
                var licenseData = ValidateRSALicense(licenseKey);
                if (licenseData != null)
                {
                    return ProcessValidLicense(licenseData, licenseKey);
                }

                // Si falla RSA, intentar el formato antiguo (compatibilidad)
                licenseData = ValidateLegacyLicense(licenseKey);
                if (licenseData != null)
                {
                    return ProcessValidLicense(licenseData, licenseKey);
                }

                // Si ningún formato funciona
                _currentLicense = new LicenseInfo
                {
                    IsValid = false,
                    Status = "Formato inválido",
                    ErrorMessage = "La clave de licencia tiene un formato inválido o no se pudo verificar"
                };
                return false;
            }
            catch (Exception ex)
            {
                _currentLicense = new LicenseInfo
                {
                    IsValid = false,
                    Status = "Error",
                    ErrorMessage = $"Error al procesar licencia: {ex.Message}"
                };
                return false;
            }
        }

        public void LoadSavedLicense()
        {
            try
            {
                string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LICENSE_FILE);

                if (!File.Exists(licensePath))
                {
                    _currentLicense = new LicenseInfo
                    {
                        IsValid = false,
                        Status = "No instalada",
                        ErrorMessage = "No hay licencia instalada"
                    };
                    return;
                }

                string licenseKey = File.ReadAllText(licensePath);
                ActivateLicense(licenseKey);
            }
            catch (Exception ex)
            {
                _currentLicense = new LicenseInfo
                {
                    IsValid = false,
                    Status = "Error de carga",
                    ErrorMessage = $"Error al cargar licencia: {ex.Message}"
                };
            }
        }

        private void InitializeRSA()
        {
            try
            {
                _rsa = new RSACryptoServiceProvider();

                // Buscar clave pública en varias ubicaciones
                string publicKeyXml = FindPublicKey();

                if (!string.IsNullOrEmpty(publicKeyXml))
                {
                    _rsa.FromXmlString(publicKeyXml);
                    System.Diagnostics.Debug.WriteLine("RSA inicializado correctamente con clave pública");
                }
                else
                {
                    // Sin clave pública, solo funcionará el modo legacy
                    _rsa?.Dispose();
                    _rsa = null;
                    System.Diagnostics.Debug.WriteLine("No se encontró clave pública, solo modo legacy disponible");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando RSA: {ex.Message}");
                _rsa?.Dispose();
                _rsa = null;
            }
        }

        private string FindPublicKey()
        {
            // Lista de ubicaciones donde buscar
            var locations = new[]
            {
                // Carpeta de la aplicación
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PUBLIC_KEY_FILE),
                // Documentos/ClubManager
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ClubManager", PUBLIC_KEY_FILE),
                // %AppData%/ClubManager
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClubManager", PUBLIC_KEY_FILE)
            };

            foreach (var location in locations)
            {
                try
                {
                    if (File.Exists(location))
                    {
                        string content = File.ReadAllText(location);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            System.Diagnostics.Debug.WriteLine($"Clave pública encontrada en: {location}");
                            return content;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error leyendo {location}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine("No se encontró clave pública en ninguna ubicación");
            return string.Empty;
        }

        private LicenseData? ValidateRSALicense(string licenseKey)
        {
            try
            {
                if (_rsa == null)
                {
                    // Debug: RSA no inicializado
                    System.Diagnostics.Debug.WriteLine("RSA no inicializado - clave pública no encontrada");
                    return null;
                }

                // Limpiar formato
                licenseKey = licenseKey.Replace("-", "").Replace(" ", "").Replace("\n", "").Replace("\r", "").Trim();

                // Debug
                System.Diagnostics.Debug.WriteLine($"Validando licencia RSA, longitud: {licenseKey.Length}");

                // Decodificar la licencia base64
                byte[] licenseBytes = Convert.FromBase64String(licenseKey);
                string licenseJson = Encoding.UTF8.GetString(licenseBytes);

                // Debug
                System.Diagnostics.Debug.WriteLine($"JSON decodificado: {licenseJson}");

                // Deserializar la estructura firmada
                var signedLicense = JsonSerializer.Deserialize<SignedLicense>(licenseJson);
                if (signedLicense == null)
                {
                    System.Diagnostics.Debug.WriteLine("Error: No se pudo deserializar SignedLicense");
                    return null;
                }

                // Verificar la firma RSA
                byte[] dataBytes = Convert.FromBase64String(signedLicense.Data);
                byte[] signature = Convert.FromBase64String(signedLicense.Signature);

                // Debug
                System.Diagnostics.Debug.WriteLine($"Datos: {dataBytes.Length} bytes, Firma: {signature.Length} bytes");

                bool isValidSignature = _rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA256"), signature);

                System.Diagnostics.Debug.WriteLine($"Firma válida: {isValidSignature}");

                if (!isValidSignature)
                    return null;

                // Deserializar los datos de la licencia
                string licenseDataJson = Encoding.UTF8.GetString(dataBytes);
                System.Diagnostics.Debug.WriteLine($"JSON de datos de licencia: {licenseDataJson}");

                var licenseData = JsonSerializer.Deserialize<GeneratorLicenseData>(licenseDataJson);

                if (licenseData == null)
                {
                    System.Diagnostics.Debug.WriteLine("Error: No se pudo deserializar GeneratorLicenseData");
                    return null;
                }

                // Convertir al formato interno
                return new LicenseData
                {
                    ClubName = licenseData.ClubName ?? "",
                    LicenseId = Guid.NewGuid().ToString(), // Generar ID único
                    ExpirationDate = licenseData.ExpirationDate,
                    Signature = "RSA_VERIFIED" // Marcar como verificado por RSA
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ValidateRSALicense: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private LicenseData? ValidateLegacyLicense(string licenseKey)
        {
            try
            {
                // Limpiar formato
                licenseKey = licenseKey.Replace("-", "").Replace(" ", "").Replace("\n", "").Replace("\r", "");

                // Verificar longitud mínima
                if (licenseKey.Length < 50)
                    return null;

                // Decodificar base64
                byte[] data = Convert.FromBase64String(licenseKey);
                string json = Encoding.UTF8.GetString(data);

                // Deserializar JSON
                var licenseData = JsonSerializer.Deserialize<LegacyLicenseData>(json);
                if (licenseData == null)
                    return null;

                // Verificar firma legacy
                if (!VerifyLegacySignature(licenseData))
                    return null;

                // Convertir al formato interno
                return new LicenseData
                {
                    ClubName = licenseData.ClubName,
                    LicenseId = licenseData.LicenseId,
                    ExpirationDate = licenseData.ExpirationDate,
                    Signature = licenseData.Signature
                };
            }
            catch
            {
                return null;
            }
        }

        private bool VerifyLegacySignature(LegacyLicenseData licenseData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licenseData.ClubName) ||
                    string.IsNullOrWhiteSpace(licenseData.LicenseId))
                    return false;

                string dataToHash = $"{licenseData.ClubName}|{licenseData.LicenseId}|{licenseData.ExpirationDate?.ToString("yyyy-MM-dd")}";
                string calculatedHash = ComputeSimpleHash(dataToHash);

                return calculatedHash == licenseData.Signature;
            }
            catch
            {
                return false;
            }
        }

        private string ComputeSimpleHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input + "ClubManagerSecretKey"));
                return Convert.ToBase64String(hashedBytes).Substring(0, 16);
            }
        }

        private bool ProcessValidLicense(LicenseData licenseData, string licenseKey)
        {
            // Verificar que no esté expirada
            var isExpired = licenseData.ExpirationDate.HasValue &&
                           licenseData.ExpirationDate.Value < DateTime.Now; // Usar DateTime.Now para incluir hora

            _currentLicense = new LicenseInfo
            {
                IsValid = !isExpired,
                IsExpired = isExpired,
                ClubName = licenseData.ClubName,
                ExpirationDate = licenseData.ExpirationDate,
                Status = isExpired ? "Expirada" : "Válida",
                ErrorMessage = isExpired ? "La licencia ha expirado" : ""
            };

            // Guardar licencia si es válida
            if (_currentLicense.IsValid)
            {
                SaveLicense(licenseKey);
            }

            return _currentLicense.IsValid;
        }

        private void SaveLicense(string licenseKey)
        {
            try
            {
                string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LICENSE_FILE);
                File.WriteAllText(licensePath, licenseKey);
            }
            catch
            {
                // Si no se puede guardar, no es crítico
            }
        }

        public void Dispose()
        {
            _rsa?.Dispose();
        }

        #region Data Classes

        private class LicenseData
        {
            public string ClubName { get; set; } = "";
            public string LicenseId { get; set; } = "";
            public DateTime? ExpirationDate { get; set; }
            public string Signature { get; set; } = "";
        }

        private class LegacyLicenseData
        {
            public string ClubName { get; set; } = "";
            public string LicenseId { get; set; } = "";
            public DateTime? ExpirationDate { get; set; }
            public string Signature { get; set; } = "";
        }

        private class GeneratorLicenseData
        {
            public string ClubName { get; set; } = "";
            public DateTime ExpirationDate { get; set; }
            public DateTime GenerationDate { get; set; }
            public string Version { get; set; } = "";
        }

        private class SignedLicense
        {
            public string Data { get; set; } = "";
            public string Signature { get; set; } = "";
        }

        #endregion
    }
}