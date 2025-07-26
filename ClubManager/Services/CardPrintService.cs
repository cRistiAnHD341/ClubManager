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

namespace ClubManager.Services
{
    /// <summary>
    /// Servicio para imprimir tarjetas de abonado
    /// </summary>
    public interface ICardPrintService
    {
        Task<bool> ImprimirTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla);
        Task<bool> ImprimirTarjetasAsync(List<Abonado> abonados, PlantillaTarjeta plantilla);
        Task<ResultadoImpresion> ImprimirTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla, ConfiguracionImpresionEspecifica configuracion);
        Task<FrameworkElement> GenerarVistaPreviaAsync(Abonado abonado, PlantillaTarjeta plantilla);
        Task<BitmapSource> GenerarImagenTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla);
        Task<bool> ExportarTarjetaAPdfAsync(Abonado abonado, PlantillaTarjeta plantilla, string rutaArchivo);
        List<string> GetImpresorasDisponibles();
        Task<bool> ValidarImpresoraAsync(string nombreImpresora);
    }

    public class CardPrintService : ICardPrintService
    {
        private readonly ITemplateService _templateService;
        private readonly IConfiguracionService _configuracionService;

        public CardPrintService(ITemplateService templateService, IConfiguracionService configuracionService = null)
        {
            _templateService = templateService;
            _configuracionService = configuracionService ?? new ConfiguracionService();
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

                // Obtener configuración guardada
                var configuracion = _configuracionService.GetConfiguracion();

                // Configurar impresora predeterminada si está disponible
                if (!string.IsNullOrEmpty(configuracion.ConfiguracionImpresion.ImpresoraPredeterminada))
                {
                    var impresoras = GetImpresorasDisponibles();
                    if (impresoras.Contains(configuracion.ConfiguracionImpresion.ImpresoraPredeterminada))
                    {
                        var printer = new System.Drawing.Printing.PrinterSettings();
                        printer.PrinterName = configuracion.ConfiguracionImpresion.ImpresoraPredeterminada;

                        // Solo mostrar diálogo si está configurado para hacerlo
                        if (configuracion.ConfiguracionImpresion.MostrarVistaPrevia)
                        {
                            if (printDialog.ShowDialog() != true)
                                return false;
                        }
                    }
                }
                else
                {
                    // Mostrar diálogo de selección de impresora
                    if (printDialog.ShowDialog() != true)
                        return false;
                }

                // NUEVO: Configurar tamaño de página basado en la plantilla
                var pageSize = CalcularTamañoPaginaOptimo(plantilla);
                System.Diagnostics.Debug.WriteLine($"Tamaño de página calculado: {pageSize.Width}x{pageSize.Height} puntos para plantilla {plantilla.Ancho}x{plantilla.Alto} píxeles");

                // Crear documento de impresión
                var document = new FixedDocument();
                var page = new FixedPage
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };

                // Escalar la tarjeta al tamaño de impresión óptimo
                var escala = CalcularEscalaOptima(plantilla, pageSize);
                tarjeta.Width = plantilla.Ancho * escala;
                tarjeta.Height = plantilla.Alto * escala;

                // Centrar la tarjeta en la página
                var marginX = (pageSize.Width - tarjeta.Width) / 2;
                var marginY = (pageSize.Height - tarjeta.Height) / 2;

                Canvas.SetLeft(tarjeta, Math.Max(0, marginX));
                Canvas.SetTop(tarjeta, Math.Max(0, marginY));

                page.Children.Add(tarjeta);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                document.Pages.Add(pageContent);

                // Imprimir
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                    $"Tarjeta de {abonado.NombreCompleto}");

                // Marcar como impreso solo si la configuración lo permite
                if (configuracion.ConfiguracionImpresion.MarcarComoImpresoAutomaticamente)
                {
                    abonado.Impreso = true;
                }

                // Guardar copia digital si está configurado
                if (configuracion.ConfiguracionImpresion.GuardarCopiaDigital &&
                    !string.IsNullOrEmpty(configuracion.ConfiguracionImpresion.RutaCopiasDigitales))
                {
                    await GuardarCopiaDigitalAsync(abonado, plantilla, configuracion.ConfiguracionImpresion.RutaCopiasDigitales);
                }

                // Actualizar estadísticas de la plantilla
                await ActualizarEstadisticasPlantilla(plantilla.Id, "Impresa");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error imprimiendo tarjeta: {ex.Message}");
                return false;
            }
        }

        public async Task<ResultadoImpresion> ImprimirTarjetaAsync(Abonado abonado, PlantillaTarjeta plantilla, ConfiguracionImpresionEspecifica configuracion)
        {
            var inicio = DateTime.Now;
            var resultado = new ResultadoImpresion();

            try
            {
                // Validar plantilla
                if (plantilla == null)
                {
                    resultado.Errores.Add("Plantilla no válida");
                    return resultado;
                }

                // Validar impresora
                if (!string.IsNullOrEmpty(configuracion.NombreImpresora))
                {
                    var esValida = await ValidarImpresoraAsync(configuracion.NombreImpresora);
                    if (!esValida)
                    {
                        resultado.Errores.Add($"Impresora '{configuracion.NombreImpresora}' no disponible");
                        return resultado;
                    }
                    resultado.ImpresoraUtilizada = configuracion.NombreImpresora;
                }

                // Generar vista previa
                var tarjeta = await GenerarVistaPreviaAsync(abonado, plantilla);
                if (tarjeta == null)
                {
                    resultado.Errores.Add("Error al generar vista previa de la tarjeta");
                    return resultado;
                }

                // Configurar impresión
                var printDialog = new System.Windows.Controls.PrintDialog();

                // Mostrar diálogo si está configurado
                if (configuracion.MostrarDialogoImpresion)
                {
                    if (printDialog.ShowDialog() != true)
                    {
                        resultado.Errores.Add("Impresión cancelada por el usuario");
                        return resultado;
                    }
                }

                // NUEVO: Usar tamaño de página basado en plantilla
                var pageSize = CalcularTamañoPaginaOptimo(plantilla);
                System.Diagnostics.Debug.WriteLine($"Usando tamaño personalizado: {pageSize.Width}x{pageSize.Height}");

                // Crear documento de impresión
                var document = new FixedDocument();
                var page = new FixedPage
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };

                // Aplicar escala y centrado
                var escala = CalcularEscalaOptima(plantilla, pageSize);
                tarjeta.Width = plantilla.Ancho * escala;
                tarjeta.Height = plantilla.Alto * escala;

                var marginX = (pageSize.Width - tarjeta.Width) / 2;
                var marginY = (pageSize.Height - tarjeta.Height) / 2;

                Canvas.SetLeft(tarjeta, Math.Max(0, marginX));
                Canvas.SetTop(tarjeta, Math.Max(0, marginY));

                page.Children.Add(tarjeta);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                document.Pages.Add(pageContent);

                // Imprimir múltiples copias si es necesario
                for (int i = 0; i < configuracion.Copias; i++)
                {
                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                        $"Tarjeta de {abonado.NombreCompleto} ({i + 1}/{configuracion.Copias})");
                }

                // Guardar copia digital si está configurado
                if (configuracion.GuardarCopia && !string.IsNullOrEmpty(configuracion.RutaCopia))
                {
                    await GuardarCopiaDigitalAsync(abonado, plantilla, configuracion.RutaCopia);
                }

                resultado.Exitoso = true;
                resultado.TarjetasImpresas = configuracion.Copias;
                resultado.Mensaje = "Tarjeta impresa correctamente";

                // Actualizar estadísticas
                await ActualizarEstadisticasPlantilla(plantilla.Id, "Impresa");

            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Errores.Add(ex.Message);
                resultado.Mensaje = "Error durante la impresión";
                System.Diagnostics.Debug.WriteLine($"Error imprimiendo tarjeta: {ex.Message}");
            }
            finally
            {
                resultado.TiempoTranscurrido = DateTime.Now - inicio;
            }

            return resultado;
        }

        public async Task<bool> ImprimirTarjetasAsync(List<Abonado> abonados, PlantillaTarjeta plantilla)
        {
            try
            {
                if (!abonados.Any())
                    return false;

                var configuracion = _configuracionService.GetConfiguracion();
                var printDialog = new System.Windows.Controls.PrintDialog();

                // Configurar impresora predeterminada si está disponible
                if (!string.IsNullOrEmpty(configuracion.ConfiguracionImpresion.ImpresoraPredeterminada))
                {
                    var impresoras = GetImpresorasDisponibles();
                    if (impresoras.Contains(configuracion.ConfiguracionImpresion.ImpresoraPredeterminada))
                    {
                        var printer = new System.Drawing.Printing.PrinterSettings();
                        printer.PrinterName = configuracion.ConfiguracionImpresion.ImpresoraPredeterminada;
                    }
                }

                // Mostrar diálogo si está configurado
                if (configuracion.ConfiguracionImpresion.MostrarVistaPrevia)
                {
                    if (printDialog.ShowDialog() != true)
                        return false;
                }

                // NUEVO: Calcular diseño óptimo para múltiples tarjetas
                var pageSize = CalcularTamañoPaginaOptimo(plantilla);
                var tarjetasPorPagina = CalcularTarjetasPorPagina(plantilla, pageSize, configuracion.ConfiguracionImpresion);

                // Configurar documento para múltiples tarjetas
                var document = new FixedDocument();

                for (int i = 0; i < abonados.Count; i += tarjetasPorPagina)
                {
                    var abonadosPagina = abonados.Skip(i).Take(tarjetasPorPagina).ToList();
                    var pagina = await CrearPaginaConTarjetasAsync(abonadosPagina, plantilla, pageSize, configuracion.ConfiguracionImpresion);

                    if (pagina != null)
                    {
                        var pageContent = new PageContent();
                        ((IAddChild)pageContent).AddChild(pagina);
                        document.Pages.Add(pageContent);
                    }
                }

                // Imprimir documento
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                    $"Tarjetas de Abonados ({abonados.Count} tarjetas)");

                // Marcar como impresos
                if (configuracion.ConfiguracionImpresion.MarcarComoImpresoAutomaticamente)
                {
                    foreach (var abonado in abonados)
                    {
                        abonado.Impreso = true;
                    }
                }

                // Guardar copias digitales
                if (configuracion.ConfiguracionImpresion.GuardarCopiaDigital &&
                    !string.IsNullOrEmpty(configuracion.ConfiguracionImpresion.RutaCopiasDigitales))
                {
                    foreach (var abonado in abonados)
                    {
                        await GuardarCopiaDigitalAsync(abonado, plantilla, configuracion.ConfiguracionImpresion.RutaCopiasDigitales);
                    }
                }

                // Actualizar estadísticas
                await ActualizarEstadisticasPlantilla(plantilla.Id, "Impresa", abonados.Count);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error imprimiendo tarjetas: {ex.Message}");
                return false;
            }
        }

        // NUEVOS MÉTODOS PARA CALCULAR TAMAÑO AUTOMÁTICO

        /// <summary>
        /// Calcula el tamaño de página óptimo basado en las dimensiones de la plantilla
        /// </summary>
        private Size CalcularTamañoPaginaOptimo(PlantillaTarjeta plantilla)
        {
            try
            {
                // Convertir píxeles de la plantilla a puntos (1 punto = 1/72 pulgadas, 1 píxel = 1/96 pulgadas)
                var anchoEnPuntos = ConvertirPixelsAPuntos(plantilla.Ancho);
                var altoEnPuntos = ConvertirPixelsAPuntos(plantilla.Alto);

                // Agregar márgenes mínimos (0.5 pulgadas = 36 puntos en cada lado)
                var margenMinimo = 36.0;
                var anchoTotal = anchoEnPuntos + (margenMinimo * 2);
                var altoTotal = altoEnPuntos + (margenMinimo * 2);

                // Verificar si cabe en tamaños estándar y ajustar si es necesario
                var tamañosEstandar = new Dictionary<string, Size>
                {
                    { "A4", new Size(595, 842) },           // 210 x 297 mm
                    { "Letter", new Size(612, 792) },       // 8.5 x 11 in
                    { "A5", new Size(420, 595) },           // 148 x 210 mm
                    { "Card", new Size(252, 360) },         // Tarjeta estándar ampliada
                    { "Custom", new Size(anchoTotal, altoTotal) }
                };

                // Buscar el tamaño estándar más pequeño que contenga la tarjeta
                foreach (var tamaño in tamañosEstandar)
                {
                    if (tamaño.Value.Width >= anchoTotal && tamaño.Value.Height >= altoTotal)
                    {
                        System.Diagnostics.Debug.WriteLine($"Usando tamaño {tamaño.Key}: {tamaño.Value.Width}x{tamaño.Value.Height}");
                        return tamaño.Value;
                    }
                }

                // Si no cabe en ningún estándar, usar tamaño personalizado
                System.Diagnostics.Debug.WriteLine($"Usando tamaño personalizado: {anchoTotal}x{altoTotal}");
                return new Size(anchoTotal, altoTotal);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculando tamaño de página: {ex.Message}");
                // Fallback a A4
                return new Size(595, 842);
            }
        }

        /// <summary>
        /// Calcula la escala óptima para la tarjeta en la página
        /// </summary>
        private double CalcularEscalaOptima(PlantillaTarjeta plantilla, Size pageSize)
        {
            try
            {
                // Calcular área disponible (restando márgenes)
                var margen = 36.0; // 0.5 pulgadas en puntos
                var areaDisponibleAncho = pageSize.Width - (margen * 2);
                var areaDisponibleAlto = pageSize.Height - (margen * 2);

                // Convertir dimensiones de plantilla a puntos
                var plantillaAnchoEnPuntos = ConvertirPixelsAPuntos(plantilla.Ancho);
                var plantillaAltoEnPuntos = ConvertirPixelsAPuntos(plantilla.Alto);

                // Calcular escalas posibles
                var escalaX = areaDisponibleAncho / plantillaAnchoEnPuntos;
                var escalaY = areaDisponibleAlto / plantillaAltoEnPuntos;

                // Usar la menor escala para mantener proporciones
                var escala = Math.Min(escalaX, escalaY);

                // Limitar escala máxima para evitar tarjetas gigantes
                escala = Math.Min(escala, 2.0);

                // Asegurar escala mínima razonable
                escala = Math.Max(escala, 0.5);

                System.Diagnostics.Debug.WriteLine($"Escala calculada: {escala:F2} (escalaX: {escalaX:F2}, escalaY: {escalaY:F2})");
                return escala;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculando escala: {ex.Message}");
                return 1.0; // Escala por defecto
            }
        }

        /// <summary>
        /// Calcula cuántas tarjetas caben por página
        /// </summary>
        private int CalcularTarjetasPorPagina(PlantillaTarjeta plantilla, Size pageSize, ConfiguracionImpresionTarjetas config)
        {
            try
            {
                // Si se especifica un número fijo, usarlo
                if (config.TarjetasPorPagina > 0 && config.TarjetasPorPagina <= 20)
                {
                    return config.TarjetasPorPagina;
                }

                // Calcular automáticamente
                var margen = 36.0; // 0.5 pulgadas
                var espaciadoH = config.EspaciadoHorizontal * 2.83; // mm a puntos
                var espaciadoV = config.EspaciadoVertical * 2.83;

                var areaAncho = pageSize.Width - (margen * 2);
                var areaAlto = pageSize.Height - (margen * 2);

                var tarjetaAnchoEnPuntos = ConvertirPixelsAPuntos(plantilla.Ancho);
                var tarjetaAltoEnPuntos = ConvertirPixelsAPuntos(plantilla.Alto);

                var tarjetasPorFila = Math.Max(1, (int)((areaAncho + espaciadoH) / (tarjetaAnchoEnPuntos + espaciadoH)));
                var tarjetasPorColumna = Math.Max(1, (int)((areaAlto + espaciadoV) / (tarjetaAltoEnPuntos + espaciadoV)));

                var total = tarjetasPorFila * tarjetasPorColumna;

                System.Diagnostics.Debug.WriteLine($"Tarjetas por página calculadas: {total} ({tarjetasPorFila}x{tarjetasPorColumna})");
                return Math.Min(total, 20); // Máximo 20 por página
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculando tarjetas por página: {ex.Message}");
                return 1;
            }
        }

        private async Task GuardarCopiaDigitalAsync(Abonado abonado, PlantillaTarjeta plantilla, string rutaBase)
        {
            try
            {
                // Crear directorio si no existe
                Directory.CreateDirectory(rutaBase);

                // Generar nombre de archivo único
                var nombreArchivo = $"Tarjeta_{abonado.NumeroSocio}_{abonado.Nombre}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var rutaCompleta = Path.Combine(rutaBase, nombreArchivo);

                // Generar imagen de la tarjeta
                var imagen = await GenerarImagenTarjetaAsync(abonado, plantilla);
                if (imagen != null)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(imagen));

                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando copia digital: {ex.Message}");
            }
        }

        private async Task<FixedPage> CrearPaginaConTarjetasAsync(List<Abonado> abonados, PlantillaTarjeta plantilla, Size pageSize, ConfiguracionImpresionTarjetas config)
        {
            try
            {
                var page = new FixedPage
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };

                var margen = 36.0;
                var espaciadoH = config.EspaciadoHorizontal * 2.83; // mm a puntos
                var espaciadoV = config.EspaciadoVertical * 2.83;

                var tarjetaAnchoEnPuntos = ConvertirPixelsAPuntos(plantilla.Ancho);
                var tarjetaAltoEnPuntos = ConvertirPixelsAPuntos(plantilla.Alto);

                // Calcular cuántas tarjetas caben por fila y columna
                var areaAncho = pageSize.Width - (margen * 2);
                var areaAlto = pageSize.Height - (margen * 2);

                var tarjetasPorFila = Math.Max(1, (int)((areaAncho + espaciadoH) / (tarjetaAnchoEnPuntos + espaciadoH)));
                var tarjetasPorColumna = Math.Max(1, (int)((areaAlto + espaciadoV) / (tarjetaAltoEnPuntos + espaciadoV)));

                for (int i = 0; i < abonados.Count && i < (tarjetasPorFila * tarjetasPorColumna); i++)
                {
                    var fila = i / tarjetasPorFila;
                    var columna = i % tarjetasPorFila;

                    var x = margen + (columna * (tarjetaAnchoEnPuntos + espaciadoH));
                    var y = margen + (fila * (tarjetaAltoEnPuntos + espaciadoV));

                    var tarjeta = await GenerarVistaPreviaAsync(abonados[i], plantilla);
                    if (tarjeta != null)
                    {
                        tarjeta.Width = tarjetaAnchoEnPuntos;
                        tarjeta.Height = tarjetaAltoEnPuntos;

                        Canvas.SetLeft(tarjeta, x);
                        Canvas.SetTop(tarjeta, y);

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

        private async Task ActualizarEstadisticasPlantilla(string plantillaId, string accion, int cantidad = 1)
        {
            try
            {
                // Aquí se podría implementar actualización de estadísticas
                // por ahora solo log
                System.Diagnostics.Debug.WriteLine($"Estadísticas: Plantilla {plantillaId} - {accion} - Cantidad: {cantidad}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando estadísticas: {ex.Message}");
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

                // Ordenar elementos por ZIndex
                var elementosOrdenados = plantilla.Elementos.OrderBy(e => e.ZIndex).ToList();

                foreach (var elemento in elementosOrdenados)
                {
                    if (!elemento.Visible) continue;

                    var elementoUI = await CrearElementoUIAsync(elemento, abonado);
                    if (elementoUI != null)
                    {
                        Canvas.SetLeft(elementoUI, elemento.X);
                        Canvas.SetTop(elementoUI, elemento.Y);
                        Canvas.SetZIndex(elementoUI, elemento.ZIndex);

                        // Aplicar transformaciones
                        if (elemento.Rotacion != 0 || elemento.Opacidad != 1.0)
                        {
                            var transform = new RotateTransform(elemento.Rotacion);
                            elementoUI.RenderTransform = transform;
                            elementoUI.Opacity = elemento.Opacidad;
                        }

                        canvas.Children.Add(elementoUI);
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
                if (string.IsNullOrEmpty(nombreImpresora))
                    return false;

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
                return CrearElementoError(elemento, "Error al crear elemento");
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
                {
                    return CrearElementoError(elemento, "Imagen no encontrada");
                }

                var image = new Image
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Stretch = elemento.MantenerAspecto ? Stretch.Uniform : Stretch.Fill
                };

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(elemento.RutaImagen, UriKind.Absolute);
                bitmap.EndInit();
                image.Source = bitmap;

                // Aplicar bordes redondeados si es necesario
                if (elemento.Redondez > 0 || elemento.GrosorBorde > 0)
                {
                    var border = new Border
                    {
                        Width = elemento.Ancho,
                        Height = elemento.Alto,
                        CornerRadius = new CornerRadius(elemento.Redondez),
                        BorderBrush = new SolidColorBrush(elemento.ColorBorde),
                        BorderThickness = new Thickness(elemento.GrosorBorde),
                        Child = image
                    };
                    return border;
                }

                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando imagen: {ex.Message}");
                return CrearElementoError(elemento, "Error al cargar imagen");
            }
        }

        private UIElement CrearCodigoBarrasUI(ElementoCodigoBarras elemento, Abonado abonado)
        {
            try
            {
                var valor = ObtenerValorCampo(abonado, elemento.CampoOrigen);
                if (string.IsNullOrEmpty(valor))
                {
                    return CrearElementoError(elemento, "Sin datos para código");
                }

                var stackPanel = new StackPanel
                {
                    Width = elemento.Ancho,
                    Height = elemento.Alto,
                    Background = new SolidColorBrush(elemento.ColorFondo)
                };

                // Simulación simple de código de barras (rectángulos)
                var barrasCanvas = new Canvas
                {
                    Height = elemento.Alto * 0.7,
                    Background = new SolidColorBrush(elemento.ColorFondo)
                };

                // Generar barras simples basadas en el valor
                var anchoTotal = elemento.Ancho - 10;
                var numeroBarras = Math.Min(valor.Length * 6, 50);
                var anchoBarra = anchoTotal / numeroBarras;

                for (int i = 0; i < numeroBarras; i++)
                {
                    var esBarra = (valor.GetHashCode() + i) % 3 != 0;
                    if (esBarra)
                    {
                        var barra = new Rectangle
                        {
                            Width = anchoBarra * 0.8,
                            Height = barrasCanvas.Height,
                            Fill = Brushes.Black
                        };
                        Canvas.SetLeft(barra, i * anchoBarra + 5);
                        barrasCanvas.Children.Add(barra);
                    }
                }

                stackPanel.Children.Add(barrasCanvas);

                // Agregar texto si está configurado
                if (elemento.MostrarTexto)
                {
                    var textoFormateado = !string.IsNullOrEmpty(elemento.FormatoTexto)
                        ? string.Format(elemento.FormatoTexto, valor)
                        : valor;

                    var textBlock = new TextBlock
                    {
                        Text = textoFormateado,
                        FontFamily = new FontFamily(elemento.FontFamily),
                        FontSize = elemento.FontSize,
                        Foreground = new SolidColorBrush(elemento.ColorTexto),
                        TextAlignment = TextAlignment.Center,
                        Height = elemento.Alto * 0.3
                    };
                    stackPanel.Children.Add(textBlock);
                }

                return stackPanel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando código de barras: {ex.Message}");
                return CrearElementoError(elemento, "Error en código de barras");
            }
        }

        private UIElement CrearCampoDinamicoUI(ElementoCampoDinamico elemento, Abonado abonado)
        {
            try
            {
                var valor = ObtenerValorCampo(abonado, elemento.CampoOrigen);
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
                return CrearElementoError(elemento, "Error en campo dinámico");
            }
        }

        private string ObtenerValorCampo(Abonado abonado, string campo)
        {
            try
            {
                return campo switch
                {
                    "NumeroSocio" => abonado.NumeroSocio.ToString(),
                    "Nombre" => abonado.Nombre ?? "",
                    "Apellidos" => abonado.Apellidos ?? "",
                    "NombreCompleto" => abonado.NombreCompleto ?? "",
                    "DNI" => abonado.DNI ?? "",
                    "Telefono" => abonado.Telefono ?? "",
                    "Email" => abonado.Email ?? "",
                    "Direccion" => abonado.Direccion ?? "",
                    "FechaNacimiento" => abonado.FechaNacimiento.ToString("dd/MM/yyyy"),
                    "CodigoBarras" => abonado.CodigoBarras ?? "",
                    "TallaCamiseta" => abonado.TallaCamiseta ?? "",
                    "Estado" => abonado.Estado.ToString(),
                    "FechaCreacion" => abonado.FechaCreacion.ToString("dd/MM/yyyy"),
                    "Gestor" => abonado.Gestor?.Nombre ?? "",
                    "Peña" => abonado.Peña?.Nombre ?? "",
                    "TipoAbono" => abonado.TipoAbono?.Nombre ?? "",
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

        private double ConvertirPixelsAPuntos(double pixels)
        {
            // 1 punto = 1/72 pulgadas, 1 pixel = 1/96 pulgadas
            return pixels * 72.0 / 96.0;
        }
    }
}