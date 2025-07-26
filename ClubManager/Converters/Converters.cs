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

        public class TipoElementoToIconConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string tipo)
                {
                    return tipo switch
                    {
                        "Texto" => "📝",
                        "Imagen" => "🖼️",
                        "Código" => "📊",
                        "Campo Dinámico" => "🏷️",
                        _ => "❓"
                    };
                }
                return "❓";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Convierte boolean a Visibility invirtiendo el valor
        /// </summary>
        public class InvertBooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is Visibility visibility)
                {
                    return visibility != Visibility.Visible;
                }
                return false;
            }
        }

        /// <summary>
        /// Convierte boolean a FontWeight
        /// </summary>
        public class BooleanToFontWeightConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool isBold && isBold)
                {
                    return FontWeights.Bold;
                }
                return FontWeights.Normal;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is FontWeight fontWeight)
                {
                    return fontWeight == FontWeights.Bold;
                }
                return false;
            }
        }

        /// <summary>
        /// Convierte boolean a FontStyle
        /// </summary>
        public class BooleanToFontStyleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool isItalic && isItalic)
                {
                    return FontStyles.Italic;
                }
                return FontStyles.Normal;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is FontStyle fontStyle)
                {
                    return fontStyle == FontStyles.Italic;
                }
                return false;
            }
        }

        /// <summary>
        /// Convierte Color de System.Windows.Media a Brush
        /// </summary>
        public class ColorToBrushConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is System.Windows.Media.Color color)
                {
                    return new System.Windows.Media.SolidColorBrush(color);
                }
                return System.Windows.Media.Brushes.Black;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is System.Windows.Media.SolidColorBrush brush)
                {
                    return brush.Color;
                }
                return System.Windows.Media.Colors.Black;
            }
        }

        /// <summary>
        /// Convierte boolean a Visibility
        /// </summary>
        public class BooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    // Verificar si el parámetro es "Invert" para invertir la lógica
                    bool invert = parameter?.ToString()?.ToLower() == "invert";

                    if (invert)
                    {
                        return boolValue ? Visibility.Collapsed : Visibility.Visible;
                    }
                    else
                    {
                        return boolValue ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is Visibility visibility)
                {
                    bool invert = parameter?.ToString()?.ToLower() == "invert";

                    if (invert)
                    {
                        return visibility != Visibility.Visible;
                    }
                    else
                    {
                        return visibility == Visibility.Visible;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Convierte valores numéricos a string con formato específico
        /// </summary>
        public class NumericFormatConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == null) return string.Empty;

                string format = parameter?.ToString() ?? "F2";

                if (value is double doubleValue)
                {
                    return doubleValue.ToString(format, culture);
                }
                if (value is float floatValue)
                {
                    return floatValue.ToString(format, culture);
                }
                if (value is decimal decimalValue)
                {
                    return decimalValue.ToString(format, culture);
                }
                if (value is int intValue)
                {
                    return intValue.ToString(format, culture);
                }

                return value.ToString() ?? string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    if (targetType == typeof(double) || targetType == typeof(double?))
                    {
                        if (double.TryParse(stringValue, NumberStyles.Float, culture, out double result))
                            return result;
                    }
                    else if (targetType == typeof(float) || targetType == typeof(float?))
                    {
                        if (float.TryParse(stringValue, NumberStyles.Float, culture, out float result))
                            return result;
                    }
                    else if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                    {
                        if (decimal.TryParse(stringValue, NumberStyles.Float, culture, out decimal result))
                            return result;
                    }
                    else if (targetType == typeof(int) || targetType == typeof(int?))
                    {
                        if (int.TryParse(stringValue, NumberStyles.Integer, culture, out int result))
                            return result;
                    }
                }

                return Binding.DoNothing;
            }
        }

        /// <summary>
        /// Convierte porcentaje a string con formato de porcentaje
        /// </summary>
        public class PercentageToStringConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double doubleValue)
                {
                    return (doubleValue * 100).ToString("F0", culture) + "%";
                }
                return "0%";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string stringValue)
                {
                    stringValue = stringValue.Replace("%", "").Trim();
                    if (double.TryParse(stringValue, NumberStyles.Float, culture, out double result))
                    {
                        return result / 100.0;
                    }
                }
                return 0.0;
            }
        }

        /// <summary>
        /// Convierte múltiples valores boolean usando operador AND
        /// </summary>
        public class MultiBooleanAndConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values == null || values.Length == 0)
                    return false;

                foreach (var value in values)
                {
                    if (value is bool boolValue && !boolValue)
                        return false;
                    if (value == DependencyProperty.UnsetValue)
                        return false;
                }

                return true;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Convierte múltiples valores boolean usando operador OR
        /// </summary>
        public class MultiBooleanOrConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values == null || values.Length == 0)
                    return false;

                foreach (var value in values)
                {
                    if (value is bool boolValue && boolValue)
                        return true;
                }

                return false;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Convierte un objeto null a boolean
        /// </summary>
        public class NullToBooleanConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool invert = parameter?.ToString()?.ToLower() == "invert";
                bool isNull = value == null;

                return invert ? isNull : !isNull;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Convierte string vacío o null a Visibility
        /// </summary>
        public class StringToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool invert = parameter?.ToString()?.ToLower() == "invert";
                bool isEmpty = string.IsNullOrWhiteSpace(value?.ToString());

                if (invert)
                {
                    return isEmpty ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    return isEmpty ? Visibility.Collapsed : Visibility.Visible;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Convierte el tamaño en bytes a texto legible
        /// </summary>
        public class FileSizeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is long bytes)
                {
                    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                    double len = bytes;
                    int order = 0;

                    while (len >= 1024 && order < sizes.Length - 1)
                    {
                        order++;
                        len = len / 1024;
                    }

                    return $"{len:0.##} {sizes[order]}";
                }

                if (value is int intBytes)
                {
                    return Convert((long)intBytes, targetType, parameter, culture);
                }

                return "0 B";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Convierte DateTime a string con formato relativo (hace X tiempo)
        /// </summary>
        public class RelativeTimeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is DateTime dateTime)
                {
                    var timeSpan = DateTime.Now - dateTime;

                    if (timeSpan.TotalMinutes < 1)
                        return "Ahora mismo";
                    if (timeSpan.TotalMinutes < 60)
                        return $"Hace {(int)timeSpan.TotalMinutes} minuto(s)";
                    if (timeSpan.TotalHours < 24)
                        return $"Hace {(int)timeSpan.TotalHours} hora(s)";
                    if (timeSpan.TotalDays < 7)
                        return $"Hace {(int)timeSpan.TotalDays} día(s)";
                    if (timeSpan.TotalDays < 30)
                        return $"Hace {(int)(timeSpan.TotalDays / 7)} semana(s)";
                    if (timeSpan.TotalDays < 365)
                        return $"Hace {(int)(timeSpan.TotalDays / 30)} mes(es)";

                    return $"Hace {(int)(timeSpan.TotalDays / 365)} año(s)";
                }

                return value?.ToString() ?? string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}