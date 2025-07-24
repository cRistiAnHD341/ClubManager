using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClubManager.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Implementación de IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Limpiar recursos administrados aquí
                    // Las clases derivadas pueden sobrescribir este método
                }

                // Limpiar recursos no administrados aquí (si los hay)
                _disposed = true;
            }
        }

        // Finalizer (solo si hay recursos no administrados)
        ~BaseViewModel()
        {
            Dispose(false);
        }
    }
}