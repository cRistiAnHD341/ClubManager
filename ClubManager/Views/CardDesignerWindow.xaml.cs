using ClubManager.Data;
using ClubManager.Models;
using ClubManager.ViewModels;
using ClubManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;

namespace ClubManager.Views
{
    public partial class CardDesignerWindow : Window
    {
        #region Campos Privados

        private CardDesignerViewModel? _viewModel;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private Point _lastPosition;
        private UIElement? _draggedElement;

        // Variables para el zoom
        private double _zoomFactor = 1.0;
        private const double ZOOM_MIN = 0.1;
        private const double ZOOM_MAX = 5.0;
        private const double ZOOM_STEP = 0.1;
        private ScaleTransform? _scaleTransform;
        private TranslateTransform? _translateTransform;
        private TransformGroup? _transformGroup;

        // Variables para redimensionamiento
        private ResizeMode _resizeMode = ResizeMode.None;
        private Rectangle? _resizeAdorner;
        private readonly List<Rectangle> _resizeHandles = new List<Rectangle>();
        private Point _resizeStartPoint;
        private Size _originalSize;
        private Point _originalPosition;

        private enum ResizeMode
        {
            None,
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left
        }

        #endregion

        #region Constructor e Inicialización

        public CardDesignerWindow()
        {
            InitializeComponent();

            // Inicializar ViewModel antes del Loaded
            _viewModel = new CardDesignerViewModel();
            DataContext = _viewModel;

            // Agregar teclas de acceso rápido
            KeyDown += CardDesignerWindow_KeyDown;

            Loaded += OnLoaded;
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

                // Actualizar el canvas inicialmente
                System.Diagnostics.Debug.WriteLine("Actualizando canvas inicial...");
                _viewModel.ActualizarCanvas();

                // Configurar eventos del canvas
                System.Diagnostics.Debug.WriteLine("Configurando eventos del canvas...");
                ConfigurarEventosCanvas();

                // Configurar zoom
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

        private void ConfigurarEventosCanvas()
        {
            try
            {
                CanvasTarjeta.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
                CanvasTarjeta.MouseMove += Canvas_MouseMove;
                CanvasTarjeta.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
                CanvasTarjeta.MouseLeave += Canvas_MouseLeave;
                CanvasTarjeta.MouseRightButtonDown += Canvas_MouseRightButtonDown;

                System.Diagnostics.Debug.WriteLine("Eventos del canvas configurados correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando eventos del canvas: {ex.Message}");
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(CardDesignerViewModel.ElementoSeleccionado))
                {
                    ActualizarPanelPropiedades();
                    MostrarHandlesRedimensionamiento();

                    var elemento = _viewModel?.ElementoSeleccionado;
                    System.Diagnostics.Debug.WriteLine($"Elemento seleccionado cambió: {elemento?.Tipo ?? "null"}");
                }
                else if (e.PropertyName == nameof(CardDesignerViewModel.PlantillaActual))
                {
                    // Actualizar cuando cambien las dimensiones de la plantilla
                    ActualizarCanvas();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ViewModel_PropertyChanged: {ex.Message}");
            }
        }

        #endregion

        #region Eventos de Teclado

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

                    case Key.D when Keyboard.Modifiers == ModifierKeys.Control:
                        if (_viewModel.ElementoSeleccionado != null)
                        {
                            _viewModel.DuplicarElementoCommand.Execute(null);
                        }
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

                    // Controles de zoom con teclado
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

                    // Ordenar elementos
                    case Key.PageUp:
                        if (Keyboard.Modifiers == ModifierKeys.Control && _viewModel.ElementoSeleccionado != null)
                        {
                            EnviarAlFrente();
                            e.Handled = true;
                        }
                        break;

                    case Key.PageDown:
                        if (Keyboard.Modifiers == ModifierKeys.Control && _viewModel.ElementoSeleccionado != null)
                        {
                            EnviarAtras();
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
                MostrarHandlesRedimensionamiento();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error moviendo elemento: {ex.Message}");
            }
        }

        private void EnviarAlFrente()
        {
            if (_viewModel?.ElementoSeleccionado == null) return;

            var maxZ = _viewModel.ElementosActuales.Max(e => e.ZIndex);
            _viewModel.ElementoSeleccionado.ZIndex = maxZ + 1;
            ActualizarCanvas();
            ActualizarPanelPropiedades();
        }

        private void EnviarAtras()
        {
            if (_viewModel?.ElementoSeleccionado == null) return;

            var minZ = _viewModel.ElementosActuales.Min(e => e.ZIndex);
            _viewModel.ElementoSeleccionado.ZIndex = Math.Max(0, minZ - 1);
            ActualizarCanvas();
            ActualizarPanelPropiedades();
        }

        #endregion

        #region Eventos del Mouse y Canvas

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var canvas = sender as Canvas;
                if (canvas == null) return;

                _lastPosition = e.GetPosition(canvas);

                // Verificar si se hizo clic en un handle de redimensionamiento
                var clickedHandle = EncontrarHandleEnPosicion(_lastPosition);
                if (clickedHandle != null)
                {
                    IniciarRedimensionamiento(clickedHandle, _lastPosition);
                    canvas.CaptureMouse();
                    return;
                }

                // Buscar elemento por posición del mouse
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

                    if (hitElement != null && hitElement != canvas && !EsHandleRedimensionamiento(hitElement))
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
                var canvas = sender as Canvas;
                if (canvas == null) return;

                var currentPosition = e.GetPosition(canvas);

                // Manejar redimensionamiento
                if (_isResizing && _viewModel?.ElementoSeleccionado != null)
                {
                    ProcesarRedimensionamiento(currentPosition);
                    return;
                }

                // Cambiar cursor según la posición del mouse
                ActualizarCursor(currentPosition);

