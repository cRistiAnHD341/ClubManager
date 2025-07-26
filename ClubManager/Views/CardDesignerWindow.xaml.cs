using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ClubManager.ViewModels;
using ClubManager.Models;
using ClubManager.Data;

namespace ClubManager.Views
{
    public partial class CardDesignerWindow : Window
    {
        private CardDesignerViewModel? _viewModel;
        private bool _isDragging = false;
        private Point _lastPosition;
        private UIElement? _draggedElement;

        // NUEVO: Variables para el zoom
        private double _zoomFactor = 1.0;
        private const double ZOOM_MIN = 0.1;
        private const double ZOOM_MAX = 5.0;
        private const double ZOOM_STEP = 0.1;
        private ScaleTransform? _scaleTransform;
        private TranslateTransform? _translateTransform;
        private TransformGroup? _transformGroup;

        public CardDesignerWindow()
        {
            InitializeComponent();

            // Inicializar ViewModel antes del Loaded
            _viewModel = new CardDesignerViewModel();
            DataContext = _viewModel;

            // MEJORADO: Agregar teclas de acceso rápido
            KeyDown += CardDesignerWindow_KeyDown;

            Loaded += OnLoaded;
        }

        private void CardDesignerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;

                // Teclas de acceso rápido
                switch (e.Key)
                {
                    case Key.Delete:
                        if (_viewModel.ElementoSeleccionado != null)
                        {
                            _viewModel.EliminarElementoCommand.Execute(null);
                        }
                        break;

                    case Key.S when Keyboard.Modifiers == ModifierKeys.Control:
                        _viewModel.GuardarPlantillaCommand.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.O when Keyboard.Modifiers == ModifierKeys.Control:
                        _viewModel.CargarPlantillaCommand.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.N when Keyboard.Modifiers == ModifierKeys.Control:
                        _viewModel.NuevaPlantillaCommand.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.Escape:
                        if (_viewModel.ElementoSeleccionado != null)
                        {
                            _viewModel.ElementoSeleccionado = null;
                        }
                        break;

                    // Mover elemento seleccionado con flechas
                    case Key.Left:
                        MoverElementoSeleccionado(-1, 0);
                        e.Handled = true;
                        break;
                    case Key.Right:
                        MoverElementoSeleccionado(1, 0);
                        e.Handled = true;
                        break;
                    case Key.Up:
                        MoverElementoSeleccionado(0, -1);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        MoverElementoSeleccionado(0, 1);
                        e.Handled = true;
                        break;

                    // NUEVO: Controles de zoom con teclado
                    case Key.Add:
                    case Key.OemPlus:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            ZoomIn_Click(sender, e);
                            e.Handled = true;
                        }
                        break;

                    case Key.Subtract:
                    case Key.OemMinus:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            ZoomOut_Click(sender, e);
                            e.Handled = true;
                        }
                        break;

