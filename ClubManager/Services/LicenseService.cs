using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClubManager.Models;

namespace ClubManager.Services
{
    public interface ILicenseService
    {
        LicenseInfo ValidateLicense(string licenseKey);
        LicenseInfo GetCurrentLicenseInfo();
        bool SetLicenseKey(string licenseKey);
        void ClearLicense();
        void LoadSavedLicense();
    }

    public class LicenseService : ILicenseService
    {
        private LicenseInfo? _currentLicense;
        private readonly string[] _publicKeyPaths;

        public LicenseService()
        {
            // Buscar la clave pública en múltiples ubicaciones
            _publicKeyPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public_key.xml"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ClubManager", "public_key.xml"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "public_key.xml")
            };
        }

        public LicenseInfo ValidateLicense(string licenseKey)
        {
            var licenseInfo = new LicenseInfo
            {
                IsValid = false,
                IsExpired = true,
                ErrorMessage = "Licencia inválida"
            };

            try
            {
                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    licenseInfo.ErrorMessage = "La clave de licencia no puede estar vacía";
                    return licenseInfo;
                }

                // Buscar la clave pública en múltiples ubicaciones
                string? publicKeyPath = FindPublicKey();
                if (publicKeyPath == null)
                {
                    licenseInfo.ErrorMessage = "No se encontró la clave pública.\n\n" +
                                             "Asegúrate de que el archivo 'public_key.xml' esté en:\n" +
                                             "- La carpeta del programa\n" +
                                             "- Documentos/ClubManager/\n" +
                                             "- El escritorio\n\n" +
                                             "O ejecuta primero el Generador de Claves.";
                    return licenseInfo;
                }

                // Decodificar la licencia desde Base64
                byte[] licenseBytes;
                try
                {
                    licenseBytes = Convert.FromBase64String(licenseKey);
                }
                catch
                {
                    licenseInfo.ErrorMessage = "Formato de licencia inválido";
                    return licenseInfo;
                }

                string licenseJson = Encoding.UTF8.GetString(licenseBytes);
                SignedLicense? signedLicense = JsonSerializer.Deserialize<SignedLicense>(licenseJson);

                if (signedLicense == null || string.IsNullOrEmpty(signedLicense.Data) || string.IsNullOrEmpty(signedLicense.Signature))
                {
                    licenseInfo.ErrorMessage = "Estructura de licencia inválida";
                    return licenseInfo;
                }

                // Verificar la firma RSA
                if (!VerifySignature(signedLicense.Data, signedLicense.Signature, publicKeyPath))
                {
                    licenseInfo.ErrorMessage = "Firma de licencia inválida";
                    return licenseInfo;
                }

                // Decodificar los datos de la licencia
                byte[] dataBytes = Convert.FromBase64String(signedLicense.Data);
                string dataJson = Encoding.UTF8.GetString(dataBytes);
                LicenseData? licenseData = JsonSerializer.Deserialize<LicenseData>(dataJson);

                if (licenseData == null)
                {
                    licenseInfo.ErrorMessage = "Datos de licencia inválidos";
                    return licenseInfo;
                }

                // Verificar fecha de expiración
                bool isExpired = DateTime.Now > licenseData.ExpirationDate;

                licenseInfo.IsValid = true;
                licenseInfo.IsExpired = isExpired;
                licenseInfo.ExpirationDate = licenseData.ExpirationDate;
                licenseInfo.ClubName = licenseData.ClubName;
                licenseInfo.ErrorMessage = isExpired ? "La licencia ha expirado" : "Licencia válida";

                return licenseInfo;
            }
            catch (Exception ex)
            {
                licenseInfo.ErrorMessage = $"Error al validar la licencia: {ex.Message}";
                return licenseInfo;
            }
        }

        private string? FindPublicKey()
        {
            foreach (string path in _publicKeyPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }

        private bool VerifySignature(string data, string signature, string publicKeyPath)
        {
            try
            {
                string publicKeyXml = File.ReadAllText(publicKeyPath);
                using var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKeyXml);

                byte[] dataBytes = Convert.FromBase64String(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);

                return rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA256"), signatureBytes);
            }
            catch
            {
                return false;
            }
        }

        public LicenseInfo GetCurrentLicenseInfo()
        {
            return _currentLicense ?? new LicenseInfo
            {
                IsValid = false,
                IsExpired = true,
                ErrorMessage = "No hay licencia configurada"
            };
        }

        public bool SetLicenseKey(string licenseKey)
        {
            var licenseInfo = ValidateLicense(licenseKey);
            _currentLicense = licenseInfo;

            if (licenseInfo.IsValid)
            {
                // Guardar la licencia en un archivo para persistencia
                try
                {
                    string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_license.lic");
                    File.WriteAllText(licensePath, licenseKey);
                }
                catch
                {
                    // Si no se puede guardar, no es crítico
                }
            }

            return licenseInfo.IsValid;
        }

        public void ClearLicense()
        {
            _currentLicense = null;
            try
            {
                string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_license.lic");
                if (File.Exists(licensePath))
                {
                    File.Delete(licensePath);
                }
            }
            catch
            {
                // Si no se puede eliminar, no es crítico
            }
        }

        public void LoadSavedLicense()
        {
            try
            {
                string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_license.lic");
                if (File.Exists(licensePath))
                {
                    string licenseKey = File.ReadAllText(licensePath);
                    if (!string.IsNullOrWhiteSpace(licenseKey))
                    {
                        _currentLicense = ValidateLicense(licenseKey);
                    }
                }
            }
            catch
            {
                // Si hay error cargando, simplemente no hay licencia
                _currentLicense = null;
            }
        }
    }
}