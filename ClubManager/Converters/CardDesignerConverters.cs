// Converters/CardDesignerConverters.cs
// Convertidores completos para el diseñador de tarjetas

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ClubManager.Models;

namespace ClubManager.Converters
{
    /// <summary>
    /// Convierte un Color a SolidColorBrush
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Black;
        }
    }

    /// <summary>
    /// Convierte bool a FontWeight
    /// </summary>
    public class BooleanToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((FontWeight)value) == FontWeights.Bold;
        }
    }

    /// <summary>
    /// Convierte bool a FontStyle
    /// </summary>
    public class BooleanToFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? FontStyles.Italic : FontStyles.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((FontStyle)value) == FontStyles.Italic;
        }
    }

    /// <summary>
    /// Convierte bool a Stretch para imágenes
    /// </summary>
    public class BooleanToStretchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Stretch.Uniform : Stretch.Fill;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Stretch)value) == Stretch.Uniform;
        }
    }

    /// <summary>
    /// Convertidor de bool a Thickness para bordes
    /// </summary>
    public class BooleanToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new Thickness(2);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness thickness)
            {
                return thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0;
            }
            return false;
        }
    }

    /// <summary>
    /// Convierte string Tipo a icono
    /// </summary>
    public class TipoElementoToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tipo)
            {
                return tipo.ToLower() switch
                {
                    "texto" => "📝",
                    "imagen" => "🖼️",
                    "código" or "codigo" => "📊",
                    "campo dinámico" or "campo dinamico" => "🏷️",
                    "qr" => "📱",
                    "forma" => "⬛",
                    "logo" => "🏢",
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
    /// Selector de plantillas para elementos basado en el tipo string
    /// </summary>
    public class ElementoTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TextoTemplate { get; set; }
        public DataTemplate? ImagenTemplate { get; set; }
        public DataTemplate? FormaTemplate { get; set; }
        public DataTemplate? CodigoBarrasTemplate { get; set; }
        public DataTemplate? CampoDinamicoTemplate { get; set; }
        public DataTemplate? DefaultTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is ElementoTarjeta elemento)
            {
                return elemento.Tipo.ToLower() switch
                {
                    "texto" => TextoTemplate ?? DefaultTemplate,
                    "imagen" => ImagenTemplate ?? DefaultTemplate,
                    "forma" => FormaTemplate ?? DefaultTemplate,
                    "código" or "codigo" => CodigoBarrasTemplate ?? DefaultTemplate,
                    "campo dinámico" or "campo dinamico" => CampoDinamicoTemplate ?? DefaultTemplate,
                    _ => DefaultTemplate
                };
            }
            return DefaultTemplate;
        }
    }

    /// <summary>
    /// Selector de plantillas para propiedades basado en el tipo string
    /// </summary>
    public class PropiedadesTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? PropiedadesTextoTemplate { get; set; }
        public DataTemplate? PropiedadesImagenTemplate { get; set; }
        public DataTemplate? PropiedadesFormaTemplate { get; set; }
        public DataTemplate? PropiedadesCodigoBarrasTemplate { get; set; }
        public DataTemplate? PropiedadesCampoDinamicoTemplate { get; set; }
        public DataTemplate? DefaultTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is ElementoTarjeta elemento)
            {
                return elemento.Tipo.ToLower() switch
                {
                    "texto" => PropiedadesTextoTemplate ?? DefaultTemplate,
                    "imagen" => PropiedadesImagenTemplate ?? DefaultTemplate,
                    "forma" => PropiedadesFormaTemplate ?? DefaultTemplate,
                    "código" or "codigo" => PropiedadesCodigoBarrasTemplate ?? DefaultTemplate,
                    "campo dinámico" or "campo dinamico" => PropiedadesCampoDinamicoTemplate ?? DefaultTemplate,
                    _ => DefaultTemplate
                };
            }
            return DefaultTemplate;
        }
    }

    /// <summary>
    /// Convierte el tipo de elemento a un color distintivo
    /// </summary>
    public class TipoElementoToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tipo)
            {
                return tipo.ToLower() switch
                {
                    "texto" => new SolidColorBrush(Colors.Blue),
                    "imagen" => new SolidColorBrush(Colors.Green),
                    "código" or "codigo" => new SolidColorBrush(Colors.Orange),
                    "campo dinámico" or "campo dinamico" => new SolidColorBrush(Colors.Purple),
                    "qr" => new SolidColorBrush(Colors.DarkBlue),
                    "forma" => new SolidColorBrush(Colors.Gray),
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un elemento a su descripción textual
    /// </summary>
    public class ElementoToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ElementoTarjeta elemento)
            {
                return elemento.Tipo.ToLower() switch
                {
                    "texto" => $"Texto: {(elemento as ElementoTexto)?.Texto ?? "Sin texto"}",
                    "imagen" => $"Imagen: {System.IO.Path.GetFileName((elemento as ElementoImagen)?.RutaImagen ?? "Sin imagen")}",
                    "código" or "codigo" => $"Código: {(elemento as ElementoCodigo)?.TipoCodigo ?? "Código de barras"}",
                    "campo dinámico" or "campo dinamico" => $"Campo: {(elemento as ElementoCampoDinamico)?.CampoOrigen ?? "Sin campo"}",
                    _ => $"Elemento: {elemento.Tipo}"
                };
            }
            return "Elemento desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}