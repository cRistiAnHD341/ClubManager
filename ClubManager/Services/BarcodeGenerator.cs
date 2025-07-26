using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClubManager.Services
{
    public static class BarcodeGenerator
    {
        public static BitmapSource? GenerateBarcode(string data, double width, double height, string format = "Code128")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data))
                    return null;

                // Crear una imagen bitmap con fondo transparente para evitar marcos
                var pixelWidth = (int)Math.Max(width, 200);
                var pixelHeight = (int)Math.Max(height, 60);

                var bitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null);

                var stride = pixelWidth * 4; // 4 bytes por pixel (BGRA)
                var pixelData = new byte[stride * pixelHeight];

                // Llenar con transparente (todos los bytes en 0)
                Array.Clear(pixelData, 0, pixelData.Length);

                // Generar patrón según el formato especificado
                string barcodePattern = format.ToUpper() switch
                {
                    "CODE128" => GenerateCode128Pattern(data),
                    "CODE39" => GenerateCode39Pattern(data),
                    "EAN13" => GenerateEAN13Pattern(data),
                    _ => GenerateCode128Pattern(data)
                };

                if (string.IsNullOrEmpty(barcodePattern))
                    return null;

                // Calcular dimensiones óptimas
                var barWidth = Math.Max(1, pixelWidth / barcodePattern.Length);
                var totalBarcodeWidth = barcodePattern.Length * barWidth;
                var startX = Math.Max(0, (pixelWidth - totalBarcodeWidth) / 2);

                // Usar toda la altura disponible (sin márgenes internos)
                var barHeight = pixelHeight;
                var startY = 0;

                // Dibujar las barras negras
                for (int patternIndex = 0; patternIndex < barcodePattern.Length; patternIndex++)
                {
                    if (barcodePattern[patternIndex] == '1') // Barra negra
                    {
                        var barStartX = startX + (patternIndex * barWidth);
                        var barEndX = Math.Min(barStartX + barWidth, pixelWidth);

                        for (int x = barStartX; x < barEndX; x++)
                        {
                            for (int y = startY; y < startY + barHeight; y++)
                            {
                                if (x >= 0 && x < pixelWidth && y >= 0 && y < pixelHeight)
                                {
                                    var index = (y * stride) + (x * 4);
                                    if (index + 3 < pixelData.Length)
                                    {
                                        pixelData[index] = 0;     // B
                                        pixelData[index + 1] = 0; // G
                                        pixelData[index + 2] = 0; // R
                                        pixelData[index + 3] = 255; // A (opaco)
                                    }
                                }
                            }
                        }
                    }
                }

                // Escribir los datos al bitmap
                var rect = new Int32Rect(0, 0, pixelWidth, pixelHeight);
                bitmap.WritePixels(rect, pixelData, stride, 0);

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando código de barras: {ex.Message}");
                return null;
            }
        }

        private static readonly Dictionary<char, string> Code128TableB = new Dictionary<char, string>
        {
            {' ', "11011001100"}, {'!', "11001101100"}, {'"', "11001100110"}, {'#', "10010011000"},
            {'$', "10010001100"}, {'%', "10001001100"}, {'&', "10011001000"}, {'\'', "10011000100"},
            {'(', "10001100100"}, {')', "11001001000"}, {'*', "11001000100"}, {'+', "11000100100"},
            {',', "10110011100"}, {'-', "10011011100"}, {'.', "10011001110"}, {'/', "10111001100"},
            {'0', "10011101100"}, {'1', "10011100110"}, {'2', "11001110010"}, {'3', "11001011100"},
            {'4', "11001001110"}, {'5', "11011100100"}, {'6', "11001110100"}, {'7', "11101101110"},
            {'8', "11101001100"}, {'9', "11100101100"}, {':', "11100100110"}, {';', "11101100100"},
            {'<', "11100110100"}, {'=', "11100110010"}, {'>', "11011011000"}, {'?', "11011000110"},
            {'@', "11000110110"}, {'A', "10100011000"}, {'B', "10001011000"}, {'C', "10001000110"},
            {'D', "10110001000"}, {'E', "10001101000"}, {'F', "10001100010"}, {'G', "11010001000"},
            {'H', "11000101000"}, {'I', "11000100010"}, {'J', "10110111000"}, {'K', "10110001110"},
            {'L', "10001101110"}, {'M', "10111011000"}, {'N', "10111000110"}, {'O', "10001110110"},
            {'P', "11101110110"}, {'Q', "11010001110"}, {'R', "11000101110"}, {'S', "11011101000"},
            {'T', "11011100010"}, {'U', "11011101110"}, {'V', "11101011000"}, {'W', "11101000110"},
            {'X', "11100010110"}, {'Y', "11101101000"}, {'Z', "11101100010"}, {'[', "11100011010"},
            {'\\', "11101111010"}, {']', "11001000010"}, {'^', "11110001010"}, {'_', "10100110000"},
            {'`', "10100001100"}, {'a', "10010110000"}, {'b', "10010000110"}, {'c', "10000101100"},
            {'d', "10000100110"}, {'e', "10110010000"}, {'f', "10110000100"}, {'g', "10011010000"},
            {'h', "10011000010"}, {'i', "10000110100"}, {'j', "10000110010"}, {'k', "11000010010"},
            {'l', "11001010000"}, {'m', "11110111010"}, {'n', "11000010100"}, {'o', "10001111010"},
            {'p', "10100111100"}, {'q', "10010111100"}, {'r', "10010011110"}, {'s', "10111100100"},
            {'t', "10011110100"}, {'u', "10011110010"}, {'v', "11110100100"}, {'w', "11110010100"},
            {'x', "11110010010"}, {'y', "11011011110"}, {'z', "11011110110"}, {'{', "11110110110"},
            {'|', "10101111000"}, {'}', "10100011110"}, {'~', "10001011110"}
        };

        private static string GenerateCode128Pattern(string data)
        {
            try
            {
                var pattern = "";

                // Start B (para caracteres ASCII)
                pattern += "11010010000";

                var checksum = 104; // Start B value

                for (int i = 0; i < data.Length; i++)
                {
                    char c = data[i];

                    if (Code128TableB.ContainsKey(c))
                    {
                        pattern += Code128TableB[c];
                        // Calcular checksum
                        var value = c == ' ' ? 0 : (c >= '!' && c <= '~') ? (c - 32) : 0;
                        checksum += value * (i + 1);
                    }
                    else
                    {
                        // Si el caracter no está en la tabla, usar '?'
                        pattern += Code128TableB['?'];
                        checksum += 31 * (i + 1); // Value for '?'
                    }
                }

                // Agregar checksum
                checksum = checksum % 103;
                var checksumChar = GetCode128Character(checksum);
                if (Code128TableB.ContainsKey(checksumChar))
                {
                    pattern += Code128TableB[checksumChar];
                }

                // Stop pattern
                pattern += "1100011101011";

                return pattern;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Code128: {ex.Message}");
                return "";
            }
        }

        private static char GetCode128Character(int value)
        {
            if (value == 0) return ' ';
            if (value >= 1 && value <= 94) return (char)(value + 31);
            return '?';
        }

        private static readonly Dictionary<char, string> Code39Table = new Dictionary<char, string>
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

        private static string GenerateCode39Pattern(string data)
        {
            try
            {
                var pattern = "";
                var upperData = data.ToUpper();

                // Start/Stop character (*)
                pattern += Code39Table['*'];
                pattern += "0"; // Espacio entre caracteres

                foreach (char c in upperData)
                {
                    if (Code39Table.ContainsKey(c))
                    {
                        pattern += Code39Table[c];
                        pattern += "0"; // Espacio entre caracteres
                    }
                    else if (char.IsLetterOrDigit(c) || c == '-' || c == '.' || c == ' ')
                    {
                        // Si no está en la tabla pero es válido, usar un placeholder
                        pattern += Code39Table['*'];
                        pattern += "0";
                    }
                }

                // Stop character (*)
                pattern += Code39Table['*'];

                return pattern;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Code39: {ex.Message}");
                return "";
            }
        }

        private static string GenerateEAN13Pattern(string data)
        {
            try
            {
                // Asegurar que tenemos exactamente 13 dígitos
                var ean13Data = data.PadLeft(13, '0');
                if (ean13Data.Length > 13)
                    ean13Data = ean13Data.Substring(ean13Data.Length - 13);

                // Verificar que todos son dígitos
                if (!ean13Data.All(char.IsDigit))
                    return "";

                var pattern = "101"; // Start guard

                var firstDigit = int.Parse(ean13Data[0].ToString());
                var leftPatternType = GetEAN13LeftPatternType(firstDigit);

                // Primeros 6 dígitos (usando patrón L o G según el primer dígito)
                for (int i = 1; i <= 6; i++)
                {
                    var digit = int.Parse(ean13Data[i].ToString());
                    if (leftPatternType[i - 1] == 'L')
                        pattern += GetEAN13LeftPattern(digit);
                    else
                        pattern += GetEAN13GPattern(digit);
                }

                pattern += "01010"; // Center guard

                // Últimos 6 dígitos (usando patrón R)
                for (int i = 7; i <= 12; i++)
                {
                    var digit = int.Parse(ean13Data[i].ToString());
                    pattern += GetEAN13RightPattern(digit);
                }

                pattern += "101"; // End guard
                return pattern;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EAN13: {ex.Message}");
                return "";
            }
        }

        private static string GetEAN13LeftPatternType(int firstDigit)
        {
            var patterns = new string[]
            {
                "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
                "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"
            };
            return firstDigit < patterns.Length ? patterns[firstDigit] : patterns[0];
        }

        private static string GetEAN13LeftPattern(int digit)
        {
            var leftPatterns = new string[]
            {
                "0001101", "0011001", "0010011", "0111101", "0100011",
                "0110001", "0101111", "0111011", "0110111", "0001011"
            };
            return digit < leftPatterns.Length ? leftPatterns[digit] : leftPatterns[0];
        }

        private static string GetEAN13GPattern(int digit)
        {
            var gPatterns = new string[]
            {
                "0100111", "0110011", "0011011", "0100001", "0011101",
                "0111001", "0000101", "0010001", "0001001", "0010111"
            };
            return digit < gPatterns.Length ? gPatterns[digit] : gPatterns[0];
        }

        private static string GetEAN13RightPattern(int digit)
        {
            var rightPatterns = new string[]
            {
                "1110010", "1100110", "1101100", "1000010", "1011100",
                "1001110", "1010000", "1000100", "1001000", "1110100"
            };
            return digit < rightPatterns.Length ? rightPatterns[digit] : rightPatterns[0];
        }

        public static BitmapSource? GenerateQRCode(string data, double size)
        {
            try
            {
                // QR Code mejorado con fondo transparente
                var pixelSize = (int)Math.Max(size, 100);
                var moduleSize = Math.Max(2, pixelSize / 25);

                var bitmap = new WriteableBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Bgra32, null);
                var stride = pixelSize * 4;
                var pixelData = new byte[stride * pixelSize];

                // Fondo transparente
                Array.Clear(pixelData, 0, pixelData.Length);

                // Generar patrón QR mejorado
                var hash = data.GetHashCode();
                var random = new Random(Math.Abs(hash));

                // Dibujar módulos QR
                for (int moduleY = 0; moduleY < 25; moduleY++)
                {
                    for (int moduleX = 0; moduleX < 25; moduleX++)
                    {
                        // Patrones de localización más precisos
                        bool isFinderPattern = IsInFinderPattern(moduleX, moduleY);
                        bool isTimingPattern = IsTimingPattern(moduleX, moduleY);

                        bool isBlack;
                        if (isFinderPattern)
                        {
                            isBlack = IsFinderPatternBlack(moduleX % 7, moduleY % 7);
                        }
                        else if (isTimingPattern)
                        {
                            isBlack = (moduleX + moduleY) % 2 == 0;
                        }
                        else
                        {
                            // Área de datos con patrón pseudo-aleatorio mejorado
                            var seed = (moduleX * 31 + moduleY * 17 + Math.Abs(hash)) % 1000;
                            isBlack = new Random(seed).Next(0, 5) == 1; // 20% de densidad
                        }

                        if (isBlack)
                        {
                            var startX = moduleX * moduleSize;
                            var startY = moduleY * moduleSize;
                            var endX = Math.Min(startX + moduleSize, pixelSize);
                            var endY = Math.Min(startY + moduleSize, pixelSize);

                            for (int y = startY; y < endY; y++)
                            {
                                for (int x = startX; x < endX; x++)
                                {
                                    if (x >= 0 && x < pixelSize && y >= 0 && y < pixelSize)
                                    {
                                        var index = (y * stride) + (x * 4);
                                        if (index + 3 < pixelData.Length)
                                        {
                                            pixelData[index] = 0;     // B
                                            pixelData[index + 1] = 0; // G
                                            pixelData[index + 2] = 0; // R
                                            pixelData[index + 3] = 255; // A
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var rect = new Int32Rect(0, 0, pixelSize, pixelSize);
                bitmap.WritePixels(rect, pixelData, stride, 0);

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando QR: {ex.Message}");
                return null;
            }
        }

        private static bool IsInFinderPattern(int x, int y)
        {
            return (x < 7 && y < 7) ||           // Top-left
                   (x >= 18 && y < 7) ||         // Top-right  
                   (x < 7 && y >= 18);           // Bottom-left
        }

        private static bool IsTimingPattern(int x, int y)
        {
            return (x == 6 && y >= 8 && y <= 16) || // Vertical timing
                   (y == 6 && x >= 8 && x <= 16);   // Horizontal timing
        }

        private static bool IsFinderPatternBlack(int x, int y)
        {
            // Patrón típico de localización QR (7x7)
            var pattern = new bool[,]
            {
                {true, true, true, true, true, true, true},
                {true, false, false, false, false, false, true},
                {true, false, true, true, true, false, true},
                {true, false, true, true, true, false, true},
                {true, false, true, true, true, false, true},
                {true, false, false, false, false, false, true},
                {true, true, true, true, true, true, true}
            };

            return x < 7 && y < 7 ? pattern[y, x] : false;
        }
    }
}