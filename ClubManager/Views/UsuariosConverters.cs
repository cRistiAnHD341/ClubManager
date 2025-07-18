using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClubManager.Views
{
    public class RolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string rol)
            {
                return rol switch
                {
                    "Administrador" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC3545")), // Rojo
                    "Gestor" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF007ACC")), // Azul
                    "Usuario" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF28A745")), // Verde
                    "Solo Lectura" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6C757D")), // Gris
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActiveToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
            {
                return activo
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50")) // Verde
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF44336")); // Rojo
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}