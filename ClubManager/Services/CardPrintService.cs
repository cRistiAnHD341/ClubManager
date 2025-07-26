using ClubManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Size = System.Windows.Size;

namespace ClubManager.Services
{
    /// <summary>
    /// Servicio para imprimir tarjetas de abonado
    /// </summary>
    public interface ICardPrintService
    {
        Task<bool> ImprimirTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla);
        Task<bool> ImprimirTarjetasAsync(List<Abonado> abonados, PlantillaTarjeta plantilla);
        Task<FrameworkElement> GenerarVistaPreviaAsync(Abonado abonado, PlantillaTarjeta plantilla);
        Task<BitmapSource> GenerarImagenTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla);
        Task<bool> ExportarTarjetaAPdfAsync(Abonado abonado, PlantillaTarjeta plantilla, string rutaArchivo);
        List<string> GetImpresorasDisponibles();
        Task<bool> ValidarImpresoraAsync(string nombreImpresora);
    }

    public class CardPrintService : ICardPrintService
    {
        private readonly ITemplateService _templateService;

        public CardPrintService(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        public async Task<bool> ImprimirTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla)
        {
            try
            {
                var tarjeta = await GenerarVistaPreviaAsync(abonado, plantilla);
                if (tarjeta == null)
                    return false;

                // Configurar impresión
                var printDialog = new System.Windows.Controls.PrintDialog();

                // Configurar tamaño de página para tarjetas
                var pageSize = new Size(
                    ConvertirPixelsAPuntos(plantilla.Ancho),
                    ConvertirPixelsAPuntos(plantilla.Alto)
                );

                // Crear documento de impresión
                var document = new FixedDocument();
                var page = new FixedPage
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };

                // Escalar la tarjeta al tamaño de impresión
                tarjeta.Width = pageSize.Width;
                tarjeta.Height = pageSize.Height;

                page.Children.Add(tarjeta);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                document.Pages.Add(pageContent);

                // Imprimir
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                    $"Tarjeta de {abonado.NombreCompleto}");

                // Marcar como impreso
                abonado.Impreso = true;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error imprimiendo tarjeta: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ImprimirTarjetasAsync(List<Abonado> abonados, PlantillaTarjeta plantilla)
        {
            try
            {
                if (!abonados.Any())
                    return false;

                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() != true)
                    return false;

                // Configurar documento para múltiples tarjetas
                var document = new FixedDocument();
                var tarjetasPorPagina = CalcularTarjetasPorPagina(plantilla, printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                for (int i = 0; i < abonados.Count; i += tarjetasPorPagina)
                {
                    var abonadosPagina = abonados.Skip(i).Take(tarjetasPorPagina).ToList();
                    var pagina = await CrearPaginaConTarjetasAsync(abonadosPagina, plantilla, printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                    if (pagina != null)
                    {
                        var pageContent = new PageContent();
                        ((IAddChild)pageContent).AddChild(pagina);
                        document.Pages.Add(pageContent);
                    }
                }

                // Imprimir documento
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                    $"Tarjetas de abonados ({abonados.Count})");

                // Marcar como impresos
                foreach (var abonado in abonados)
                {
                    abonado.Impreso = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error imprimiendo tarjetas múltiples: {ex.Message}");
                return false;
            }
        }

        public async Task<FrameworkElement> GenerarVistaPreviaAsync(Abonado abonado, PlantillaTarjeta plantilla)
        {
            try
            {
                var canvas = new Canvas
                {
                    Width = plantilla.Ancho,
                    Height = plantilla.Alto,
                    Background = Brushes.White
                };

                // Agregar borde a la tarjeta
                var border = new Rectangle
                {
                    Width = plantilla.Ancho,
                    Height = plantilla.Alto,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    Fill = Brushes.White
                };
                canvas.Children.Add(border);

                // Procesar elementos de la plantilla
                foreach (var elemento in plantilla.Elementos.OrderBy(e => e.ZIndex))
                {
                    if (!elemento.Visible)
                        continue;

                    var uiElement = await CrearElementoUIAsync(elemento, abonado);
                    if (uiElement != null)
                    {
                        Canvas.SetLeft(uiElement, elemento.X);
                        Canvas.SetTop(uiElement, elemento.Y);
                        Canvas.SetZIndex(uiElement, elemento.ZIndex);

                        // Aplicar transformaciones
                        if (elemento.Rotacion != 0 || elemento.Opacidad != 1.0)
                        {
                            var transform = new RotateTransform(elemento.Rotacion);
                            uiElement.RenderTransform = transform;
                            uiElement.Opacity = elemento.Opacidad;
                        }

                        canvas.Children.Add(uiElement);
                    }
                }

                return canvas;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando vista previa: {ex.Message}");
                return null;
            }
        }

        public async Task<BitmapSource> GenerarImagenTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla)
        {
            try
            {
                var tarjeta = await GenerarVistaPreviaAsync(abonado, plantilla);
                if (tarjeta == null)
                    return null;

                // Renderizar a bitmap
                var renderTarget = new RenderTargetBitmap(
                    (int)plantilla.Ancho,
                    (int)plantilla.Alto,
                    96, 96,
                    PixelFormats.Pbgra32
                );

                tarjeta.Measure(new Size(plantilla.Ancho, plantilla.Alto));
                tarjeta.Arrange(new Rect(0, 0, plantilla.Ancho, plantilla.Alto));
                renderTarget.Render(tarjeta);

                return renderTarget;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando imagen: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ExportarTarjetaAPdfAsync(Abonado abonado, PlantillaTarjeta plantilla, string rutaArchivo)
        {
            try
            {
                // Nota: Para una implementación completa de PDF, se necesitaría una librería como iTextSharp
                // Por ahora, exportamos como imagen PNG
                var imagen = await GenerarImagenTarjetaAsync(abonado, plantilla);
                if (imagen == null)
                    return false;

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imagen));

                var rutaPng = System.IO.Path.ChangeExtension(rutaArchivo, ".png");
                using (var stream = new FileStream(rutaPng, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exportando a PDF: {ex.Message}");
                return false;
            }
        }

        public List<string> GetImpresorasDisponibles()
        {
            try
            {
                var impresoras = new List<string>();

                // Obtener impresoras del sistema
                foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    impresoras.Add(printerName);
                }

                return impresoras;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo impresoras: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> ValidarImpresoraAsync(string nombreImpresora)
        {
            try
            {
                var impresoras = GetImpresorasDisponibles();
                return impresoras.Contains(nombreImpresora);
            }
            catch
            {
                return false;
            }
        }

        private async Task<UIElement> CrearElementoUIAsync(ElementoTarjeta elemento, Abonado abonado)
        {
            try
            {
                return elemento switch
                {
                    ElementoTexto texto => CrearTextoUI(texto),
                    ElementoImagen imagen => await CrearImagenUIAsync(imagen),
                    ElementoCodigoBarras codigo => CrearCodigoBarrasUI(codigo, abonado),
                    ElementoCampoDinamico campo => CrearCampoDinamicoUI(campo, abonado),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando elemento UI: {ex.Message}");
                return null;
            }
        }

        private TextBlock CrearTextoUI(ElementoTexto elemento)
        {
            var textBlock = new TextBlock
            {
                Text = elemento.Texto,
                FontFamily = new FontFamily(elemento.FontFamily),
                FontSize = elemento.FontSize,
                Foreground = new SolidColorBrush(elemento.Color),
                FontWeight = elemento.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = elemento.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                TextDecorations = elemento.IsUnderline ? TextDecorations.Underline : null,
                TextAlignment = elemento.TextAlignment,
                Width = elemento.Ancho,
                Height = elemento.Alto,
                TextWrapping = elemento.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap
            };

            if (elemento.AutoSize)
            {
                textBlock.Width = double.NaN;
                textBlock.Height = double.NaN;
            }

            return textBlock;
        }

        private async Task<UIElement> CrearImagenUIAsync(ElementoImagen elemento)
        {
            try
            {
                if (string.IsNullOrEmpty(elemento.RutaImagen) || !File.Exists(elemento.RutaImagen))
                    return null;

                var image = new Image
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Stretch = elemento.MantenerAspecto ? Stretch.Uniform : Stretch.Fill
                };

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(elemento.RutaImagen);
                bitmap.EndInit();
                image.Source = bitmap;

                // Aplicar bordes redondeados si es necesario
                if (elemento.Redondez > 0)
                {
                    var border = new Border
                    {
                        Width = elemento.Ancho,
                        Height = elemento.Alto,
                        CornerRadius = new CornerRadius(elemento.Redondez),
                        Child = image,
                        ClipToBounds = true
                    };

                    if (elemento.GrosorBorde > 0)
                    {
                        border.BorderBrush = new SolidColorBrush(elemento.ColorBorde);
                        border.BorderThickness = new Thickness(elemento.GrosorBorde);
                    }

                    return border;
                }

                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando imagen: {ex.Message}");
                return null;
            }
        }

        private UIElement CrearCodigoBarrasUI(ElementoCodigoBarras elemento, Abonado abonado)
        {
            try
            {
                var valor = ObtenerValorCampo(elemento.CampoOrigen, abonado);
                if (string.IsNullOrEmpty(valor))
                    return null;

                var container = new Border
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Background = new SolidColorBrush(elemento.ColorFondo),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0.5)
                };

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Generar código de barras
                var barcodeImage = BarcodeGenerator.GenerateBarcode(valor, elemento.Ancho - 4,
                    elemento.MostrarTexto ? elemento.Alto - 15 : elemento.Alto - 4, elemento.TipoCodigo);

                if (barcodeImage != null)
                {
                    var image = new Image
                    {
                        Source = barcodeImage,
                        Stretch = Stretch.Fill,
                        Margin = new Thickness(2)
                    };
                    stackPanel.Children.Add(image);
                }

                // Agregar texto si está habilitado
                if (elemento.MostrarTexto)
                {
                    var textoFormateado = string.IsNullOrEmpty(elemento.FormatoTexto)
                        ? valor
                        : string.Format(elemento.FormatoTexto, valor);

                    var textBlock = new TextBlock
                    {
                        Text = textoFormateado,
                        FontFamily = new FontFamily(elemento.FontFamily),
                        FontSize = elemento.FontSize,
                        Foreground = new SolidColorBrush(elemento.ColorTexto),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 0)
                    };
                    stackPanel.Children.Add(textBlock);
                }

                container.Child = stackPanel;
                return container;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando código de barras: {ex.Message}");
                return CrearElementoError(elemento, "Error en código");
            }
        }

        private UIElement CrearCampoDinamicoUI(ElementoCampoDinamico elemento, Abonado abonado)
        {
            try
            {
                var valor = ObtenerValorCampo(elemento.CampoOrigen, abonado);

                // Aplicar formato si es necesario
                var textoFinal = FormatearValor(valor, elemento);

                var textBlock = new TextBlock
                {
                    Text = textoFinal,
                    FontFamily = new FontFamily(elemento.FontFamily),
                    FontSize = elemento.FontSize,
                    Foreground = new SolidColorBrush(elemento.Color),
                    FontWeight = elemento.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = elemento.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                    TextDecorations = elemento.IsUnderline ? TextDecorations.Underline : null,
                    TextAlignment = elemento.TextAlignment,
                    Width = elemento.Ancho,
                    Height = elemento.Alto
                };

                if (elemento.AutoSize)
                {
                    textBlock.Width = double.NaN;
                    textBlock.Height = double.NaN;
                }

                return textBlock;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando campo dinámico: {ex.Message}");
                return CrearElementoError(elemento, "Error en campo");
            }
        }

        private string ObtenerValorCampo(string campo, Abonado abonado)
        {
            try
            {
                return campo switch
                {
                    "NombreCompleto" => abonado.NombreCompleto,
                    "Nombre" => abonado.Nombre,
                    "Apellidos" => abonado.Apellidos,
                    "NumeroSocio" => abonado.NumeroSocio.ToString(),
                    "DNI" => abonado.DNI ?? "",
                    "Telefono" => abonado.Telefono ?? "",
                    "Email" => abonado.Email ?? "",
                    "Direccion" => abonado.Direccion ?? "",
                    "CodigoBarras" => abonado.CodigoBarras ?? "",
                    "TallaCamiseta" => abonado.TallaCamiseta ?? "",
                    "Estado" => abonado.EstadoTexto,
                    "FechaNacimiento" => abonado.FechaNacimiento.ToString("dd/MM/yyyy"),
                    "FechaCreacion" => abonado.FechaCreacion.ToString("dd/MM/yyyy"),
                    "Peña" => abonado.Peña?.Nombre ?? "",
                    "TipoAbono" => abonado.TipoAbono?.Nombre ?? "",
                    "Gestor" => abonado.Gestor?.Nombre ?? "",
                    "PrecioAbono" => abonado.TipoAbono?.Precio.ToString("C") ?? "",
                    "Observaciones" => abonado.Observaciones ?? "",
                    _ => ""
                };
            }
            catch
            {
                return "";
            }
        }

        private string FormatearValor(string valor, ElementoCampoDinamico elemento)
        {
            try
            {
                if (string.IsNullOrEmpty(valor))
                {
                    return string.IsNullOrEmpty(elemento.TextoSiVacio) ? "" : elemento.TextoSiVacio;
                }

                var resultado = valor;

                // Aplicar formato de número si es aplicable
                if (!string.IsNullOrEmpty(elemento.FormatoNumero) && decimal.TryParse(valor, out decimal numero))
                {
                    resultado = numero.ToString(elemento.FormatoNumero);
                }
                // Aplicar formato de fecha si es aplicable
                else if (!string.IsNullOrEmpty(elemento.FormatoFecha) && DateTime.TryParse(valor, out DateTime fecha))
                {
                    resultado = fecha.ToString(elemento.FormatoFecha);
                }

                // Agregar prefijo y sufijo
                if (!string.IsNullOrEmpty(elemento.Prefijo))
                    resultado = elemento.Prefijo + resultado;

                if (!string.IsNullOrEmpty(elemento.Sufijo))
                    resultado = resultado + elemento.Sufijo;

                return resultado;
            }
            catch
            {
                return valor;
            }
        }

        private UIElement CrearElementoError(ElementoTarjeta elemento, string mensaje)
        {
            var border = new Border
            {
                Width = elemento.Ancho,
                Height = elemento.Alto,
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(1),
                Background = Brushes.LightPink
            };

            var textBlock = new TextBlock
            {
                Text = mensaje,
                FontSize = 10,
                Foreground = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            border.Child = textBlock;
            return border;
        }

        private async Task<FixedPage> CrearPaginaConTarjetasAsync(List<Abonado> abonados, PlantillaTarjeta plantilla,
            double anchoPagina, double altoPagina)
        {
            try
            {
                var page = new FixedPage
                {
                    Width = anchoPagina,
                    Height = altoPagina
                };

                var tarjetasPorFila = (int)(anchoPagina / (plantilla.Ancho + 10)); // 10px de margen
                var tarjetasPorColumna = (int)(altoPagina / (plantilla.Alto + 10));

                var margenX = (anchoPagina - (tarjetasPorFila * (plantilla.Ancho + 10))) / 2;
                var margenY = (altoPagina - (tarjetasPorColumna * (plantilla.Alto + 10))) / 2;

                for (int i = 0; i < abonados.Count && i < (tarjetasPorFila * tarjetasPorColumna); i++)
                {
                    var fila = i / tarjetasPorFila;
                    var columna = i % tarjetasPorFila;

                    var tarjeta = await GenerarVistaPreviaAsync(abonados[i], plantilla);
                    if (tarjeta != null)
                    {
                        var x = margenX + columna * (plantilla.Ancho + 10);
                        var y = margenY + fila * (plantilla.Alto + 10);

                        FixedPage.SetLeft(tarjeta, x);
                        FixedPage.SetTop(tarjeta, y);
                        page.Children.Add(tarjeta);
                    }
                }

                return page;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando página con tarjetas: {ex.Message}");
                return null;
            }
        }

        private int CalcularTarjetasPorPagina(PlantillaTarjeta plantilla, double anchoPagina, double altoPagina)
        {
            try
            {
                var tarjetasPorFila = (int)(anchoPagina / (plantilla.Ancho + 10)); // 10px de margen
                var tarjetasPorColumna = (int)(altoPagina / (plantilla.Alto + 10));
                return Math.Max(1, tarjetasPorFila * tarjetasPorColumna);
            }
            catch
            {
                return 1;
            }
        }

        private double ConvertirPixelsAPuntos(double pixels)
        {
            // 1 punto = 1/72 pulgadas, 1 pixel = 1/96 pulgadas
            return pixels * 72.0 / 96.0;
        }
    }

    /// <summary>
    /// Configuración de impresión personalizada
    /// </summary>
    public class ConfiguracionImpresionTarjetas
    {
        public string NombreImpresora { get; set; } = "";
        public double MargenSuperior { get; set; } = 10;
        public double MargenInferior { get; set; } = 10;
        public double MargenIzquierdo { get; set; } = 10;
        public double MargenDerecho { get; set; } = 10;
        public int Calidad { get; set; } = 300; // DPI
        public bool ImpresionColor { get; set; } = true;
        public string TamañoPapel { get; set; } = "A4";
        public bool AjustarTamañoAutomaticamente { get; set; } = true;
        public int MaximasTarjetasPorPagina { get; set; } = 10;
        public double EspaciadoHorizontal { get; set; } = 5;
        public double EspaciadoVertical { get; set; } = 5;
        public bool MostrarBordeCorte { get; set; } = false;
        public bool ImprimirEnDuplicado { get; set; } = false; // Para hacer copias de seguridad
    }

    /// <summary>
    /// Resultado de operación de impresión
    /// </summary>
    public class ResultadoImpresion
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
        public int TarjetasImpresas { get; set; }
        public int TarjetasConError { get; set; }
        public List<string> Errores { get; set; } = new();
        public TimeSpan TiempoTranscurrido { get; set; }
        public DateTime FechaImpresion { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Extensiones para facilitar la impresión
    /// </summary>
    public static class CardPrintExtensions
    {
        /// <summary>
        /// Imprime una tarjeta con configuración personalizada
        /// </summary>
        public static async Task<ResultadoImpresion> ImprimirConConfiguracionAsync(
            this ICardPrintService service,
            Abonado abonado,
            PlantillaTarjeta plantilla,
            ConfiguracionImpresionTarjetas configuracion)
        {
            var inicio = DateTime.Now;
            var resultado = new ResultadoImpresion();

            try
            {
                // Validar impresora
                if (!string.IsNullOrEmpty(configuracion.NombreImpresora))
                {
                    var esValida = await service.ValidarImpresoraAsync(configuracion.NombreImpresora);
                    if (!esValida)
                    {
                        resultado.Errores.Add($"Impresora '{configuracion.NombreImpresora}' no disponible");
                        return resultado;
                    }
                }

                // Imprimir
                var exitoso = await service.ImprimirTarjetaAsync(abonado, plantilla);

                resultado.Exitoso = exitoso;
                resultado.TarjetasImpresas = exitoso ? 1 : 0;
                resultado.TarjetasConError = exitoso ? 0 : 1;
                resultado.Mensaje = exitoso ? "Tarjeta impresa correctamente" : "Error al imprimir tarjeta";

                if (!exitoso)
                {
                    resultado.Errores.Add("Error durante la impresión");
                }
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Errores.Add(ex.Message);
                resultado.Mensaje = "Error inesperado durante la impresión";
            }
            finally
            {
                resultado.TiempoTranscurrido = DateTime.Now - inicio;
            }

            return resultado;
        }

        /// <summary>
        /// Valida que una plantilla sea apta para impresión
        /// </summary>
        public static bool EsAptaParaImpresion(this PlantillaTarjeta plantilla)
        {
            if (plantilla == null)
                return false;

            // Verificar dimensiones mínimas
            if (plantilla.Ancho < 50 || plantilla.Alto < 30)
                return false;

            // Verificar que tenga al menos un elemento
            if (!plantilla.Elementos.Any())
                return false;

            // Verificar que los elementos estén dentro de los límites
            foreach (var elemento in plantilla.Elementos)
            {
                if (elemento.X + elemento.Ancho > plantilla.Ancho ||
                    elemento.Y + elemento.Alto > plantilla.Alto)
                    return false;
            }

            return true;
        }
    }
}