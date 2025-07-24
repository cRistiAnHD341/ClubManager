using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ClubManager.Models;

namespace ClubManager.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Manejar valores null
            if (value == null)
                return Visibility.Collapsed;

            bool boolValue = false;

            // Intentar convertir diferentes tipos a bool
            if (value is bool directBool)
            {
                boolValue = directBool;
            }
            else if (value is int intValue)
            {
                boolValue = intValue > 0;
            }
            else if (value is string stringValue)
            {
                boolValue = !string.IsNullOrEmpty(stringValue);
            }
            else
            {
                // Para otros tipos, considerar no-null como true
                boolValue = true;
            }

            // Verificar si se debe invertir
            bool invert = parameter?.ToString()?.ToLowerInvariant() == "invert";

            if (invert)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = parameter?.ToString()?.ToLowerInvariant() == "invert";
                bool result = visibility == Visibility.Visible;

                if (invert)
                    result = !result;

                return result;
            }

            return false;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInvert = parameter?.ToString()?.ToLowerInvariant() == "invert";
            bool hasValue = value != null;

            if (isInvert)
                hasValue = !hasValue;

            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NullToVisibilityConverter no soporta ConvertBack");
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && !boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Collapsed;
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : false;
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value?.ToString() ?? "";
            bool invert = parameter?.ToString() == "Invert";
            bool hasText = !string.IsNullOrWhiteSpace(text);

            return (hasText ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue ? "✅" : "❌";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EstadoToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstadoAbonado estado)
            {
                return estado switch
                {
                    EstadoAbonado.Activo => "✅",
                    EstadoAbonado.Inactivo => "⏸️",
                };
            }
            return "❓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ✅ CONVERTER FALTANTE AÑADIDO
    public class NumberToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                if (intValue > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (intValue == 0)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }

            if (value is double doubleValue)
            {
                if (doubleValue > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (doubleValue == 0)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }

            if (value is decimal decimalValue)
            {
                if (decimalValue > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (decimalValue == 0)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ✅ CONVERTERS ADICIONALES ÚTILES
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Activo" : "Inactivo";
            }
            return "Desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value?.ToString()?.ToLower() ?? "";
            return text == "activo";
        }
    }

    public class DecimalToEuroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("C", new CultureInfo("es-ES"));
            }
            if (value is double doubleValue)
            {
                return doubleValue.ToString("C", new CultureInfo("es-ES"));
            }
            if (value is int intValue)
            {
                return intValue.ToString("C", new CultureInfo("es-ES"));
            }
            return "0,00 €";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (decimal.TryParse(value?.ToString()?.Replace("€", "").Replace(",", "."), NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return 0m;
        }
    }

    public class RolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string rol = value?.ToString()?.ToLower() ?? "";
            return rol switch
            {
                "administrador" => new SolidColorBrush(Colors.DarkRed),
                "gestor" => new SolidColorBrush(Colors.DarkBlue),
                "usuario" => new SolidColorBrush(Colors.DarkGreen),
                "solo lectura" => new SolidColorBrush(Colors.Gray),
                _ => new SolidColorBrush(Colors.Black)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                bool invert = parameter?.ToString() == "Invert";
                bool hasItems = count > 0;
                return (hasItems ^ invert) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                string format = parameter?.ToString() ?? "dd/MM/yyyy HH:mm";
                return dateTime.ToString(format);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DateTime.TryParse(value?.ToString(), out DateTime result))
            {
                return result;
            }
            return DateTime.Now;
        }
    }

    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return $"{doubleValue:F1}%";
            }
            if (value is decimal decimalValue)
            {
                return $"{decimalValue:F1}%";
            }
            if (value is int intValue)
            {
                return $"{intValue}%";
            }
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value?.ToString()?.Replace("%", "") ?? "0";
            if (double.TryParse(text, out double result))
            {
                return result;
            }
            return 0.0;
        }

        public class ColumnIndexToTextConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int index)
                {
                    return index >= 0 ? $"Columna {index + 1}" : "-- No seleccionado --";
                }

                return "-- No seleccionado --";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}