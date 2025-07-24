using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ClubManager.Services;
using ClubManager.Models;
using ClubManager.Commands;

namespace ClubManager.ViewModels
{
    public class LicenseViewModel : BaseViewModel
    {
        private readonly ILicenseService _licenseService;
        private string _licenseKey = "";
        private string _statusMessage = "Introduce una clave de licencia";
        private string _statusColor = "#FFCCCCCC";
        private string _licenseDetails = "";
        private Visibility _showDetails = Visibility.Collapsed;

        public event EventHandler? LicenseActivated;

        public LicenseViewModel()
        {
            _licenseService = new LicenseService();

            LoadFromFileCommand = new RelayCommand(LoadFromFile);
            PasteFromClipboardCommand = new RelayCommand(PasteFromClipboard);
            ActivateLicenseCommand = new RelayCommand(ActivateLicense, CanActivateLicense);

            // Cargar licencia guardada si existe
            _licenseService.LoadSavedLicense();
            UpdateStatusFromCurrentLicense();
        }

        #region Properties

        public string LicenseKey
        {
            get => _licenseKey;
            set
            {
                SetProperty(ref _licenseKey, value);
                ValidateLicenseKey();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public string LicenseDetails
        {
            get => _licenseDetails;
            set => SetProperty(ref _licenseDetails, value);
        }

        public Visibility ShowDetails
        {
            get => _showDetails;
            set => SetProperty(ref _showDetails, value);
        }

        #endregion

        #region Commands

        public ICommand LoadFromFileCommand { get; }
        public ICommand PasteFromClipboardCommand { get; }
        public ICommand ActivateLicenseCommand { get; }

        private bool CanActivateLicense()
        {
            return !string.IsNullOrWhiteSpace(LicenseKey) && LicenseKey.Length >= 10;
        }

        private void LoadFromFile()
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "Archivos de Licencia (*.lic)|*.lic|Archivos de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                    Title = "Cargar archivo de licencia"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var content = File.ReadAllText(openDialog.FileName).Trim();
                    if (!string.IsNullOrEmpty(content))
                    {
                        LicenseKey = content;
                        StatusMessage = "📄 Licencia cargada desde archivo";
                    }
                    else
                    {
                        StatusMessage = "❌ El archivo está vacío";
                        StatusColor = "#FFF44336";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error al cargar archivo: {ex.Message}";
                StatusColor = "#FFF44336";
            }
        }

        private void PasteFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var clipboardText = Clipboard.GetText().Trim();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        LicenseKey = clipboardText;
                        StatusMessage = "📋 Licencia pegada desde portapapeles";
                    }
                    else
                    {
                        StatusMessage = "❌ El portapapeles está vacío";
                        StatusColor = "#FFF44336";
                    }
                }
                else
                {
                    StatusMessage = "❌ No hay texto en el portapapeles";
                    StatusColor = "#FFF44336";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error al acceder al portapapeles: {ex.Message}";
                StatusColor = "#FFF44336";
            }
        }

        private void ActivateLicense()
        {
            try
            {
                bool isValid = _licenseService.ActivateLicense(LicenseKey);

                if (isValid)
                {
                    UpdateStatusFromCurrentLicense();
                    LicenseActivated?.Invoke(this, EventArgs.Empty);

                    MessageBox.Show("¡Licencia activada correctamente!", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "❌ Licencia inválida";
                    StatusColor = "#FFF44336";
                    LicenseDetails = "";
                    ShowDetails = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error al activar licencia: {ex.Message}";
                StatusColor = "#FFF44336";
                LicenseDetails = "";
                ShowDetails = Visibility.Collapsed;
            }
        }

        #endregion

        #region Methods

        private void ValidateLicenseKey()
        {
            if (string.IsNullOrWhiteSpace(LicenseKey))
            {
                StatusMessage = "Introduce una clave de licencia";
                StatusColor = "#FFCCCCCC";
                ShowDetails = Visibility.Collapsed;
            }
            else if (LicenseKey.Length < 10)
            {
                StatusMessage = "⚠️ La licencia debe tener al menos 10 caracteres";
                StatusColor = "#FFFF9800";
                ShowDetails = Visibility.Collapsed;
            }
            else
            {
                StatusMessage = "✅ Formato válido - Haz clic en Activar";
                StatusColor = "#FF4CAF50";
                ShowDetails = Visibility.Collapsed;
            }
        }

        private void UpdateStatusFromCurrentLicense()
        {
            var licenseInfo = _licenseService.GetCurrentLicenseInfo();

            if (licenseInfo.IsValid)
            {
                if (licenseInfo.IsExpired)
                {
                    StatusMessage = "⏰ Licencia expirada";
                    StatusColor = "#FFF44336"; // Rojo
                    LicenseDetails = $"Club: {licenseInfo.ClubName}\nExpiró: {licenseInfo.ExpirationDate:dd/MM/yyyy HH:mm}";
                }
                else
                {
                    StatusMessage = "✅ Licencia válida";
                    StatusColor = "#FF4CAF50"; // Verde
                    TimeSpan remaining = licenseInfo.ExpirationDate!.Value - DateTime.Now;
                    string remainingText = GetRemainingTimeText(remaining);
                    LicenseDetails = $"Club: {licenseInfo.ClubName}\nExpira: {licenseInfo.ExpirationDate:dd/MM/yyyy HH:mm}\nTiempo restante: {remainingText}";
                }
                ShowDetails = Visibility.Visible;
            }
            else
            {
                StatusMessage = $"❌ {licenseInfo.ErrorMessage}";
                StatusColor = "#FFF44336"; // Rojo
                LicenseDetails = "";
                ShowDetails = Visibility.Collapsed;
            }
        }

        private string GetRemainingTimeText(TimeSpan remaining)
        {
            if (remaining.TotalDays > 365)
                return $"{(int)(remaining.TotalDays / 365)} año(s)";
            else if (remaining.TotalDays > 30)
                return $"{(int)(remaining.TotalDays / 30)} mes(es)";
            else if (remaining.TotalDays > 1)
                return $"{(int)remaining.TotalDays} día(s)";
            else if (remaining.TotalHours > 1)
                return $"{(int)remaining.TotalHours} hora(s)";
            else
                return "Menos de 1 hora";
        }

        #endregion
    }
}