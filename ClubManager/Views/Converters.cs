using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ClubManager.Models;

namespace ClubManager.Views
{
    public class EstadoToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstadoAbonado estado)
            {
                return estado == EstadoAbonado.Activo
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

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool impreso)
            {
                return impreso
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9800")) // Amarillo
                    : new SolidColorBrush(Colors.Transparent);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool impreso)
            {
                return impreso ? "SÍ" : "NO";
            }
            return "NO";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}