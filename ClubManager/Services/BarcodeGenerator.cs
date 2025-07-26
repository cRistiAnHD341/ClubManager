using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClubManager.Services
{
    /// <summary>
    /// Generador de códigos de barras funcionales sin dependencias externas
    /// Implementa Code128 y Code39 con algoritmos reales
    /// </summary>
    public static class BarcodeGenerator
    {
        /// <summary>
        /// Genera una imagen de código de barras
        /// </summary>
        /// <param name="text">Texto a codificar</param>
        /// <param name="width">Ancho de la imagen</param>
        /// <param name="height">Alto de la imagen</param>
        /// <param name="type">Tipo de código (Code128, Code39)</param>
        /// <returns>Imagen bitmap del código de barras</returns>
        public static BitmapSource? GenerateBarcode(string text, double width, double height, string type = "Code128")
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return null;

                string pattern = type.ToUpper() switch
                {
                    "CODE128" => GenerateCode128Pattern(text),
                    "CODE39" => GenerateCode39Pattern(text),
                    _ => GenerateCode128Pattern(text) // Default to Code128
                };

                if (string.IsNullOrEmpty(pattern))
                    return null;

                return CreateBarcodeImage(pattern, width, height);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando código de barras: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Genera el patrón binario para Code128
        /// </summary>
        private static string GenerateCode128Pattern(string text)
        {
            try
            {
                var pattern = "";
                var checksum = 104; // Start Code B

                // Start pattern for Code B
                pattern += Code128Patterns[104];

                // Encode each character
                for (int i = 0; i < text.Length; i++)
                {
                    var charCode = GetCode128Value(text[i]);
                    if (charCode == -1) continue; // Skip invalid characters

                    pattern += Code128Patterns[charCode];
                    checksum += charCode * (i + 1);
                }

                // Add checksum
                var checksumValue = checksum % 103;
                pattern += Code128Patterns[checksumValue];

                // Stop pattern
                pattern += "1100011101011"; // Stop pattern

                return pattern;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Genera el patrón binario para Code39
        /// </summary>
        private static string GenerateCode39Pattern(string text)
        {
            try
            {
                var pattern = "";

                // Start character (*)
                pattern += "100010111011101";

                // Encode each character
                foreach (char c in text.ToUpper())
                {
                    if (Code39Patterns.ContainsKey(c))
                    {
                        pattern += Code39Patterns[c];
                        pattern += "0"; // Inter-character gap
                    }
                }

                // Stop character (*)
                pattern += "100010111011101";

                return pattern;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Crea la imagen bitmap del código de barras
        /// </summary>
        private static BitmapSource CreateBarcodeImage(string pattern, double width, double height)
        {
            try
            {
                var drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    // Fondo blanco
                    context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                    // Calcular ancho de cada barra
                    var barWidth = width / pattern.Length;
                    var barHeight = height * 0.8; // 80% de la altura para las barras
                    var startY = height * 0.1; // 10% de margen superior

                    // Dibujar las barras
                    for (int i = 0; i < pattern.Length; i++)
                    {
                        if (pattern[i] == '1')
                        {
                            var x = i * barWidth;
                            context.DrawRectangle(Brushes.Black, null,
                                new Rect(x, startY, barWidth, barHeight));
                        }
                    }
                }

                var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene el valor Code128 para un carácter
        /// </summary>
        private static int GetCode128Value(char c)
        {
            if (c >= ' ' && c <= '_')
                return c - ' ';
            return -1; // Invalid character
        }

        /// <summary>
        /// Patrones Code128 (103 valores + start/stop)
        /// Cada patrón representa las barras y espacios en binario
        /// </summary>
        private static readonly Dictionary<int, string> Code128Patterns = new()
        {
            {0, "11011001100"},   {1, "11001101100"},   {2, "11001100110"},   {3, "10010011000"},
            {4, "10010001100"},   {5, "10001001100"},   {6, "10011001000"},   {7, "10011000100"},
            {8, "10001100100"},   {9, "11001001000"},   {10, "11001000100"},  {11, "11000100100"},
            {12, "10110011100"},  {13, "10011011100"},  {14, "10011001110"},  {15, "10111001100"},
            {16, "10011101100"},  {17, "10011100110"},  {18, "11001110010"},  {19, "11001011100"},
            {20, "11001001110"},  {21, "11011100100"},  {22, "11001110100"},  {23, "11101101110"},
            {24, "11101001100"},  {25, "11100101100"},  {26, "11100100110"},  {27, "11101100100"},
            {28, "11100110100"},  {29, "11100110010"},  {30, "11011011000"},  {31, "11011000110"},
            {32, "11000110110"},  {33, "10100011000"},  {34, "10001011000"},  {35, "10001000110"},
            {36, "10110001000"},  {37, "10001101000"},  {38, "10001100010"},  {39, "11010001000"},
            {40, "11000101000"},  {41, "11000100010"},  {42, "10110111000"},  {43, "10110001110"},
            {44, "10001101110"},  {45, "10111011000"},  {46, "10111000110"},  {47, "10001110110"},
            {48, "11101110110"},  {49, "11010001110"},  {50, "11000101110"},  {51, "11011101000"},
            {52, "11011100010"},  {53, "11011101110"},  {54, "11101011000"},  {55, "11101000110"},
            {56, "11100010110"},  {57, "11101101000"},  {58, "11101100010"},  {59, "11100011010"},
            {60, "11101111010"},  {61, "11001000010"},  {62, "11110001010"},  {63, "10100110000"},
            {64, "10100001100"},  {65, "10010110000"},  {66, "10010000110"},  {67, "10000101100"},
            {68, "10000100110"},  {69, "10110010000"},  {70, "10110000100"},  {71, "10011010000"},
            {72, "10011000010"},  {73, "10000110100"},  {74, "10000110010"},  {75, "11000010010"},
            {76, "11001010000"},  {77, "11110111010"},  {78, "11000010100"},  {79, "10001111010"},
            {80, "10100111100"},  {81, "10010111100"},  {82, "10010011110"},  {83, "10111100100"},
            {84, "10011110100"},  {85, "10011110010"},  {86, "11110100100"},  {87, "11110010100"},
            {88, "11110010010"},  {89, "11011011110"},  {90, "11011110110"},  {91, "11110110110"},
            {92, "10101111000"},  {93, "10100011110"},  {94, "10001011110"},  {95, "10111101000"},
            {96, "10111100010"},  {97, "11110101000"},  {98, "11110100010"},  {99, "10111011110"},
            {100, "10111101110"}, {101, "11101011110"}, {102, "11110101110"}, {103, "11010000100"},
            {104, "11010010000"}, {105, "11010011100"}, {106, "1100011101011"} // Stop
        };

        /// <summary>
        /// Patrones Code39
        /// Cada carácter se representa con 9 elementos (5 barras, 4 espacios)
        /// 3 elementos son anchos, 6 son estrechos
        /// </summary>
        private static readonly Dictionary<char, string> Code39Patterns = new()
        {
            {'0', "101001101101"}, {'1', "110100101011"}, {'2', "101100101011"}, {'3', "110110010101"},
            {'4', "101001101011"}, {'5', "110100110101"}, {'6', "101100110101"}, {'7', "101001011011"},
            {'8', "110100101101"}, {'9', "101100101101"}, {'A', "110101001011"}, {'B', "101101001011"},
            {'C', "110110100101"}, {'D', "101011001011"}, {'E', "110101100101"}, {'F', "101101100101"},
            {'G', "101010011011"}, {'H', "110101001101"}, {'I', "101101001101"}, {'J', "101011001101"},
            {'K', "110101010011"}, {'L', "101101010011"}, {'M', "110110101001"}, {'N', "101011010011"},
            {'O', "110101101001"}, {'P', "101101101001"}, {'Q', "101010110011"}, {'R', "110101011001"},
            {'S', "101101011001"}, {'T', "101011011001"}, {'U', "110010101011"}, {'V', "100110101011"},
            {'W', "110011010101"}, {'X', "100101101011"}, {'Y', "110010110101"}, {'Z', "100110110101"},
            {'-', "100101011011"}, {'.', "110010101101"}, {' ', "100110101101"}, {'$', "100100100101"},
            {'/', "100100101001"}, {'+', "100101001001"}, {'%', "101001001001"}, {'*', "100101101101"}
        };

        /// <summary>
        /// Valida si un texto es válido para Code128
        /// </summary>
        public static bool IsValidCode128(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.All(c => c >= ' ' && c <= '_');
        }

        /// <summary>
        /// Valida si un texto es válido para Code39
        /// </summary>
        public static bool IsValidCode39(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.ToUpper().All(c => Code39Patterns.ContainsKey(c));
        }

        /// <summary>
        /// Genera un código de barras simple basado en patrones hash (fallback)
        /// </summary>
        public static BitmapSource GenerateSimpleBarcode(string text, double width, double height)
        {
            try
            {
                var drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    // Fondo blanco
                    context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                    // Generar patrón simple basado en hash del texto
                    var hash = text.GetHashCode();
                    var pattern = Convert.ToString(Math.Abs(hash), 2).PadLeft(32, '0');

                    // Repetir patrón para llenar el ancho disponible
                    var fullPattern = "";
                    var targetLength = (int)(width / 2); // 2 píxeles por bit
                    while (fullPattern.Length < targetLength)
                    {
                        fullPattern += pattern;
                    }
                    fullPattern = fullPattern.Substring(0, targetLength);

                    // Dibujar barras
                    var barWidth = width / fullPattern.Length;
                    var barHeight = height * 0.7;
                    var startY = height * 0.15;

                    for (int i = 0; i < fullPattern.Length; i++)
                    {
                        if (fullPattern[i] == '1')
                        {
                            var x = i * barWidth;
                            context.DrawRectangle(Brushes.Black, null,
                                new Rect(x, startY, barWidth, barHeight));
                        }
                    }
                }

                var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Genera un código QR simple (representación visual básica)
        /// </summary>
        public static BitmapSource GenerateSimpleQR(string text, double size)
        {
            try
            {
                var drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    // Fondo blanco
                    context.DrawRectangle(Brushes.White, null, new Rect(0, 0, size, size));

                    // Generar patrón QR simple basado en hash
                    var hash = text.GetHashCode();
                    var gridSize = 21; // QR estándar 21x21
                    var cellSize = size / gridSize;

                    var random = new Random(Math.Abs(hash));

                    // Dibujar patrón de localización (esquinas)
                    DrawFinderPattern(context, 0, 0, cellSize);
                    DrawFinderPattern(context, (gridSize - 7) * cellSize, 0, cellSize);
                    DrawFinderPattern(context, 0, (gridSize - 7) * cellSize, cellSize);

                    // Llenar con patrón aleatorio basado en hash
                    for (int x = 0; x < gridSize; x++)
                    {
                        for (int y = 0; y < gridSize; y++)
                        {
                            // Evitar zonas de patrones de localización
                            if (IsInFinderPattern(x, y, gridSize))
                                continue;

                            if (random.NextDouble() > 0.5)
                            {
                                context.DrawRectangle(Brushes.Black, null,
                                    new Rect(x * cellSize, y * cellSize, cellSize, cellSize));
                            }
                        }
                    }
                }

                var bitmap = new RenderTargetBitmap((int)size, (int)size, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static void DrawFinderPattern(DrawingContext context, double x, double y, double cellSize)
        {
            // Patrón de localización QR (7x7)
            var pattern = new bool[7, 7]
            {
                {true, true, true, true, true, true, true},
                {true, false, false, false, false, false, true},
                {true, false, true, true, true, false, true},
                {true, false, true, true, true, false, true},
                {true, false, true, true, true, false, true},
                {true, false, false, false, false, false, true},
                {true, true, true, true, true, true, true}
            };

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (pattern[i, j])
                    {
                        context.DrawRectangle(Brushes.Black, null,
                            new Rect(x + j * cellSize, y + i * cellSize, cellSize, cellSize));
                    }
                }
            }
        }

        private static bool IsInFinderPattern(int x, int y, int gridSize)
        {
            // Verificar si está en zona de patrón de localización
            return (x < 9 && y < 9) ||  // Top-left
                   (x >= gridSize - 8 && y < 9) ||  // Top-right
                   (x < 9 && y >= gridSize - 8);  // Bottom-left
        }
    }
}