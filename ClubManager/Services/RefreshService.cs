// Services/RefreshService.cs - Versión simple para actualización en tiempo real
using System;
using System.ComponentModel;

namespace ClubManager.Services
{
    public enum RefreshType
    {
        Configuration,
        License,
        Theme,
        All
    }

    public class SimpleRefreshEventArgs : EventArgs
    {
        public RefreshType Type { get; }
        public string? Message { get; }

        public SimpleRefreshEventArgs(RefreshType type, string? message = null)
        {
            Type = type;
            Message = message;
        }
    }

    public interface IRefreshService
    {
        event EventHandler<SimpleRefreshEventArgs>? RefreshRequested;
        void RequestRefresh(RefreshType type, string? message = null);
        void RefreshConfiguration(string? message = null);
        void RefreshLicense(string? message = null);
        void RefreshTheme(string? message = null);
        void RequestFullRefresh(string? message = null);
    }

    public class RefreshService : IRefreshService, INotifyPropertyChanged
    {
        public event EventHandler<SimpleRefreshEventArgs>? RefreshRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void RequestRefresh(RefreshType type, string? message = null)
        {
            try
            {
                var args = new SimpleRefreshEventArgs(type, message);
                RefreshRequested?.Invoke(this, args);

                System.Diagnostics.Debug.WriteLine($"🔄 Refresh solicitado: {type} - {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en refresh: {ex.Message}");
            }
        }

        public void RefreshConfiguration(string? message = null)
        {
            RequestRefresh(RefreshType.Configuration, message ?? "Configuración actualizada");
        }

        public void RefreshLicense(string? message = null)
        {
            RequestRefresh(RefreshType.License, message ?? "Licencia actualizada");
        }

        public void RefreshTheme(string? message = null)
        {
            RequestRefresh(RefreshType.Theme, message ?? "Tema actualizado");
        }

        public void RequestFullRefresh(string? message = null)
        {
            RequestRefresh(RefreshType.All, message ?? "Sistema actualizado");
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}