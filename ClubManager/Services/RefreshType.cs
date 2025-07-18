using System;
using System.Threading.Tasks;

namespace ClubManager.Services
{
    public enum RefreshType
    {
        Configuration,
        Abonados,
        Users,
        All,
        License,
        Statistics,
        Theme
    }

    public class RefreshEventArgs : EventArgs
    {
        public RefreshType Type { get; }
        public object? Data { get; }

        public RefreshEventArgs(RefreshType type, object? data = null)
        {
            Type = type;
            Data = data;
        }
    }

    public interface IRefreshService
    {
        event EventHandler<RefreshEventArgs>? RefreshRequested;

        void RequestRefresh(RefreshType type, object? data = null);
        void RequestFullRefresh();

        // Métodos específicos para facilitar el uso
        void RefreshConfiguration();
        void RefreshAbonados();
        void RefreshUsers();
        void RefreshLicense();
        void RefreshStatistics();
        void RefreshTheme();
    }

    public class RefreshService : IRefreshService
    {
        public event EventHandler<RefreshEventArgs>? RefreshRequested;

        public void RequestRefresh(RefreshType type, object? data = null)
        {
            RefreshRequested?.Invoke(this, new RefreshEventArgs(type, data));
        }

        public void RequestFullRefresh()
        {
            RequestRefresh(RefreshType.All);
        }

        public void RefreshConfiguration()
        {
            RequestRefresh(RefreshType.Configuration);
        }

        public void RefreshAbonados()
        {
            RequestRefresh(RefreshType.Abonados);
        }

        public void RefreshUsers()
        {
            RequestRefresh(RefreshType.Users);
        }

        public void RefreshLicense()
        {
            RequestRefresh(RefreshType.License);
        }

        public void RefreshStatistics()
        {
            RequestRefresh(RefreshType.Statistics);
        }

        public void RefreshTheme()
        {
            RequestRefresh(RefreshType.Theme);
        }
    }
}