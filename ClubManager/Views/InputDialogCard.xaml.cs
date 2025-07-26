using System.Windows;

namespace ClubManager.Views
{
    public class InputDialogCard : Window
    {
        private System.Windows.Controls.TextBox _textBox;

        public string Respuesta { get; private set; } = "";

        public InputDialogCard(string titulo, string pregunta, string valorPorDefecto = "")
        {
            Title = titulo;
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //ResizeMode = ResizeMode.NoResize;

            // Crear el contenido
            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Etiqueta de pregunta
            var label = new System.Windows.Controls.Label
            {
                Content = pregunta,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(label);

            // TextBox para la respuesta
            _textBox = new System.Windows.Controls.TextBox
            {
                Text = valorPorDefecto,
                FontSize = 12,
                Padding = new Thickness(5),
                Height = 30
            };
            _textBox.SelectAll();
            _textBox.KeyDown += TextBox_KeyDown;
            stackPanel.Children.Add(_textBox);

            System.Windows.Controls.Grid.SetRow(stackPanel, 0);
            grid.Children.Add(stackPanel);

            // Panel de botones
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20)
            };

            var btnOk = new System.Windows.Controls.Button
            {
                Content = "Aceptar",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            btnOk.Click += BtnOk_Click;
            buttonPanel.Children.Add(btnOk);

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Width = 80,
                Height = 30,
                IsCancel = true
            };
            btnCancel.Click += BtnCancel_Click;
            buttonPanel.Children.Add(btnCancel);

            System.Windows.Controls.Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;

            Loaded += (s, e) => _textBox.Focus();
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                BtnOk_Click(sender, e);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Respuesta = _textBox.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}