using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ClubManager
{
    /// <summary>
    /// Convertidor que evalúa múltiples valores de Visibility y devuelve Visible si al menos uno es Visible
    /// </summary>
    public class VisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            // Si alguno de los valores es Visible, mostrar la sección
            bool hasVisibleItem = values.Any(value =>
                value is Visibility visibility && visibility == Visibility.Visible);

            return hasVisibleItem ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertidor que evalúa múltiples valores booleanos y devuelve true si al menos uno es true
    /// </summary>
    public class AnyTrueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            return values.Any(value => value is bool boolValue && boolValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}