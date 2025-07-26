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

        public CardDesignerWindow()
        {
            InitializeComponent();

            // Inicializar ViewModel antes del Loaded
            _viewModel = new CardDesignerViewModel();
            DataContext = _viewModel;

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

                // Actualizar el canvas inicialmente para mostrar cualquier elemento existente
                System.Diagnostics.Debug.WriteLine("Actualizando canvas inicial...");
                _viewModel.ActualizarCanvas();

                // Configurar eventos del canvas
                System.Diagnostics.Debug.WriteLine("Configurando eventos del canvas...");
                CanvasTarjeta.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
                CanvasTarjeta.MouseMove += Canvas_MouseMove;
                CanvasTarjeta.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
                CanvasTarjeta.MouseLeave += Canvas_MouseLeave;

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

                _lastPosition = e.GetPosition(canvas);
                var hitElement = canvas.InputHitTest(_lastPosition) as UIElement;

                if (hitElement != null && hitElement != canvas)
                {
                    // Encontrar el elemento de tarjeta correspondiente
                    var elementoTarjeta = EncontrarElementoPorUI(hitElement);
                    if (elementoTarjeta != null && _viewModel != null)
                    {
                        _viewModel.ElementoSeleccionado = elementoTarjeta;
                        _draggedElement = hitElement;
                        _isDragging = true;
                        canvas.CaptureMouse();
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

                // Actualizar propiedades
                ActualizarPanelPropiedades();
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

                if (_isDragging)
                {
                    _isDragging = false;
                    _draggedElement = null;
                    canvas.ReleaseMouseCapture();
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
                    _draggedElement = null;

                    var canvas = sender as Canvas;
                    canvas?.ReleaseMouseCapture();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MouseLeave: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Utilidad

        private ElementoTarjeta? EncontrarElementoPorUI(UIElement uiElement)
        {
            try
            {
                if (_viewModel?.ElementosActuales == null) return null;

                // Buscar por posición en el canvas
                var left = Canvas.GetLeft(uiElement);
                var top = Canvas.GetTop(uiElement);

                foreach (var elemento in _viewModel.ElementosActuales)
                {
                    if (Math.Abs(elemento.X - left) < 1 && Math.Abs(elemento.Y - top) < 1)
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