                // Arrastrar elemento
                if (_isDragging && _draggedElement != null && _viewModel?.ElementoSeleccionado != null)
                {
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

                    // Actualizar propiedades en tiempo real durante el arrastre
                    ActualizarPropiedadesEnTiempoReal();

                    // También actualizar los handles de redimensionamiento
                    ActualizarPosicionHandles();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MouseMove: {ex.Message}");
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var canvas = sender as Canvas;
                if (canvas == null) return;

                // Finalizar redimensionamiento
                if (_isResizing)
                {
                    _isResizing = false;
                    _resizeMode = ResizeMode.None;
                    canvas.ReleaseMouseCapture();
                    ActualizarPanelPropiedades();
                    return;
                }

                if (_isDragging)
                {
                    _isDragging = false;

                    // Restaurar Z-Index original
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
                // Resetear cursor al salir del canvas
                CanvasTarjeta.Cursor = Cursors.Arrow;

                if (_isDragging || _isResizing)
                {
                    _isDragging = false;
                    _isResizing = false;
                    _resizeMode = ResizeMode.None;

                    // Restaurar Z-Index original
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

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Menú contextual mejorado
                if (_viewModel?.ElementoSeleccionado != null)
                {
                    var contextMenu = new ContextMenu();

                    // Duplicar
                    var duplicarItem = new MenuItem
                    {
                        Header = "📋 Duplicar",
                        Command = _viewModel.DuplicarElementoCommand
                    };
                    contextMenu.Items.Add(duplicarItem);

                    contextMenu.Items.Add(new Separator());

                    // Ordenar
                    var frenteItem = new MenuItem { Header = "⬆️ Enviar al frente" };
                    frenteItem.Click += (s, args) => EnviarAlFrente();
                    contextMenu.Items.Add(frenteItem);

                    var atrasItem = new MenuItem { Header = "⬇️ Enviar atrás" };
                    atrasItem.Click += (s, args) => EnviarAtras();
                    contextMenu.Items.Add(atrasItem);

                    contextMenu.Items.Add(new Separator());

                    // Centrar
                    var centrarItem = new MenuItem { Header = "🎯 Centrar elemento" };
                    centrarItem.Click += (s, args) => CentrarElemento();
                    contextMenu.Items.Add(centrarItem);

                    contextMenu.Items.Add(new Separator());

                    // Eliminar
                    var eliminarItem = new MenuItem
                    {
                        Header = "🗑️ Eliminar",
                        Command = _viewModel.EliminarElementoCommand
                    };
                    contextMenu.Items.Add(eliminarItem);

                    contextMenu.IsOpen = true;
                }
                else
                {
                    // Resetear zoom si no hay elemento seleccionado
                    ResetearZoom();
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en clic derecho: {ex.Message}");
            }
        }

        private void CentrarElemento()
        {
            if (_viewModel?.ElementoSeleccionado == null) return;

            var elem = _viewModel.ElementoSeleccionado;
            elem.X = (_viewModel.PlantillaActual.Ancho - elem.Ancho) / 2;
            elem.Y = (_viewModel.PlantillaActual.Alto - elem.Alto) / 2;
            ActualizarCanvas();
            ActualizarPanelPropiedades();
        }

        #endregion

        #region Funcionalidad de Redimensionamiento

        private void MostrarHandlesRedimensionamiento()
        {
            try
            {
                // Limpiar handles anteriores
                LimpiarHandlesRedimensionamiento();

                if (_viewModel?.ElementoSeleccionado == null) return;

                var elemento = _viewModel.ElementoSeleccionado;

                // Crear handles de redimensionamiento
                CrearHandles(elemento);

                System.Diagnostics.Debug.WriteLine($"Handles de redimensionamiento creados para {elemento.Tipo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando handles: {ex.Message}");
            }
        }

        private void CrearHandles(ElementoTarjeta elemento)
        {
            var handleSize = 8.0;
            var handleColor = Brushes.Blue;
            var handleBorder = Brushes.DarkBlue;

            // Posiciones de los handles
            var handles = new[]
            {
                new { Mode = ResizeMode.TopLeft, X = elemento.X - handleSize/2, Y = elemento.Y - handleSize/2, Cursor = Cursors.SizeNWSE },
                new { Mode = ResizeMode.Top, X = elemento.X + elemento.Ancho/2 - handleSize/2, Y = elemento.Y - handleSize/2, Cursor = Cursors.SizeNS },
                new { Mode = ResizeMode.TopRight, X = elemento.X + elemento.Ancho - handleSize/2, Y = elemento.Y - handleSize/2, Cursor = Cursors.SizeNESW },
                new { Mode = ResizeMode.Right, X = elemento.X + elemento.Ancho - handleSize/2, Y = elemento.Y + elemento.Alto/2 - handleSize/2, Cursor = Cursors.SizeWE },
                new { Mode = ResizeMode.BottomRight, X = elemento.X + elemento.Ancho - handleSize/2, Y = elemento.Y + elemento.Alto - handleSize/2, Cursor = Cursors.SizeNWSE },
                new { Mode = ResizeMode.Bottom, X = elemento.X + elemento.Ancho/2 - handleSize/2, Y = elemento.Y + elemento.Alto - handleSize/2, Cursor = Cursors.SizeNS },
                new { Mode = ResizeMode.BottomLeft, X = elemento.X - handleSize/2, Y = elemento.Y + elemento.Alto - handleSize/2, Cursor = Cursors.SizeNESW },
                new { Mode = ResizeMode.Left, X = elemento.X - handleSize/2, Y = elemento.Y + elemento.Alto/2 - handleSize/2, Cursor = Cursors.SizeWE }
            };

            foreach (var handleInfo in handles)
            {
                var handle = new Rectangle
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = handleColor,
                    Stroke = handleBorder,
                    StrokeThickness = 1,
                    Tag = handleInfo.Mode,
                    Cursor = handleInfo.Cursor
                };

                Canvas.SetLeft(handle, handleInfo.X);
                Canvas.SetTop(handle, handleInfo.Y);
                Canvas.SetZIndex(handle, 2000); // Siempre visible

                CanvasTarjeta.Children.Add(handle);
                _resizeHandles.Add(handle);
            }

            // Crear borde de selección
            var selectionBorder = new Rectangle
            {
                Width = elemento.Ancho,
                Height = elemento.Alto,
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new double[] { 3, 3 }),
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(selectionBorder, elemento.X);
            Canvas.SetTop(selectionBorder, elemento.Y);
            Canvas.SetZIndex(selectionBorder, 1999);

            CanvasTarjeta.Children.Add(selectionBorder);
            _resizeHandles.Add(selectionBorder);
        }

        private void LimpiarHandlesRedimensionamiento()
        {
            try
            {
                foreach (var handle in _resizeHandles)
                {
                    CanvasTarjeta.Children.Remove(handle);
                }
                _resizeHandles.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error limpiando handles: {ex.Message}");
            }
        }

        private Rectangle? EncontrarHandleEnPosicion(Point position)
        {
            foreach (var handle in _resizeHandles)
            {
                if (handle.Tag is ResizeMode && handle is Rectangle rect)
                {
                    var left = Canvas.GetLeft(rect);
                    var top = Canvas.GetTop(rect);

                    if (position.X >= left && position.X <= left + rect.Width &&
                        position.Y >= top && position.Y <= top + rect.Height)
                    {
                        return rect;
                    }
                }
            }
            return null;
        }

        private bool EsHandleRedimensionamiento(UIElement element)
        {
            return _resizeHandles.Contains(element);
        }

        private void IniciarRedimensionamiento(Rectangle handle, Point startPoint)
        {
            try
            {
                if (_viewModel?.ElementoSeleccionado == null || !(handle.Tag is ResizeMode mode))
                    return;

                _isResizing = true;
                _resizeMode = mode;
                _resizeStartPoint = startPoint;
                _originalSize = new Size(_viewModel.ElementoSeleccionado.Ancho, _viewModel.ElementoSeleccionado.Alto);
                _originalPosition = new Point(_viewModel.ElementoSeleccionado.X, _viewModel.ElementoSeleccionado.Y);

                // Cambiar cursor
                CanvasTarjeta.Cursor = handle.Cursor;

                System.Diagnostics.Debug.WriteLine($"Iniciando redimensionamiento: {mode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error iniciando redimensionamiento: {ex.Message}");
            }
        }

        private void ProcesarRedimensionamiento(Point currentPoint)
        {
            try
            {
                if (_viewModel?.ElementoSeleccionado == null || _resizeMode == ResizeMode.None)
                    return;

                var elemento = _viewModel.ElementoSeleccionado;
                var deltaX = currentPoint.X - _resizeStartPoint.X;
                var deltaY = currentPoint.Y - _resizeStartPoint.Y;

                var newX = _originalPosition.X;
                var newY = _originalPosition.Y;
                var newWidth = _originalSize.Width;
                var newHeight = _originalSize.Height;

                // Aplicar cambios según el modo de redimensionamiento
                switch (_resizeMode)
                {
                    case ResizeMode.TopLeft:
                        newX = _originalPosition.X + deltaX;
                        newY = _originalPosition.Y + deltaY;
                        newWidth = _originalSize.Width - deltaX;
                        newHeight = _originalSize.Height - deltaY;
                        break;

                    case ResizeMode.Top:
                        newY = _originalPosition.Y + deltaY;
                        newHeight = _originalSize.Height - deltaY;
                        break;

                    case ResizeMode.TopRight:
                        newY = _originalPosition.Y + deltaY;
                        newWidth = _originalSize.Width + deltaX;
                        newHeight = _originalSize.Height - deltaY;
                        break;

                    case ResizeMode.Right:
                        newWidth = _originalSize.Width + deltaX;
                        break;

                    case ResizeMode.BottomRight:
                        newWidth = _originalSize.Width + deltaX;
                        newHeight = _originalSize.Height + deltaY;
                        break;

                    case ResizeMode.Bottom:
                        newHeight = _originalSize.Height + deltaY;
                        break;

                    case ResizeMode.BottomLeft:
                        newX = _originalPosition.X + deltaX;
                        newWidth = _originalSize.Width - deltaX;
                        newHeight = _originalSize.Height + deltaY;
                        break;

                    case ResizeMode.Left:
                        newX = _originalPosition.X + deltaX;
                        newWidth = _originalSize.Width - deltaX;
                        break;
                }

                // Aplicar límites mínimos
                var minSize = 10.0;
                newWidth = Math.Max(minSize, newWidth);
                newHeight = Math.Max(minSize, newHeight);

                // Aplicar límites de canvas
                newX = Math.Max(0, Math.Min(newX, _viewModel.PlantillaActual.Ancho - newWidth));
                newY = Math.Max(0, Math.Min(newY, _viewModel.PlantillaActual.Alto - newHeight));

                // Verificar que no se salga del canvas
                if (newX + newWidth > _viewModel.PlantillaActual.Ancho)
                {
                    newWidth = _viewModel.PlantillaActual.Ancho - newX;
                }
                if (newY + newHeight > _viewModel.PlantillaActual.Alto)
                {
                    newHeight = _viewModel.PlantillaActual.Alto - newY;
                }

                // Actualizar elemento
                elemento.X = newX;
                elemento.Y = newY;
                elemento.Ancho = newWidth;
                elemento.Alto = newHeight;

                // Actualizar UI
                ActualizarElementoUI(elemento);
                ActualizarPosicionHandles();
                ActualizarPropiedadesEnTiempoReal();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error procesando redimensionamiento: {ex.Message}");
            }
        }

        private void ActualizarElementoUI(ElementoTarjeta elemento)
        {
            try
            {
                var uiElement = EncontrarUIElementoPorElemento(elemento);
                if (uiElement != null)
                {
                    Canvas.SetLeft(uiElement, elemento.X);
                    Canvas.SetTop(uiElement, elemento.Y);

                    if (uiElement is FrameworkElement fe)
                    {
                        fe.Width = elemento.Ancho;
                        fe.Height = elemento.Alto;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando UI del elemento: {ex.Message}");
            }
        }

        private void ActualizarPosicionHandles()
        {
            try
            {
                if (_viewModel?.ElementoSeleccionado == null) return;

                // Recrear handles en las nuevas posiciones
                MostrarHandlesRedimensionamiento();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando posición de handles: {ex.Message}");
            }
        }

        private void ActualizarCursor(Point mousePosition)
        {
            try
            {
                if (_isResizing || _isDragging) return;

                var handle = EncontrarHandleEnPosicion(mousePosition);
                if (handle != null)
                {
                    CanvasTarjeta.Cursor = handle.Cursor;
                }
                else
                {
                    // Verificar si está sobre un elemento
                    var elemento = EncontrarElementoPorPosicion(mousePosition);
                    CanvasTarjeta.Cursor = elemento != null ? Cursors.Hand : Cursors.Arrow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando cursor: {ex.Message}");
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
                CanvasScrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;

                System.Diagnostics.Debug.WriteLine("Zoom configurado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando zoom: {ex.Message}");
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                // Si Ctrl está presionado, manejar el zoom nosotros y no el ScrollViewer
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    e.Handled = true; // Prevenir que el ScrollViewer procese el evento
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PreviewMouseWheel: {ex.Message}");
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
                // Click derecho para resetear zoom solo si no hay elemento seleccionado
                if (_viewModel?.ElementoSeleccionado == null)
                {
                    ResetearZoom();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en clic derecho zoom: {ex.Message}");
            }
        }

        private void AplicarZoom(double delta, Point mousePosition)
        {
            try
            {
                var nuevoZoom = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, _zoomFactor + delta));

                if (Math.Abs(nuevoZoom - _zoomFactor) < 0.01) return;

                // Aplicar zoom directo
                _scaleTransform!.ScaleX = nuevoZoom;
                _scaleTransform.ScaleY = nuevoZoom;

                _zoomFactor = nuevoZoom;

                // Actualizar layout después del zoom
                CanvasScrollViewer.UpdateLayout();

                // Actualizar la información de zoom
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

                // Centrar la vista después del reset
                CanvasScrollViewer.UpdateLayout();
                CanvasScrollViewer.ScrollToHorizontalOffset(0);
                CanvasScrollViewer.ScrollToVerticalOffset(0);

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
                // Calcular zoom fit considerando el tamaño actual de la tarjeta
                CanvasScrollViewer.UpdateLayout();

                var availableWidth = CanvasScrollViewer.ActualWidth - 20; // Margen
                var availableHeight = CanvasScrollViewer.ActualHeight - 20; // Margen

                var scaleX = availableWidth / (_viewModel?.PlantillaActual.Ancho ?? 350);
                var scaleY = availableHeight / (_viewModel?.PlantillaActual.Alto ?? 220);
                var scale = Math.Min(scaleX, scaleY);

                scale = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, scale));

                _zoomFactor = scale;
                _scaleTransform!.ScaleX = scale;
                _scaleTransform.ScaleY = scale;
                _translateTransform!.X = 0;
                _translateTransform.Y = 0;

                // Centrar la vista después del zoom fit
                CanvasScrollViewer.UpdateLayout();

                var contentWidth = TarjetaBorder.ActualWidth * scale;
                var contentHeight = TarjetaBorder.ActualHeight * scale;

                var centerX = Math.Max(0, (contentWidth - CanvasScrollViewer.ActualWidth) / 2);
                var centerY = Math.Max(0, (contentHeight - CanvasScrollViewer.ActualHeight) / 2);

                CanvasScrollViewer.ScrollToHorizontalOffset(centerX);
                CanvasScrollViewer.ScrollToVerticalOffset(centerY);

                ActualizarInfoZoom();

                System.Diagnostics.Debug.WriteLine($"Zoom fit aplicado: {scale:P0}");
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

                // Buscar por el UIElement directamente y por posición
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

                // Buscar el elemento que contiene la posición del mouse (por Z-Index descendente)
                foreach (var elemento in _viewModel.ElementosActuales.OrderByDescending(e => e.ZIndex))
                {
                    if (elemento.Visible &&
                        posicion.X >= elemento.X && posicion.X <= elemento.X + elemento.Ancho &&
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

        private void ActualizarCanvas()
        {
            try
            {
                _viewModel?.ActualizarCanvas();
                MostrarHandlesRedimensionamiento();
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
                System.Diagnostics.Debug.WriteLine("=== ACTUALIZANDO PANEL DE PROPIEDADES MEJORADO ===");

                if (PropiedadesPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: PropiedadesPanel es null!");
                    return;
                }

                PropiedadesPanel.Children.Clear();

                if (_viewModel?.ElementoSeleccionado == null)
                {
                    MostrarPanelSinSeleccion();
                    return;
                }

                var elemento = _viewModel.ElementoSeleccionado;
                System.Diagnostics.Debug.WriteLine($"Creando propiedades para: {elemento.Tipo}");

                // Título con icono según tipo
                var icono = elemento.Tipo switch
                {
                    "Texto" => "📝",
                    "Imagen" => "🖼️",
                    "Código de Barras" => "📊",
                    "Campo Dinámico" => "🏷️",
                    _ => "📄"
                };

                var titulo = new TextBlock
                {
                    Text = $"{icono} {elemento.Tipo}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkBlue,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                PropiedadesPanel.Children.Add(titulo);

                // ID del elemento (para debugging)
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    var idText = new TextBlock
                    {
                        Text = $"ID: {elemento.Id.Substring(0, 8)}...",
                        FontSize = 9,
                        Foreground = Brushes.LightGray,
                        Margin = new Thickness(0, -10, 0, 10)
                    };
                    PropiedadesPanel.Children.Add(idText);
                }

                // Propiedades básicas (posición y tamaño)
                AgregarSeccion("📍 Posición y Tamaño");

                AgregarPropiedadNumerica("X:", elemento.X,
                    (valor) => {
                        elemento.X = Math.Max(0, Math.Min(valor, _viewModel.PlantillaActual.Ancho - elemento.Ancho));
                        ActualizarCanvas();
                    });

                AgregarPropiedadNumerica("Y:", elemento.Y,
                    (valor) => {
                        elemento.Y = Math.Max(0, Math.Min(valor, _viewModel.PlantillaActual.Alto - elemento.Alto));
                        ActualizarCanvas();
                    });

                AgregarPropiedadNumerica("Ancho:", elemento.Ancho,
                    (valor) => {
                        elemento.Ancho = Math.Max(10, Math.Min(valor, _viewModel.PlantillaActual.Ancho - elemento.X));
                        ActualizarCanvas();
                    });

                AgregarPropiedadNumerica("Alto:", elemento.Alto,
                    (valor) => {
                        elemento.Alto = Math.Max(10, Math.Min(valor, _viewModel.PlantillaActual.Alto - elemento.Y));
                        ActualizarCanvas();
                    });

                AgregarPropiedadNumerica("Orden (Z):", elemento.ZIndex,
                    (valor) => {
                        elemento.ZIndex = Math.Max(0, (int)valor);
                        ActualizarCanvas();
                    });

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

                // Propiedades avanzadas (comunes a todos)
                AgregarPropiedadesAvanzadas(elemento);

                // Validación en tiempo real
                ValidarElementoEnTiempoReal(elemento);

                System.Diagnostics.Debug.WriteLine($"Panel actualizado con {PropiedadesPanel.Children.Count} controles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR actualizando propiedades: {ex}");
                MostrarErrorEnPanel(ex.Message);
            }
        }

        private void MostrarPanelSinSeleccion()
        {
            var sinSeleccion = new StackPanel();

            sinSeleccion.Children.Add(new TextBlock
            {
                Text = "💡 Ningún elemento seleccionado",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.DarkBlue,
                Margin = new Thickness(0, 0, 0, 10)
            });

            sinSeleccion.Children.Add(new TextBlock
            {
                Text = "• Haz clic en cualquier elemento de la tarjeta para seleccionarlo\n" +
                       "• Usa las herramientas del panel izquierdo para agregar elementos\n" +
                       "• Arrastra elementos para moverlos\n" +
                       "• Usa los cuadros azules para redimensionar",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Gray,
                FontSize = 12,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Estadísticas de la plantilla
            if (_viewModel?.ElementosActuales.Count > 0)
            {
                sinSeleccion.Children.Add(new TextBlock
                {
                    Text = "📊 Estadísticas de la plantilla:",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 10, 0, 5)
                });

                var elementosPorTipo = _viewModel.ElementosActuales
                    .GroupBy(e => e.Tipo)
                    .Select(g => $"• {g.Key}: {g.Count()}")
                    .ToArray();

                var stats = new TextBlock
                {
                    Text = $"Total elementos: {_viewModel.ElementosActuales.Count}\n" +
                           string.Join("\n", elementosPorTipo),
                    FontSize = 11,
                    Foreground = Brushes.DarkGray,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                sinSeleccion.Children.Add(stats);

                // Botón para ver estadísticas completas
                var statsBtn = new Button
                {
                    Content = "📈 Ver estadísticas completas",
                    Height = 30,
                    Margin = new Thickness(0, 5, 0, 0),
                    Background = Brushes.LightBlue,
                    BorderBrush = Brushes.Blue
                };
                statsBtn.Click += (s, e) => MostrarEstadisticasCompletas();
                sinSeleccion.Children.Add(statsBtn);
            }

            PropiedadesPanel.Children.Add(sinSeleccion);
        }

        private void MostrarErrorEnPanel(string mensaje)
        {
            if (PropiedadesPanel != null)
            {
                PropiedadesPanel.Children.Clear();
                var errorText = new TextBlock
                {
                    Text = $"❌ Error al cargar propiedades:\n{mensaje}",
                    Foreground = Brushes.Red,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 10),
                    FontSize = 11
                };
                PropiedadesPanel.Children.Add(errorText);
            }
        }

        private void MostrarEstadisticasCompletas()
        {
            try
            {
                if (_viewModel == null) return;

                var stats = _viewModel.ObtenerEstadisticasPlantilla();

                var ventanaStats = new Window
                {
                    Title = "Estadísticas de la Plantilla",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(15)
                };

                var textBlock = new TextBlock
                {
                    Text = stats,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                };

                scrollViewer.Content = textBlock;
                ventanaStats.Content = scrollViewer;
                ventanaStats.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error mostrando estadísticas: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Controles de Propiedades Auxiliares

        private void AgregarSeccion(string titulo)
        {
            try
            {
                var separador = new Separator
                {
                    Margin = new Thickness(0, 10, 0, 10),
                    Background = Brushes.LightGray
                };
                PropiedadesPanel.Children.Add(separador);

                var tituloBlock = new TextBlock
                {
                    Text = titulo,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = Brushes.DarkBlue,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                PropiedadesPanel.Children.Add(tituloBlock);

                System.Diagnostics.Debug.WriteLine($"Sección agregada: {titulo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando sección: {ex.Message}");
            }
        }

        private void AgregarPropiedadNumerica(string nombre, double valor, Action<double> onChange)
        {
            try
            {
                var label = new TextBlock
                {
                    Text = nombre,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                PropiedadesPanel.Children.Add(label);

                var textBox = new TextBox
                {
                    Text = valor.ToString("F1"),
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(5, 5, 5, 5),
                    FontSize = 12,
                    Height = 25,
                    Margin = new Thickness(0, 2, 0, 8)
                };

                // Validación en tiempo real
                textBox.TextChanged += (s, e) =>
                {
                    try
                    {
                        if (double.TryParse(textBox.Text, out double nuevoValor))
                        {
                            textBox.Background = Brushes.White;
                            onChange(nuevoValor);
                        }
                        else
                        {
                            textBox.Background = Brushes.LightPink;
                        }
                    }
                    catch
                    {
                        textBox.Background = Brushes.LightPink;
                    }
                };

                textBox.LostFocus += (s, e) =>
                {
                    try
                    {
                        if (double.TryParse(textBox.Text, out double nuevoValor))
                        {
                            onChange(nuevoValor);
                            textBox.Background = Brushes.White;
                        }
                        else
                        {
                            textBox.Text = valor.ToString("F1");
                            textBox.Background = Brushes.White;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Valor inválido: {ex.Message}", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        textBox.Text = valor.ToString("F1");
                        textBox.Background = Brushes.White;
                    }
                };

                PropiedadesPanel.Children.Add(textBox);
                System.Diagnostics.Debug.WriteLine($"Propiedad numérica agregada: {nombre} = {valor}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando propiedad numérica: {ex.Message}");
            }
        }

        private void AgregarPropiedadTexto(string nombre, string valor, Action<string> onChange)
        {
            try
            {
                var label = new TextBlock
                {
                    Text = nombre,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                PropiedadesPanel.Children.Add(label);

                var textBox = new TextBox
                {
                    Text = valor,
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(5, 5, 5, 5),
                    FontSize = 12,
                    Height = 25,
                    Margin = new Thickness(0, 2, 0, 8)
                };

                // Actualización en tiempo real
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
                System.Diagnostics.Debug.WriteLine($"Propiedad texto agregada: {nombre} = {valor}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando propiedad texto: {ex.Message}");
            }
        }

        private void AgregarCheckBox(string etiqueta, bool valorActual, Action<bool> onChange)
        {
            try
            {
                var checkBox = new CheckBox
                {
                    Content = etiqueta,
                    IsChecked = valorActual,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 10)
                };

                checkBox.Checked += (s, e) => onChange(true);
                checkBox.Unchecked += (s, e) => onChange(false);

                PropiedadesPanel.Children.Add(checkBox);
                System.Diagnostics.Debug.WriteLine($"CheckBox agregado: {etiqueta} = {valorActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando checkbox: {ex.Message}");
            }
        }

        private void AgregarSelectorColor(string etiqueta, Color colorActual, Action<Color> onChange)
        {
            try
            {
                var label = new TextBlock
                {
                    Text = etiqueta,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                PropiedadesPanel.Children.Add(label);

                // Grid con colores predefinidos
                var colorGrid = new UniformGrid
                {
                    Columns = 5,
                    Rows = 3,
                    Margin = new Thickness(0, 2, 0, 10)
                };

                var colores = new[] {
                    Colors.Black, Colors.White, Colors.Red, Colors.Blue, Colors.Green,
                    Colors.Yellow, Colors.Orange, Colors.Purple, Colors.Brown, Colors.Gray,
                    Colors.DarkBlue, Colors.DarkRed, Colors.DarkGreen, Colors.LightBlue, Colors.LightGray
                };

                foreach (var color in colores)
                {
                    var colorButton = new Button
                    {
                        Width = 25,
                        Height = 20,
                        Background = new SolidColorBrush(color),
                        BorderBrush = color == colorActual ? Brushes.DarkBlue : Brushes.Gray,
                        BorderThickness = color == colorActual ? new Thickness(3) : new Thickness(1),
                        Margin = new Thickness(1),
                        ToolTip = color.ToString()
                    };

                    colorButton.Click += (s, e) =>
                    {
                        // Actualizar todos los botones para quitar selección
                        foreach (Button btn in colorGrid.Children)
                        {
                            btn.BorderBrush = Brushes.Gray;
                            btn.BorderThickness = new Thickness(1);
                        }

                        // Marcar el botón seleccionado
                        colorButton.BorderBrush = Brushes.DarkBlue;
                        colorButton.BorderThickness = new Thickness(3);

                        onChange(color);
                    };

                    colorGrid.Children.Add(colorButton);
                }

                PropiedadesPanel.Children.Add(colorGrid);

                System.Diagnostics.Debug.WriteLine($"Selector color agregado: {etiqueta}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando selector color: {ex.Message}");
            }
        }

        private void AgregarSelectorAlineacion(string etiqueta, TextAlignment alineacionActual, Action<TextAlignment> onChange)
        {
            try
            {
                var label = new TextBlock
                {
                    Text = etiqueta,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                PropiedadesPanel.Children.Add(label);

                var comboBox = new ComboBox
                {
                    FontSize = 12,
                    Height = 25,
                    Margin = new Thickness(0, 2, 0, 10),
                    Background = Brushes.White
                };

                var alineaciones = new[]
                {
                    new { Value = TextAlignment.Left, Display = "⬅️ Izquierda" },
                    new { Value = TextAlignment.Center, Display = "⬅️➡️ Centro" },
                    new { Value = TextAlignment.Right, Display = "➡️ Derecha" },
                    new { Value = TextAlignment.Justify, Display = "↔️ Justificado" }
                };

                foreach (var alineacion in alineaciones)
                {
                    var item = new ComboBoxItem
                    {
                        Content = alineacion.Display,
                        Tag = alineacion.Value
                    };
                    comboBox.Items.Add(item);

                    if (alineacion.Value == alineacionActual)
                        comboBox.SelectedItem = item;
                }

                comboBox.SelectionChanged += (s, e) =>
                {
                    if (comboBox.SelectedItem is ComboBoxItem item && item.Tag is TextAlignment alignment)
                    {
                        onChange(alignment);
                    }
                };

                PropiedadesPanel.Children.Add(comboBox);
                System.Diagnostics.Debug.WriteLine($"Selector alineación agregado: {etiqueta}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando selector alineación: {ex.Message}");
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
                                case "X:":
                                    if (textBox.Text != _viewModel.ElementoSeleccionado.X.ToString("F1"))
                                        textBox.Text = _viewModel.ElementoSeleccionado.X.ToString("F1");
                                    break;
                                case "Y:":
                                    if (textBox.Text != _viewModel.ElementoSeleccionado.Y.ToString("F1"))
                                        textBox.Text = _viewModel.ElementoSeleccionado.Y.ToString("F1");
                                    break;
                                case "Ancho:":
                                    if (textBox.Text != _viewModel.ElementoSeleccionado.Ancho.ToString("F1"))
                                        textBox.Text = _viewModel.ElementoSeleccionado.Ancho.ToString("F1");
                                    break;
                                case "Alto:":
                                    if (textBox.Text != _viewModel.ElementoSeleccionado.Alto.ToString("F1"))
                                        textBox.Text = _viewModel.ElementoSeleccionado.Alto.ToString("F1");
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

        private void ValidarElementoEnTiempoReal(ElementoTarjeta elemento)
        {
            try
            {
                var problemas = new List<string>();

                // Validar posición
                if (elemento.X < 0) problemas.Add("X negativa");
                if (elemento.Y < 0) problemas.Add("Y negativa");
                if (elemento.X + elemento.Ancho > _viewModel?.PlantillaActual.Ancho)
                    problemas.Add("Se sale por la derecha");
                if (elemento.Y + elemento.Alto > _viewModel?.PlantillaActual.Alto)
                    problemas.Add("Se sale por abajo");

                // Validar tamaño
                if (elemento.Ancho <= 0) problemas.Add("Ancho inválido");
                if (elemento.Alto <= 0) problemas.Add("Alto inválido");

                // Mostrar problemas si existen
                if (problemas.Any())
                {
                    var avisoPanel = PropiedadesPanel.Children
                        .OfType<Border>()
                        .FirstOrDefault(b => b.Name == "AvisoValidacion");

                    if (avisoPanel == null)
                    {
                        avisoPanel = new Border
                        {
                            Name = "AvisoValidacion",
                            Background = Brushes.LightYellow,
                            BorderBrush = Brushes.Orange,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(8),
                            Margin = new Thickness(0, 5, 0, 10)
                        };

                        var avisoText = new TextBlock
                        {
                            FontSize = 10,
                            Foreground = Brushes.DarkOrange,
                            TextWrapping = TextWrapping.Wrap
                        };

                        avisoPanel.Child = avisoText;
                        PropiedadesPanel.Children.Add(avisoPanel);
                    }

                    if (avisoPanel.Child is TextBlock avisoTextBlock)
                    {
                        avisoTextBlock.Text = $"⚠️ Problemas detectados:\n• {string.Join("\n• ", problemas)}";
                    }
                }
                else
                {
                    // Remover aviso si no hay problemas
                    var avisoPanel = PropiedadesPanel.Children
                        .OfType<Border>()
                        .FirstOrDefault(b => b.Name == "AvisoValidacion");

                    if (avisoPanel != null)
                    {
                        PropiedadesPanel.Children.Remove(avisoPanel);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en validación: {ex.Message}");
            }
        }

        #endregion
        #region Propiedades Específicas por Tipo

        private void AgregarPropiedadesTexto(ElementoTexto texto)
        {
            AgregarSeccion("📝 Propiedades de Texto");

            AgregarPropiedadTexto("Texto:", texto.Texto,
                (valor) => { texto.Texto = valor; ActualizarCanvas(); });

            AgregarPropiedadTexto("Fuente:", texto.FontFamily,
                (valor) => { texto.FontFamily = valor; ActualizarCanvas(); });

            AgregarPropiedadNumerica("Tamaño:", texto.FontSize,
                (valor) => { texto.FontSize = Math.Max(6, Math.Min(72, valor)); ActualizarCanvas(); });

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

            AgregarCheckBox("Ajuste de línea:", texto.WordWrap,
                (valor) => { texto.WordWrap = valor; ActualizarCanvas(); });
        }

        private void AgregarPropiedadesImagen(ElementoImagen imagen)
        {
            try
            {
                AgregarSeccion("🖼️ Propiedades de Imagen");

                var labelRuta = new TextBlock
                {
                    Text = "Ruta de imagen:",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                PropiedadesPanel.Children.Add(labelRuta);

                var pathLabel = new TextBlock
                {
                    Text = System.IO.Path.GetFileName(imagen.RutaImagen),
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                PropiedadesPanel.Children.Add(pathLabel);

                var cambiarImagenButton = new Button
                {
                    Content = "📁 Cambiar imagen",
                    Background = Brushes.LightBlue,
                    BorderBrush = Brushes.Blue,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 11,
                    Height = 28,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                cambiarImagenButton.Click += (s, e) => CambiarImagen(imagen);
                PropiedadesPanel.Children.Add(cambiarImagenButton);

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

                System.Diagnostics.Debug.WriteLine("Propiedades de imagen agregadas correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando propiedades de imagen: {ex.Message}");
            }
        }

        private void AgregarPropiedadesCodigoBarras(ElementoCodigoBarras codigo)
        {
            AgregarSeccion("📊 Propiedades de Código de Barras");

            // Campo origen con selector mejorado
            AgregarSelectorCampo("Campo origen:", codigo.CampoOrigen,
                (campo) => {
                    codigo.CampoOrigen = campo;
                    ActualizarCanvas();
                    System.Diagnostics.Debug.WriteLine($"Campo origen cambiado a: {campo}");
                });

            // Tipo de código
            var labelTipo = new TextBlock
            {
                Text = "Tipo de código:",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 2)
            };
            PropiedadesPanel.Children.Add(labelTipo);

            var tipoCombo = new ComboBox
            {
                FontSize = 12,
                Height = 25,
                Margin = new Thickness(0, 2, 0, 10),
                Background = Brushes.White
            };

            var tiposCodigo = new[] { "Code128", "Code39", "EAN13", "QRCode" };
            foreach (var tipo in tiposCodigo)
            {
                tipoCombo.Items.Add(tipo);
            }
            tipoCombo.SelectedItem = codigo.TipoCodigo;

            tipoCombo.SelectionChanged += (s, e) =>
            {
                var nuevoTipo = tipoCombo.SelectedItem?.ToString() ?? "Code128";
                codigo.TipoCodigo = nuevoTipo;
                ActualizarCanvas();
                ActualizarPanelPropiedades(); // Actualizar para mostrar info específica del tipo
            };

            PropiedadesPanel.Children.Add(tipoCombo);

            // Información sobre el tipo seleccionado
            var infoTipo = new TextBlock
            {
                FontSize = 10,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var infoTexto = codigo.TipoCodigo switch
            {
                "Code128" => "• Alfanumérico completo\n• Alta densidad\n• Recomendado para uso general",
                "Code39" => "• Solo mayúsculas y números\n• Menos denso pero más robusto\n• Ampliamente compatible",
                "EAN13" => "• Solo 13 dígitos numéricos\n• Estándar internacional\n• Para productos comerciales",
                "QRCode" => "• Código 2D de alta capacidad\n• Admite texto, URLs, etc.\n• Legible por móviles",
                _ => "Seleccione un tipo de código"
            };

            infoTipo.Text = infoTexto;
            PropiedadesPanel.Children.Add(infoTipo);

            // Mostrar texto
            AgregarCheckBox("Mostrar texto:", codigo.MostrarTexto,
                (valor) => {
                    codigo.MostrarTexto = valor;
                    ActualizarCanvas();
                    ActualizarPanelPropiedades(); // Recrear para mostrar/ocultar opciones de texto
                });

            // Propiedades de texto (solo si está habilitado)
            if (codigo.MostrarTexto)
            {
                AgregarPropiedadNumerica("Tamaño texto:", codigo.FontSize,
                    (valor) => { codigo.FontSize = Math.Max(6, Math.Min(20, valor)); ActualizarCanvas(); });

                AgregarSelectorColor("Color texto:", codigo.ColorTexto,
                    (color) => { codigo.ColorTexto = color; ActualizarCanvas(); });
            }

            // Color de fondo con opción transparente
            AgregarSelectorColorFondo("Color fondo:", codigo.ColorFondo,
                (color) => { codigo.ColorFondo = color; ActualizarCanvas(); });

            // Vista previa del código
            AgregarVistaPreviewCodigo(codigo);
        }

        private void AgregarPropiedadesCampoDinamico(ElementoCampoDinamico campo)
        {
            AgregarSeccion("🏷️ Propiedades de Campo Dinámico");

            // Campo origen con selector mejorado
            AgregarSelectorCampo("Campo origen:", campo.CampoOrigen,
                (valor) => {
                    campo.CampoOrigen = valor;
                    ActualizarCanvas();
                    ActualizarPanelPropiedades(); // Actualizar para mostrar nueva preview
                });

            // Prefijo y sufijo
            AgregarPropiedadTexto("Prefijo:", campo.Prefijo ?? "",
                (valor) => {
                    campo.Prefijo = valor;
                    ActualizarCanvas();
                });

            AgregarPropiedadTexto("Sufijo:", campo.Sufijo ?? "",
                (valor) => {
                    campo.Sufijo = valor;
                    ActualizarCanvas();
                });

            // Vista previa del valor
            var labelPreview = new TextBlock
            {
                Text = "Vista previa:",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 5)
            };
            PropiedadesPanel.Children.Add(labelPreview);

            var valorActual = _viewModel?.ObtenerValorCampo(campo.CampoOrigen) ?? "";
            var textoFinal = $"{campo.Prefijo}{valorActual}{campo.Sufijo}";

            var previewText = new TextBlock
            {
                Text = textoFinal,
                FontSize = 11,
                Foreground = Brushes.DarkBlue,
                Background = Brushes.LightYellow,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.SemiBold
            };
            PropiedadesPanel.Children.Add(previewText);

            // Propiedades de formato
            AgregarSeccion("🎨 Formato del Texto");

            AgregarPropiedadTexto("Fuente:", campo.FontFamily,
                (valor) => {
                    campo.FontFamily = valor;
                    ActualizarCanvas();
                });

            AgregarPropiedadNumerica("Tamaño:", campo.FontSize,
                (valor) => {
                    campo.FontSize = Math.Max(6, Math.Min(72, valor));
                    ActualizarCanvas();
                });

            AgregarSelectorColor("Color:", campo.Color,
                (color) => {
                    campo.Color = color;
                    ActualizarCanvas();
                });

            AgregarSelectorAlineacion("Alineación:", campo.TextAlignment,
                (alineacion) => {
                    campo.TextAlignment = alineacion;
                    ActualizarCanvas();
                });

            // Estilo del texto
            AgregarCheckBox("Negrita:", campo.IsBold,
                (valor) => {
                    campo.IsBold = valor;
                    ActualizarCanvas();
                });

            AgregarCheckBox("Cursiva:", campo.IsItalic,
                (valor) => {
                    campo.IsItalic = valor;
                    ActualizarCanvas();
                });
        }

        private void AgregarPropiedadesAvanzadas(ElementoTarjeta elemento)
        {
            AgregarSeccion("⚡ Propiedades Avanzadas");

            // Opacidad
            AgregarPropiedadNumerica("Opacidad (0-1):", elemento.Opacidad,
                (valor) => {
                    elemento.Opacidad = Math.Max(0, Math.Min(1, valor));
                    ActualizarCanvas();
                });

            // Rotación
            AgregarPropiedadNumerica("Rotación (grados):", elemento.Rotacion,
                (valor) => {
                    elemento.Rotacion = valor % 360;
                    ActualizarCanvas();
                });

            // Visibilidad
            AgregarCheckBox("Visible:", elemento.Visible,
                (valor) => {
                    elemento.Visible = valor;
                    ActualizarCanvas();
                });

            // Botones de acción rápida
            var accionesStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Botón duplicar
            var duplicarBtn = new Button
            {
                Content = "📋",
                Width = 30,
                Height = 25,
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "Duplicar elemento",
                FontSize = 12
            };
            duplicarBtn.Click += (s, e) => _viewModel?.DuplicarElementoCommand.Execute(null);

            // Botón enviar al frente
            var frenteBtn = new Button
            {
                Content = "⬆️",
                Width = 30,
                Height = 25,
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "Enviar al frente",
                FontSize = 12
            };
            frenteBtn.Click += (s, e) => EnviarAlFrente();

            // Botón enviar atrás
            var atrasBtn = new Button
            {
                Content = "⬇️",
                Width = 30,
                Height = 25,
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "Enviar atrás",
                FontSize = 12
            };
            atrasBtn.Click += (s, e) => EnviarAtras();

            // Botón centrar
            var centrarBtn = new Button
            {
                Content = "🎯",
                Width = 30,
                Height = 25,
                ToolTip = "Centrar elemento",
                FontSize = 12
            };
            centrarBtn.Click += (s, e) => CentrarElemento();

            accionesStack.Children.Add(duplicarBtn);
            accionesStack.Children.Add(frenteBtn);
            accionesStack.Children.Add(atrasBtn);
            accionesStack.Children.Add(centrarBtn);

            PropiedadesPanel.Children.Add(accionesStack);
        }

        #endregion

        #region Métodos Auxiliares Específicos

        private void AgregarSelectorCampo(string etiqueta, string campoActual, Action<string> onChange)
        {
            try
            {
                var label = new TextBlock
                {
                    Text = etiqueta,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                PropiedadesPanel.Children.Add(label);

                var comboBox = new ComboBox
                {
                    FontSize = 12,
                    Height = 25,
                    Margin = new Thickness(0, 2, 0, 5),
                    Background = Brushes.White
                };

                // Campos con descripciones
                var campos = new Dictionary<string, string>
                {
                    { "NombreCompleto", "Nombre y apellidos del abonado" },
                    { "Nombre", "Solo el nombre del abonado" },
                    { "Apellidos", "Solo los apellidos del abonado" },
                    { "NumeroSocio", "Número único de socio" },
                    { "DNI", "Documento de identidad" },
                    { "Telefono", "Teléfono de contacto" },
                    { "Email", "Correo electrónico" },
                    { "Peña", "Peña a la que pertenece" },
                    { "TipoAbono", "Tipo de abonamiento" },
                    { "Estado", "Estado actual del abono" },
                    { "FechaNacimiento", "Fecha de nacimiento" },
                    { "CodigoBarras", "Código único para códigos de barras" },
                    { "Direccion", "Dirección del abonado" },
                    { "TallaCamiseta", "Talla de camiseta" },
                    { "FechaCreacion", "Fecha de alta como abonado" },
                    { "Gestor", "Gestor asignado al abonado" }
                };

                foreach (var campo in campos)
                {
                    var item = new ComboBoxItem
                    {
                        Content = campo.Key,
                        ToolTip = campo.Value
                    };
                    comboBox.Items.Add(item);
                }

                // Seleccionar el campo actual
                foreach (ComboBoxItem item in comboBox.Items)
                {
                    if (item.Content.ToString() == campoActual)
                    {
                        comboBox.SelectedItem = item;
                        break;
                    }
                }

                comboBox.SelectionChanged += (s, e) =>
                {
                    if (comboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        onChange(selectedItem.Content.ToString() ?? campoActual);
                    }
                };

                PropiedadesPanel.Children.Add(comboBox);

                // Mostrar descripción del campo seleccionado
                var descripcion = new TextBlock
                {
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                if (campos.ContainsKey(campoActual))
                {
                    descripcion.Text = campos[campoActual];
                }

                PropiedadesPanel.Children.Add(descripcion);

                // Actualizar descripción cuando cambie la selección
                comboBox.SelectionChanged += (s, e) =>
                {
                    if (comboBox.SelectedItem is ComboBoxItem item &&
                        campos.ContainsKey(item.Content.ToString() ?? ""))
                    {
                        descripcion.Text = campos[item.Content.ToString() ?? ""];
                    }
                };

                System.Diagnostics.Debug.WriteLine($"Selector campo mejorado agregado: {etiqueta}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando selector campo: {ex.Message}");
            }
        }

        private void AgregarSelectorColorFondo(string etiqueta, Color colorActual, Action<Color> onChange)
        {
            try
            {
                var label = new TextBlock
                {
                    Text = etiqueta,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                PropiedadesPanel.Children.Add(label);

                var fondoGrid = new UniformGrid
                {
                    Columns = 6,
                    Rows = 2,
                    Margin = new Thickness(0, 2, 0, 10)
                };

                var coloresFondo = new[] {
                    Colors.Transparent, Colors.White, Colors.LightGray, Colors.LightBlue, Colors.LightYellow, Colors.LightGreen,
                    Colors.Black, Colors.DarkGray, Colors.Blue, Colors.Red, Colors.Orange, Colors.Purple
                };

                foreach (var color in coloresFondo)
                {
                    var colorButton = new Button
                    {
                        Width = 25,
                        Height = 20,
                        Margin = new Thickness(1),
                        BorderBrush = color == colorActual ? Brushes.DarkBlue : Brushes.Gray,
                        BorderThickness = color == colorActual ? new Thickness(3) : new Thickness(1)
                    };

                    // Fondo especial para transparente
                    if (color == Colors.Transparent)
                    {
                        var checkeredBrush = new DrawingBrush
                        {
                            TileMode = TileMode.Tile,
                            Viewport = new Rect(0, 0, 8, 8),
                            ViewportUnits = BrushMappingMode.Absolute,
                            Drawing = new GeometryDrawing
                            {
                                Brush = Brushes.LightGray,
                                Geometry = Geometry.Parse("M 0,0 L 4,0 L 4,4 L 8,4 L 8,8 L 4,8 L 4,4 L 0,4 Z")
                            }
                        };
                        colorButton.Background = checkeredBrush;
                        colorButton.ToolTip = "Transparente";
                    }
                    else
                    {
                        colorButton.Background = new SolidColorBrush(color);
                        colorButton.ToolTip = color.ToString();
                    }

                    colorButton.Click += (s, e) =>
                    {
                        // Actualizar todos los botones
                        foreach (Button btn in fondoGrid.Children)
                        {
                            btn.BorderBrush = Brushes.Gray;
                            btn.BorderThickness = new Thickness(1);
                        }

                        // Marcar el seleccionado
                        colorButton.BorderBrush = Brushes.DarkBlue;
                        colorButton.BorderThickness = new Thickness(3);

                        onChange(color);
                    };

                    fondoGrid.Children.Add(colorButton);
                }

                PropiedadesPanel.Children.Add(fondoGrid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error agregando selector color fondo: {ex.Message}");
            }
        }

        private void AgregarVistaPreviewCodigo(ElementoCodigoBarras codigo)
        {
            try
            {
                var labelPreview = new TextBlock
                {
                    Text = "Vista previa:",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                PropiedadesPanel.Children.Add(labelPreview);

                var previewBorder = new Border
                {
                    Width = 150,
                    Height = 60,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                // Generar preview del código
                var valorEjemplo = _viewModel?.ObtenerValorCampo(codigo.CampoOrigen) ?? "123456789";
                BitmapSource? previewImage = null;

                try
                {
                    switch (codigo.TipoCodigo.ToUpper())
                    {
                        case "CODE128":
                            previewImage = BarcodeGenerator.GenerateBarcode(valorEjemplo, 140, 40, "Code128");
                            break;
                        case "CODE39":
                            previewImage = BarcodeGenerator.GenerateBarcode(valorEjemplo, 140, 40, "Code39");
                            break;
                        case "EAN13":
                            var ean13 = valorEjemplo.Length >= 12 ? valorEjemplo.Substring(0, 12) : valorEjemplo.PadLeft(12, '0');
                            previewImage = BarcodeGenerator.GenerateBarcode(ean13 + "0", 140, 40, "EAN13");
                            break;
                        case "QRCODE":
                            previewImage = BarcodeGenerator.GenerateQRCode(valorEjemplo, 50);
                            break;
                    }

                    if (previewImage != null)
                    {
                        var imagePreview = new Image
                        {
                            Source = previewImage,
                            Stretch = Stretch.Uniform,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        previewBorder.Child = imagePreview;
                    }
                    else
                    {
                        previewBorder.Child = new TextBlock
                        {
                            Text = "Error en preview",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 10,
                            Foreground = Brushes.Red
                        };
                    }
                }
                catch
                {
                    previewBorder.Child = new TextBlock
                    {
                        Text = $"Preview\n{codigo.TipoCodigo}",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 10
                    };
                }

                PropiedadesPanel.Children.Add(previewBorder);

                // Información del valor actual
                var valorInfo = new TextBlock
                {
                    Text = $"Valor: {valorEjemplo}",
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                PropiedadesPanel.Children.Add(valorInfo);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando preview: {ex.Message}");
            }
        }

        private void CambiarImagen(ElementoImagen imagen)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Title = "Seleccionar imagen",
                    Multiselect = false
                };

                if (openDialog.ShowDialog() == true)
                {
                    // Validar tamaño de archivo
                    var fileInfo = new FileInfo(openDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024) // 5MB máximo
                    {
                        MessageBox.Show("El archivo es demasiado grande. Máximo 5MB permitido.",
                                      "Archivo demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    imagen.RutaImagen = openDialog.FileName;
                    ActualizarCanvas();
                    ActualizarPanelPropiedades(); // Actualizar para mostrar nuevo nombre
                    System.Diagnostics.Debug.WriteLine($"Imagen cambiada a: {openDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cambiar imagen: {ex.Message}");
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Limpiar recursos antes de cerrar
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

                    // Llamar a dispose si está disponible
                    if (_viewModel is IDisposable disposableViewModel)
                    {
                        disposableViewModel.Dispose();
                    }
                }

                // Limpiar handles de redimensionamiento
                LimpiarHandlesRedimensionamiento();

                // Limpiar eventos de zoom
                if (CanvasScrollViewer != null)
                {
                    CanvasScrollViewer.MouseWheel -= Canvas_MouseWheel;
                    CanvasScrollViewer.PreviewMouseRightButtonDown -= Canvas_PreviewMouseRightButtonDown;
                    CanvasScrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                }

                // Limpiar eventos del canvas
                if (CanvasTarjeta != null)
                {
                    CanvasTarjeta.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
                    CanvasTarjeta.MouseMove -= Canvas_MouseMove;
                    CanvasTarjeta.MouseLeftButtonUp -= Canvas_MouseLeftButtonUp;
                    CanvasTarjeta.MouseLeave -= Canvas_MouseLeave;
                    CanvasTarjeta.MouseRightButtonDown -= Canvas_MouseRightButtonDown;
                }

                System.Diagnostics.Debug.WriteLine("Recursos limpiados correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error durante el cleanup: {ex.Message}");
            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CardDesignerWindow cerrada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error final al cerrar: {ex.Message}");
            }

            base.OnClosed(e);
        }

        #endregion

        #region Métodos de Optimización

        private void OptimizarRendimiento()
        {
            try
            {
                // Limpiar elementos fuera de los límites del canvas
                if (_viewModel != null)
                {
                    _viewModel.OptimizarRendimiento();
                }

                // Limpiar handles innecesarios
                if (_viewModel?.ElementoSeleccionado == null)
                {
                    LimpiarHandlesRedimensionamiento();
                }

                // Forzar garbage collection si es necesario
                if (GC.GetTotalMemory(false) > 50 * 1024 * 1024) // 50MB
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Diagnostics.Debug.WriteLine("Garbage collection ejecutado");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error optimizando rendimiento: {ex.Message}");
            }
        }

        // Método para diagnosticar problemas
        private void DiagnosticarEstado()
        {
            try
            {
                var diagnostico = new System.Text.StringBuilder();
                diagnostico.AppendLine("=== DIAGNÓSTICO CardDesignerWindow ===");
                diagnostico.AppendLine($"ViewModel: {(_viewModel != null ? "OK" : "NULL")}");
                diagnostico.AppendLine($"Canvas: {(CanvasTarjeta != null ? "OK" : "NULL")}");
                diagnostico.AppendLine($"PropiedadesPanel: {(PropiedadesPanel != null ? "OK" : "NULL")}");
                diagnostico.AppendLine($"Elementos en canvas: {CanvasTarjeta?.Children.Count ?? 0}");
                diagnostico.AppendLine($"Elementos en ViewModel: {_viewModel?.ElementosActuales.Count ?? 0}");
                diagnostico.AppendLine($"Elemento seleccionado: {_viewModel?.ElementoSeleccionado?.Tipo ?? "Ninguno"}");
                diagnostico.AppendLine($"Handles activos: {_resizeHandles.Count}");
                diagnostico.AppendLine($"Zoom actual: {_zoomFactor:P0}");
                diagnostico.AppendLine($"Arrastrando: {_isDragging}");
                diagnostico.AppendLine($"Redimensionando: {_isResizing}");

                System.Diagnostics.Debug.WriteLine(diagnostico.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en diagnóstico: {ex.Message}");
            }
        }

        #endregion
    }
}