                    case Key.D0:
                    case Key.NumPad0:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            ResetearZoom();
                            e.Handled = true;
                        }
                        break;

                    case Key.D9:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            ZoomFit_Click(sender, e);
                            e.Handled = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en KeyDown: {ex.Message}");
            }
        }

        private void MoverElementoSeleccionado(double deltaX, double deltaY)
        {
            try
            {
                if (_viewModel?.ElementoSeleccionado == null) return;

                var elemento = _viewModel.ElementoSeleccionado;
                var incremento = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;

                var newX = Math.Max(0, Math.Min(elemento.X + (deltaX * incremento),
                    _viewModel.PlantillaActual.Ancho - elemento.Ancho));
                var newY = Math.Max(0, Math.Min(elemento.Y + (deltaY * incremento),
                    _viewModel.PlantillaActual.Alto - elemento.Alto));

                elemento.X = newX;
                elemento.Y = newY;

                ActualizarCanvas();
                ActualizarPropiedadesEnTiempoReal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error moviendo elemento: {ex.Message}");
            }
        }

        public CardDesignerWindow(ClubDbContext? dbContext = null) : this()
        {
            // El ViewModel ya se inicializó en el constructor principal
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== CARGANDO CardDesignerWindow ===");

                if (_viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: ViewModel es null!");
                    MessageBox.Show("Error: ViewModel no inicializado", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (CanvasTarjeta == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: CanvasTarjeta es null!");
                    MessageBox.Show("Error: Canvas no encontrado", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Asignando canvas al ViewModel...");
                _viewModel.SetCanvas(CanvasTarjeta);

                System.Diagnostics.Debug.WriteLine("Suscribiendo a PropertyChanged...");
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                // Actualizar el canvas inicialmente para mostrar cualquier elemento existente
                System.Diagnostics.Debug.WriteLine("Actualizando canvas inicial...");
                _viewModel.ActualizarCanvas();

                // Configurar eventos del canvas
                System.Diagnostics.Debug.WriteLine("Configurando eventos del canvas...");
                CanvasTarjeta.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
                CanvasTarjeta.MouseMove += Canvas_MouseMove;
                CanvasTarjeta.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
                CanvasTarjeta.MouseLeave += Canvas_MouseLeave;

                // NUEVO: Configurar zoom
                ConfigurarZoom();

                // Actualizar propiedades inicialmente
                System.Diagnostics.Debug.WriteLine("Actualizando panel de propiedades...");
                ActualizarPanelPropiedades();

                System.Diagnostics.Debug.WriteLine($"Canvas Children Count: {CanvasTarjeta.Children.Count}");
                System.Diagnostics.Debug.WriteLine($"ElementosActuales Count: {_viewModel.ElementosActuales.Count}");

                System.Diagnostics.Debug.WriteLine("=== CardDesignerWindow CARGADO EXITOSAMENTE ===");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error al inicializar diseñador: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"ERROR en OnLoaded: {ex}");
                MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CardDesignerViewModel.ElementoSeleccionado))
            {
                ActualizarPanelPropiedades();
                ResaltarElementoSeleccionado();
            }
        }

        #region Eventos del Mouse y Canvas

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var canvas = sender as Canvas;
                if (canvas == null) return;

                // MEJORADO: Considerar zoom en la posición del mouse
                _lastPosition = e.GetPosition(canvas);

                // Buscar elemento por posición del mouse directamente
                var elementoEncontrado = EncontrarElementoPorPosicion(_lastPosition);

                if (elementoEncontrado != null && _viewModel != null)
                {
                    _viewModel.ElementoSeleccionado = elementoEncontrado;

                    // Buscar el UIElement correspondiente para el arrastre
                    _draggedElement = EncontrarUIElementoPorElemento(elementoEncontrado);

                    if (_draggedElement != null)
                    {
                        _isDragging = true;
                        canvas.CaptureMouse();

                        // Hacer que el elemento seleccionado esté en primer plano durante el arrastre
                        Panel.SetZIndex(_draggedElement, 9999);
                    }
                }
                else
                {
                    // Si no se encuentra elemento, intentar con HitTest como respaldo
                    var hitElement = canvas.InputHitTest(_lastPosition) as UIElement;

                    if (hitElement != null && hitElement != canvas)
                    {
                        var elementoTarjeta = EncontrarElementoPorUI(hitElement);
                        if (elementoTarjeta != null && _viewModel != null)
                        {
                            _viewModel.ElementoSeleccionado = elementoTarjeta;
                            _draggedElement = hitElement;
                            _isDragging = true;
                            canvas.CaptureMouse();
                            Panel.SetZIndex(_draggedElement, 9999);
                        }
                    }
                    else
                    {
                        // Click en área vacía - deseleccionar
                        if (_viewModel != null)
                        {
                            _viewModel.ElementoSeleccionado = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MouseDown: {ex.Message}");
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (!_isDragging || _draggedElement == null || _viewModel?.ElementoSeleccionado == null)
                    return;

                var canvas = sender as Canvas;
                if (canvas == null) return;

                var currentPosition = e.GetPosition(canvas);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                // Actualizar posición del elemento
                var elemento = _viewModel.ElementoSeleccionado;
                var newX = Math.Max(0, Math.Min(elemento.X + deltaX, _viewModel.PlantillaActual.Ancho - elemento.Ancho));
                var newY = Math.Max(0, Math.Min(elemento.Y + deltaY, _viewModel.PlantillaActual.Alto - elemento.Alto));

                elemento.X = newX;
                elemento.Y = newY;

                // Actualizar posición visual
                Canvas.SetLeft(_draggedElement, newX);
                Canvas.SetTop(_draggedElement, newY);

                _lastPosition = currentPosition;

                // MEJORADO: Actualizar propiedades en tiempo real durante el arrastre
                ActualizarPropiedadesEnTiempoReal();

                // También actualizar el borde de selección
                ResaltarElementoSeleccionado();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MouseMove: {ex.Message}");
            }
        }

        private void ActualizarPropiedadesEnTiempoReal()
        {
            try
            {
                if (_viewModel?.ElementoSeleccionado == null) return;

                // Buscar y actualizar solo los TextBox de posición sin recrear todo el panel
                foreach (UIElement child in PropiedadesPanel.Children)
                {
                    if (child is TextBox textBox)
                    {
                        var previousChild = GetPreviousChild(textBox);
                        if (previousChild is TextBlock label)
                        {
                            switch (label.Text)
                            {
                                case "Posición X:":
                                    textBox.Text = _viewModel.ElementoSeleccionado.X.ToString("F0");
                                    break;
                                case "Posición Y:":
                                    textBox.Text = _viewModel.ElementoSeleccionado.Y.ToString("F0");
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando propiedades en tiempo real: {ex.Message}");
            }
        }

        private UIElement? GetPreviousChild(UIElement element)
        {
            var index = PropiedadesPanel.Children.IndexOf(element);
            return index > 0 ? PropiedadesPanel.Children[index - 1] : null;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var canvas = sender as Canvas;
                if (canvas == null) return;

                if (_isDragging)
                {
                    _isDragging = false;

                    // MEJORADO: Restaurar Z-Index original
                    if (_draggedElement != null && _viewModel?.ElementoSeleccionado != null)
                    {
                        Panel.SetZIndex(_draggedElement, _viewModel.ElementoSeleccionado.ZIndex);
                    }

                    _draggedElement = null;
                    canvas.ReleaseMouseCapture();

                    // Actualizar propiedades después del arrastre
                    ActualizarPanelPropiedades();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MouseUp: {ex.Message}");
            }
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDragging)
                {
                    _isDragging = false;

                    // MEJORADO: Restaurar Z-Index original
                    if (_draggedElement != null && _viewModel?.ElementoSeleccionado != null)
                    {
                        Panel.SetZIndex(_draggedElement, _viewModel.ElementoSeleccionado.ZIndex);
                    }

                    _draggedElement = null;

                    var canvas = sender as Canvas;
                    canvas?.ReleaseMouseCapture();

                    // Actualizar propiedades después del arrastre
                    ActualizarPanelPropiedades();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MouseLeave: {ex.Message}");
            }
        }

        #endregion

        #region Funcionalidad de Zoom

        private void ConfigurarZoom()
        {
            try
            {
                // Configurar transformaciones
                _scaleTransform = new ScaleTransform(1.0, 1.0);
                _translateTransform = new TranslateTransform();

                _transformGroup = new TransformGroup();
                _transformGroup.Children.Add(_scaleTransform);
                _transformGroup.Children.Add(_translateTransform);

                // Aplicar transformación al borde de la tarjeta
                TarjetaBorder.RenderTransform = _transformGroup;
                TarjetaBorder.RenderTransformOrigin = new Point(0.5, 0.5);

                // Configurar eventos de zoom
                CanvasScrollViewer.MouseWheel += Canvas_MouseWheel;
                CanvasScrollViewer.PreviewMouseRightButtonDown += Canvas_PreviewMouseRightButtonDown;

                System.Diagnostics.Debug.WriteLine("Zoom configurado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando zoom: {ex.Message}");
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Zoom con Ctrl + rueda del mouse
                    var delta = e.Delta > 0 ? ZOOM_STEP : -ZOOM_STEP;
                    AplicarZoom(delta, e.GetPosition(CanvasScrollViewer));
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MouseWheel: {ex.Message}");
            }
        }

        private void Canvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Click derecho para resetear zoom
                ResetearZoom();
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en clic derecho: {ex.Message}");
            }
        }

        private void AplicarZoom(double delta, Point mousePosition)
        {
            try
            {
                var nuevoZoom = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, _zoomFactor + delta));

                if (Math.Abs(nuevoZoom - _zoomFactor) < 0.01) return;

                // Calcular el punto de zoom relativo al centro de la tarjeta
                var tarjetaCenter = new Point(
                    TarjetaBorder.ActualWidth / 2,
                    TarjetaBorder.ActualHeight / 2);

                // Aplicar la nueva escala
                _scaleTransform!.ScaleX = nuevoZoom;
                _scaleTransform.ScaleY = nuevoZoom;

                _zoomFactor = nuevoZoom;

                // Actualizar la información de zoom si hay un label en la interfaz
                ActualizarInfoZoom();

                System.Diagnostics.Debug.WriteLine($"Zoom aplicado: {_zoomFactor:P0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando zoom: {ex.Message}");
            }
        }

        private void ResetearZoom()
        {
            try
            {
                _zoomFactor = 1.0;
                _scaleTransform!.ScaleX = 1.0;
                _scaleTransform.ScaleY = 1.0;
                _translateTransform!.X = 0;
                _translateTransform.Y = 0;

                ActualizarInfoZoom();

                System.Diagnostics.Debug.WriteLine("Zoom reseteado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reseteando zoom: {ex.Message}");
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            AplicarZoom(ZOOM_STEP, new Point(CanvasScrollViewer.ActualWidth / 2, CanvasScrollViewer.ActualHeight / 2));
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            AplicarZoom(-ZOOM_STEP, new Point(CanvasScrollViewer.ActualWidth / 2, CanvasScrollViewer.ActualHeight / 2));
        }

        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Calcular el zoom para que la tarjeta quepa en el visor
                var scaleX = CanvasScrollViewer.ActualWidth / (TarjetaBorder.ActualWidth + 100);
                var scaleY = CanvasScrollViewer.ActualHeight / (TarjetaBorder.ActualHeight + 100);
                var scale = Math.Min(scaleX, scaleY);

                scale = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, scale));

                _zoomFactor = scale;
                _scaleTransform!.ScaleX = scale;
                _scaleTransform.ScaleY = scale;
                _translateTransform!.X = 0;
                _translateTransform.Y = 0;

                ActualizarInfoZoom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en zoom fit: {ex.Message}");
            }
        }

        private void ResetearZoom_Click(object sender, RoutedEventArgs e)
        {
            ResetearZoom();
        }

        private void ActualizarInfoZoom()
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.EstadoActual = $"Zoom: {_zoomFactor:P0} - Listo para diseñar";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando info de zoom: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Utilidad

        private ElementoTarjeta? EncontrarElementoPorUI(UIElement uiElement)
        {
            try
            {
                if (_viewModel?.ElementosActuales == null) return null;

                // MEJORADO: Buscar por el UIElement directamente y por posición

                // Primero intentar buscar por referencia directa del UIElement
                foreach (var elemento in _viewModel.ElementosActuales)
                {
                    var elementoUI = EncontrarUIElementoPorElemento(elemento);
                    if (elementoUI == uiElement || EsHijoDe(uiElement, elementoUI))
                    {
                        return elemento;
                    }
                }

                // Si no se encuentra, buscar por posición (método de respaldo)
                var left = Canvas.GetLeft(uiElement);
                var top = Canvas.GetTop(uiElement);

                if (!double.IsNaN(left) && !double.IsNaN(top))
                {
                    foreach (var elemento in _viewModel.ElementosActuales)
                    {
                        if (Math.Abs(elemento.X - left) < 1 && Math.Abs(elemento.Y - top) < 1)
                        {
                            return elemento;
                        }
                    }
                }

                // Último intento: buscar por posición del mouse en el canvas
                return EncontrarElementoPorPosicion(Mouse.GetPosition(CanvasTarjeta));
            }
            catch
            {
                return null;
            }
        }

        private UIElement? EncontrarUIElementoPorElemento(ElementoTarjeta elemento)
        {
            foreach (UIElement child in CanvasTarjeta.Children)
            {
                if (child.GetValue(NameProperty) != null && child.GetValue(NameProperty).ToString() == "SelectionBorder")
                    continue;

                var left = Canvas.GetLeft(child);
                var top = Canvas.GetTop(child);

                if (Math.Abs(elemento.X - left) < 1 && Math.Abs(elemento.Y - top) < 1)
                {
                    return child;
                }
            }
            return null;
        }

        private bool EsHijoDe(UIElement hijo, UIElement? padre)
        {
            if (padre == null) return false;

            var current = hijo;
            while (current != null)
            {
                if (current == padre) return true;
                current = VisualTreeHelper.GetParent(current) as UIElement;
            }
            return false;
        }

        private ElementoTarjeta? EncontrarElementoPorPosicion(Point posicion)
        {
            try
            {
                if (_viewModel?.ElementosActuales == null) return null;

                // Buscar el elemento que contiene la posición del mouse
                foreach (var elemento in _viewModel.ElementosActuales.OrderByDescending(e => e.ZIndex))
                {
                    if (posicion.X >= elemento.X && posicion.X <= elemento.X + elemento.Ancho &&
                        posicion.Y >= elemento.Y && posicion.Y <= elemento.Y + elemento.Alto)
                    {
                        return elemento;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void ResaltarElementoSeleccionado()
        {
            try
            {
                // Remover resaltados anteriores
                var elementosARemover = new List<UIElement>();
                foreach (UIElement child in CanvasTarjeta.Children)
                {
                    if (child is Rectangle rect && rect.Name == "SelectionBorder")
                    {
                        elementosARemover.Add(child);
                    }
                }

                foreach (var elemento in elementosARemover)
                {
                    CanvasTarjeta.Children.Remove(elemento);
                }

                // Agregar resaltado al elemento seleccionado
                if (_viewModel?.ElementoSeleccionado != null)
                {
                    var elemento = _viewModel.ElementoSeleccionado;
                    var border = new Rectangle
                    {
                        Name = "SelectionBorder",
                        Width = elemento.Ancho,
                        Height = elemento.Alto,
                        Stroke = Brushes.Red,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection(new double[] { 5, 3 }),
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false
                    };

                    Canvas.SetLeft(border, elemento.X);
                    Canvas.SetTop(border, elemento.Y);
                    Canvas.SetZIndex(border, 1000); // Siempre en la parte superior

                    CanvasTarjeta.Children.Add(border);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al resaltar elemento: {ex.Message}");
            }
        }

        private void ActualizarCanvas()
        {
            try
            {
                _viewModel?.ActualizarCanvas();
                ResaltarElementoSeleccionado();
                System.Diagnostics.Debug.WriteLine($"Canvas actualizado. Elementos: {_viewModel?.ElementosActuales.Count ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando canvas: {ex.Message}");
            }
        }

        #endregion

        #region Panel de Propiedades - MEJORADO

        private void ActualizarPanelPropiedades()
        {
            try
            {
                PropiedadesPanel.Children.Clear();

                if (_viewModel?.ElementoSeleccionado == null)
                {
                    var sinSeleccion = new TextBlock
                    {
                        Text = "Selecciona un elemento para ver sus propiedades",
                        Style = (Style)FindResource("ModernTextBlock"),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = (Brush)FindResource("MutedBrush")
                    };
                    PropiedadesPanel.Children.Add(sinSeleccion);
                    return;
                }

                var elemento = _viewModel.ElementoSeleccionado;

                // Título del elemento
                var titulo = new TextBlock
                {
                    Text = $"📝 {elemento.Tipo}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkBlue,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                PropiedadesPanel.Children.Add(titulo);

                // Propiedades de posición y tamaño (comunes)
                AgregarSeccion("📍 Posición y Tamaño");

                AgregarPropiedadNumerica("Posición X:", elemento.X,
                    (valor) => { elemento.X = valor; ActualizarCanvas(); });

                AgregarPropiedadNumerica("Posición Y:", elemento.Y,
                    (valor) => { elemento.Y = valor; ActualizarCanvas(); });

                AgregarPropiedadNumerica("Ancho:", elemento.Ancho,
                    (valor) => { elemento.Ancho = Math.Max(10, valor); ActualizarCanvas(); });

                AgregarPropiedadNumerica("Alto:", elemento.Alto,
                    (valor) => { elemento.Alto = Math.Max(10, valor); ActualizarCanvas(); });

                AgregarPropiedadNumerica("Z-Index:", elemento.ZIndex,
                    (valor) => { elemento.ZIndex = (int)valor; ActualizarCanvas(); });

                // Propiedades específicas por tipo
                switch (elemento)
                {
                    case ElementoTexto texto:
                        AgregarPropiedadesTexto(texto);
                        break;
                    case ElementoImagen imagen:
                        AgregarPropiedadesImagen(imagen);
                        break;
                    case ElementoCodigoBarras codigo:
                        AgregarPropiedadesCodigoBarras(codigo);
                        break;
                    case ElementoCampoDinamico campo:
                        AgregarPropiedadesCampoDinamico(campo);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando propiedades: {ex.Message}");
            }
        }

        private void AgregarSeccion(string titulo)
        {
            var separador = new Separator
            {
                Margin = new Thickness(0, 10, 0, 10),
                Background = (Brush)FindResource("BorderBrush")
            };
            PropiedadesPanel.Children.Add(separador);

            var tituloBlock = new TextBlock
            {
                Text = titulo,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = (Brush)FindResource("AccentBrush"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            PropiedadesPanel.Children.Add(tituloBlock);
        }

        private void AgregarPropiedadNumerica(string nombre, double valor, Action<double> onChange)
        {
            var label = new TextBlock
            {
                Text = nombre,
                Style = (Style)FindResource("ModernTextBlock")
            };
            PropiedadesPanel.Children.Add(label);

            var textBox = new TextBox
            {
                Text = valor.ToString("F0"),
                Style = (Style)FindResource("EditableTextBox")
            };

            // MEJORADO: Actualización en tiempo real mientras se escribe
            textBox.TextChanged += (s, e) =>
            {
                try
                {
                    if (double.TryParse(textBox.Text, out double nuevoValor))
                    {
                        onChange(nuevoValor);
                        // Actualizar inmediatamente sin esperar a LostFocus
                    }
                }
                catch
                {
                    // Ignorar errores durante la escritura
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                try
                {
                    if (double.TryParse(textBox.Text, out double nuevoValor))
                    {
                        onChange(nuevoValor);
                    }
                    else
                    {
                        textBox.Text = valor.ToString("F0");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Valor inválido: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = valor.ToString("F0");
                }
            };

            PropiedadesPanel.Children.Add(textBox);
        }

        private void AgregarPropiedadTexto(string nombre, string valor, Action<string> onChange)
        {
            var label = new TextBlock
            {
                Text = nombre,
                Style = (Style)FindResource("ModernTextBlock")
            };
            PropiedadesPanel.Children.Add(label);

            var textBox = new TextBox
            {
                Text = valor,
                Style = (Style)FindResource("EditableTextBox")
            };

            // MEJORADO: Actualización en tiempo real
            textBox.TextChanged += (s, e) =>
            {
                try
                {
                    onChange(textBox.Text);
                }
                catch
                {
                    // Ignorar errores durante la escritura
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                try
                {
                    onChange(textBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = valor;
                }
            };

            PropiedadesPanel.Children.Add(textBox);
        }

        private void AgregarPropiedadesTexto(ElementoTexto texto)
        {
            AgregarSeccion("📝 Propiedades de Texto");

            AgregarPropiedadTexto("Texto:", texto.Texto,
                (valor) => { texto.Texto = valor; ActualizarCanvas(); });

            AgregarPropiedadTexto("Fuente:", texto.FontFamily,
                (valor) => { texto.FontFamily = valor; ActualizarCanvas(); });

            AgregarPropiedadNumerica("Tamaño:", texto.FontSize,
                (valor) => { texto.FontSize = Math.Max(6, valor); ActualizarCanvas(); });

            // Selector de color
            AgregarSelectorColor("Color:", texto.Color,
                (color) => { texto.Color = color; ActualizarCanvas(); });

            // Alineación
            AgregarSelectorAlineacion("Alineación:", texto.TextAlignment,
                (alineacion) => { texto.TextAlignment = alineacion; ActualizarCanvas(); });

            // CheckBoxes para formato
            AgregarCheckBox("Negrita:", texto.IsBold,
                (valor) => { texto.IsBold = valor; ActualizarCanvas(); });

            AgregarCheckBox("Cursiva:", texto.IsItalic,
                (valor) => { texto.IsItalic = valor; ActualizarCanvas(); });

            AgregarCheckBox("Subrayado:", texto.IsUnderline,
                (valor) => { texto.IsUnderline = valor; ActualizarCanvas(); });

            AgregarCheckBox("Tamaño automático:", texto.AutoSize,
                (valor) => { texto.AutoSize = valor; ActualizarCanvas(); });
        }

        private void AgregarPropiedadesImagen(ElementoImagen imagen)
        {
            AgregarSeccion("🖼️ Propiedades de Imagen");

            var label = new TextBlock
            {
                Text = "Ruta de imagen:",
                Style = (Style)FindResource("ModernTextBlock")
            };
            PropiedadesPanel.Children.Add(label);

            var pathLabel = new TextBlock
            {
                Text = System.IO.Path.GetFileName(imagen.RutaImagen),
                FontSize = 10,
                Foreground = Brushes.Gray,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 0, 5)
            };
            PropiedadesPanel.Children.Add(pathLabel);

            var button = new Button
            {
                Content = "📁 Cambiar imagen",
                Style = (Style)FindResource("ModernButton"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            button.Click += (s, e) => CambiarImagen(imagen);
            PropiedadesPanel.Children.Add(button);

            AgregarCheckBox("Mantener aspecto:", imagen.MantenerAspecto,
                (valor) => { imagen.MantenerAspecto = valor; ActualizarCanvas(); });

            AgregarPropiedadNumerica("Redondez:", imagen.Redondez,
                (valor) => { imagen.Redondez = Math.Max(0, valor); ActualizarCanvas(); });

            AgregarPropiedadNumerica("Grosor borde:", imagen.GrosorBorde,
                (valor) => { imagen.GrosorBorde = Math.Max(0, valor); ActualizarCanvas(); });

            if (imagen.GrosorBorde > 0)
            {
                AgregarSelectorColor("Color borde:", imagen.ColorBorde,
                    (color) => { imagen.ColorBorde = color; ActualizarCanvas(); });
            }
        }

        private void AgregarPropiedadesCodigoBarras(ElementoCodigoBarras codigo)
        {
            AgregarSeccion("📊 Propiedades de Código de Barras");

            // Campo origen
            AgregarSelectorCampo("Campo origen:", codigo.CampoOrigen,
                (campo) => { codigo.CampoOrigen = campo; ActualizarCanvas(); });

            // Tipo de código
            AgregarComboBox("Tipo de código:", codigo.TipoCodigo, new[] { "Code128", "Code39", "EAN13" },
                (tipo) => { codigo.TipoCodigo = tipo; ActualizarCanvas(); });

            AgregarCheckBox("Mostrar texto:", codigo.MostrarTexto,
                (valor) => { codigo.MostrarTexto = valor; ActualizarCanvas(); });

            if (codigo.MostrarTexto)
            {
                AgregarPropiedadNumerica("Tamaño texto:", codigo.FontSize,
                    (valor) => { codigo.FontSize = Math.Max(6, valor); ActualizarCanvas(); });

                AgregarSelectorColor("Color texto:", codigo.ColorTexto,
                    (color) => { codigo.ColorTexto = color; ActualizarCanvas(); });
            }

            AgregarSelectorColor("Color fondo:", codigo.ColorFondo,
                (color) => { codigo.ColorFondo = color; ActualizarCanvas(); });
        }

        private void AgregarPropiedadesCampoDinamico(ElementoCampoDinamico campo)
        {
            AgregarSeccion("🏷️ Propiedades de Campo Dinámico");

            // Campo origen
            AgregarSelectorCampo("Campo origen:", campo.CampoOrigen,
                (valor) => { campo.CampoOrigen = valor; ActualizarCanvas(); });

            AgregarPropiedadTexto("Prefijo:", campo.Prefijo ?? "",
                (valor) => { campo.Prefijo = valor; ActualizarCanvas(); });

            AgregarPropiedadTexto("Sufijo:", campo.Sufijo ?? "",
                (valor) => { campo.Sufijo = valor; ActualizarCanvas(); });

            AgregarPropiedadTexto("Fuente:", campo.FontFamily,
                (valor) => { campo.FontFamily = valor; ActualizarCanvas(); });

            AgregarPropiedadNumerica("Tamaño:", campo.FontSize,
                (valor) => { campo.FontSize = Math.Max(6, valor); ActualizarCanvas(); });

            AgregarSelectorColor("Color:", campo.Color,
                (color) => { campo.Color = color; ActualizarCanvas(); });

            AgregarSelectorAlineacion("Alineación:", campo.TextAlignment,
                (alineacion) => { campo.TextAlignment = alineacion; ActualizarCanvas(); });

            AgregarCheckBox("Negrita:", campo.IsBold,
                (valor) => { campo.IsBold = valor; ActualizarCanvas(); });

            AgregarCheckBox("Cursiva:", campo.IsItalic,
                (valor) => { campo.IsItalic = valor; ActualizarCanvas(); });
        }

        #endregion

        #region Controles de Propiedades Auxiliares

        private void AgregarCheckBox(string etiqueta, bool valorActual, Action<bool> onChange)
        {
            var checkBox = new CheckBox
            {
                Content = etiqueta,
                IsChecked = valorActual,
                Style = (Style)FindResource("ModernCheckBox"),
                Margin = new Thickness(0, 5, 0, 10)
            };

            checkBox.Checked += (s, e) => onChange(true);
            checkBox.Unchecked += (s, e) => onChange(false);

            PropiedadesPanel.Children.Add(checkBox);
        }

        private void AgregarSelectorColor(string etiqueta, Color colorActual, Action<Color> onChange)
        {
            var label = new TextBlock
            {
                Text = etiqueta,
                Style = (Style)FindResource("ModernTextBlock")
            };
            PropiedadesPanel.Children.Add(label);

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Mostrar color actual
            var colorPreview = new Border
            {
                Width = 30,
                Height = 20,
                Background = new SolidColorBrush(colorActual),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 10, 0)
            };

            // Botón para cambiar color
            var colorButton = new Button
            {
                Content = "Cambiar",
                Style = (Style)FindResource("ModernButton"),
                Width = 80
            };

            colorButton.Click += (s, e) =>
            {
                // Ciclar entre colores comunes
                var colores = new[] { Colors.Black, Colors.White, Colors.Red, Colors.Blue, Colors.Green,
                                    Colors.Yellow, Colors.Orange, Colors.Purple, Colors.Brown, Colors.Gray };
                var indiceActual = Array.IndexOf(colores, colorActual);
                var nuevoColor = colores[(indiceActual + 1) % colores.Length];

                colorPreview.Background = new SolidColorBrush(nuevoColor);
                onChange(nuevoColor);
            };

            stackPanel.Children.Add(colorPreview);
            stackPanel.Children.Add(colorButton);
            PropiedadesPanel.Children.Add(stackPanel);
        }

        private void AgregarSelectorAlineacion(string etiqueta, TextAlignment alineacionActual, Action<TextAlignment> onChange)
        {
            var label = new TextBlock
            {
                Text = etiqueta,
                Style = (Style)FindResource("ModernTextBlock")
            };
            PropiedadesPanel.Children.Add(label);

            var comboBox = new ComboBox
            {
                Style = (Style)FindResource("ModernComboBox"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            comboBox.Items.Add("Left");
            comboBox.Items.Add("Center");
            comboBox.Items.Add("Right");
            comboBox.Items.Add("Justify");
            comboBox.SelectedItem = alineacionActual.ToString();

            comboBox.SelectionChanged += (s, e) =>
            {
                if (Enum.TryParse<TextAlignment>(comboBox.SelectedItem?.ToString(), out var alineacion))
                {
                    onChange(alineacion);
                }
            };

            PropiedadesPanel.Children.Add(comboBox);
        }

        private void AgregarSelectorCampo(string etiqueta, string campoActual, Action<string> onChange)
        {
            var label = new TextBlock
            {
                Text = etiqueta,
                Style = (Style)FindResource("ModernTextBlock")
            };
            PropiedadesPanel.Children.Add(label);

            var comboBox = new ComboBox
            {
                Style = (Style)FindResource("ModernComboBox"),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var campos = new[] { "NombreCompleto", "Nombre", "Apellidos", "NumeroSocio", "DNI",
                               "Telefono", "Email", "Peña", "TipoAbono", "Estado", "FechaNacimiento", "CodigoBarras" };

            foreach (var campo in campos)
            {
                comboBox.Items.Add(campo);
            }

            comboBox.SelectedItem = campoActual;

            comboBox.SelectionChanged += (s, e) =>
            {
                onChange(comboBox.SelectedItem?.ToString() ?? campoActual);
            };

            PropiedadesPanel.Children.Add(comboBox);
        }

        private void AgregarComboBox(string etiqueta, string valorActual, string[] opciones, Action<string> onChange)
        {
            var label = new TextBlock
            {
                Text = etiqueta,
                Style = (Style)FindResource("ModernTextBlock")
            };
            PropiedadesPanel.Children.Add(label);

            var comboBox = new ComboBox
            {
                Style = (Style)FindResource("ModernComboBox"),
                Margin = new Thickness(0, 0, 0, 10)
            };

            foreach (var opcion in opciones)
            {
                comboBox.Items.Add(opcion);
            }

            comboBox.SelectedItem = valorActual;

            comboBox.SelectionChanged += (s, e) =>
            {
                onChange(comboBox.SelectedItem?.ToString() ?? valorActual);
            };

            PropiedadesPanel.Children.Add(comboBox);
        }

        private void CambiarImagen(ElementoImagen imagen)
        {
            try
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Seleccionar imagen"
                };

                if (openDialog.ShowDialog() == true)
                {
                    imagen.RutaImagen = openDialog.FileName;
                    ActualizarCanvas();
                    ActualizarPanelPropiedades(); // Actualizar para mostrar nuevo nombre
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar imagen: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Eventos de la Ventana

        private void CerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Preguntar si quiere guardar antes de cerrar
                if (_viewModel?.ElementosActuales.Count > 0)
                {
                    var result = MessageBox.Show(
                        "¿Desea guardar la plantilla antes de cerrar?",
                        "Guardar cambios",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            _viewModel.GuardarPlantillaCommand.Execute(null);
                            break;
                        case MessageBoxResult.Cancel:
                            return; // No cerrar
                    }
                }

                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar ventana: {ex.Message}");
                Close(); // Cerrar de todas formas
            }
        }

        #endregion

        #region Cleanup y Eventos de Cierre

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar: {ex.Message}");
            }

            base.OnClosed(e);
        }

        #endregion
    }
}