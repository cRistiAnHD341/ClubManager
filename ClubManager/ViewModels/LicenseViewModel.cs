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
    public class LicenseViewModel : INotifyPropertyChanged
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
                _licenseKey = value;
                OnPropertyChanged();
                ValidateLicenseKey();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        public string LicenseDetails
        {
            get => _licenseDetails;
            set { _licenseDetails = value; OnPropertyChanged(); }
        }

        public Visibility ShowDetails
        {
            get => _showDetails;
            set { _showDetails = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand LoadFromFileCommand { get; }
        public ICommand PasteFromClipboardCommand { get; }
        public ICommand ActivateLicenseCommand { get; }

        #endregion

        #region Methods

        private void UpdateStatusFromCurrentLicense()
        {
            var currentLicense = _licenseService.GetCurrentLicenseInfo();

            if (currentLicense.IsValid)
            {
                if (currentLicense.IsExpired)
                {
                    StatusMessage = "⚠️ Licencia expirada - Modo solo lectura";
                    StatusColor = "#FFFF9800"; // Amarillo/Naranja
                    LicenseDetails = $"Club: {currentLicense.ClubName}\nExpiró: {currentLicense.ExpirationDate:dd/MM/yyyy HH:mm}";
                }
                else
                {
                    StatusMessage = "✅ Licencia válida y activa";
                    StatusColor = "#FF4CAF50"; // Verde
                    LicenseDetails = $"Club: {currentLicense.ClubName}\nExpira: {currentLicense.ExpirationDate:dd/MM/yyyy HH:mm}";
                }
                ShowDetails = Visibility.Visible;
            }
            else
            {
                StatusMessage = "❌ Sin licencia válida";
                StatusColor = "#FFF44336"; // Rojo
                LicenseDetails = "";
                ShowDetails = Visibility.Collapsed;
            }
        }

        private void ValidateLicenseKey()
        {
            if (string.IsNullOrWhiteSpace(LicenseKey))
            {
                StatusMessage = "Introduce una clave de licencia";
                StatusColor = "#FFCCCCCC";
                LicenseDetails = "";
                ShowDetails = Visibility.Collapsed;
                return;
            }

            var licenseInfo = _licenseService.ValidateLicense(LicenseKey);

            if (licenseInfo.IsValid)
            {
                if (licenseInfo.IsExpired)
                {
                    StatusMessage = "⚠️ Licencia válida pero expirada";
                    StatusColor = "#FFFF9800"; // Amarillo/Naranja
                    LicenseDetails = $"Club: {licenseInfo.ClubName}\nExpiró: {licenseInfo.ExpirationDate:dd/MM/yyyy HH:mm}\n\nSe activará en modo solo lectura.";
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

        private void LoadFromFile()
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "Archivos de Licencia (*.lic)|*.lic|Archivos de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                    Title = "Seleccionar archivo de licencia"
                };

                if (openDialog.ShowDialog() == true)
                {
                    string fileContent = File.ReadAllText(openDialog.FileName);
                    LicenseKey = fileContent.Trim();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el archivo:\n{ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PasteFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    LicenseKey = Clipboard.GetText().Trim();
                }
                else
                {
                    MessageBox.Show("No hay texto en el portapapeles", "Información",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al acceder al portapapeles:\n{ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActivateLicense()
        {
            try
            {
                bool success = _licenseService.SetLicenseKey(LicenseKey);

                if (success)
                {
                    var licenseInfo = _licenseService.GetCurrentLicenseInfo();

                    string message = licenseInfo.IsExpired
                        ? "Licencia activada en modo solo lectura.\n\nLa licencia ha expirado, pero puedes consultar los datos existentes."
                        : "¡Licencia activada correctamente!\n\nYa puedes usar todas las funciones del programa.";

                    MessageBox.Show(message, "Licencia Activada",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    LicenseActivated?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageBox.Show("No se pudo activar la licencia. Verifica que la clave sea correcta.",
                                   "Error de Activación", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al activar la licencia:\n{ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanActivateLicense()
        {
            if (string.IsNullOrWhiteSpace(LicenseKey))
                return false;

            var licenseInfo = _licenseService.ValidateLicense(LicenseKey);
            return licenseInfo.IsValid;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}