using RetroLib.General;
using System.Data;
using System.Drawing;


namespace RetroLib.Palettes;

/// <summary>
/// Class to convert Bitmap RGB palette to 9 bit format.
/// </summary>
public class _9bitPalette
{
    /// <summary>
    /// Imports a color palette from a BEX file and applies it to a Bitmap image.
    /// </summary>
    /// <param name="bexFilePath">The file path of the BEX file to import the color palette from.</param>
    /// <param name="bmp">The Bitmap image to which the color palette will be applied.</param>
    public static HashSet<Color> GetPaletteFromBex(string bexFilePath)
    {
        if (!File.Exists(bexFilePath))
        {
            throw new FileNotFoundException("The specified BEX file does not exist.");
        }

        string bexContent = File.ReadAllText(bexFilePath);
        HashSet<Color> palette = ParseBexPaletteString(bexContent);
        return palette;
    }

    private static HashSet<Color> ParseBexPaletteString(string bexContent)
    {
        // Разделяем строку по запятым
        var parts = bexContent.Split(',');

        // Создаём список для хранения шестнадцатеричных строк
        List<string> hexValues = new();

        foreach (var part in parts)
        {
            // Находим индекс символа $, который предшествует шестнадцатеричному значению
            int dollarIndex = part.IndexOf('$');
            if (dollarIndex != -1)
            {
                // Извлекаем шестнадцатеричное значение после символа $
                string hexValue = part.Substring(dollarIndex + 1).Trim();
                hexValues.Add(hexValue);
            }
        }

        // Преобразуем каждое шестнадцатеричное значение в цвет
        return [.. hexValues.Select(hex => Convert9bitToColor(int.Parse(hex, System.Globalization.NumberStyles.HexNumber)))];
    }

    public static void ExportToBin(Bitmap bmp, string outFilePath)
    {
        var palette = Palette.GetPalette(bmp);
        List<UInt16> colors = ConvertColorsTo9bit(palette);

        using BinaryWriter writer = new(File.Open(outFilePath, FileMode.Create));
        foreach (var color in colors)
        {
            byte firstByte = (byte)(color >> 8);
            byte sacondByte = (byte)color;

            // Записываем два байта в файл
            writer.Write(firstByte);  // Пишем старший байт
            writer.Write(sacondByte);   // Пишем младший байт
        }
    }

    /// <summary>
    /// Generates a string for BEX palette format and writes it to a file.
    /// </summary>
    /// <param name="bmp">The Bitmap image from which to extract the color palette.</param>
    /// <param name="inFilePath">The file path of the input image.</param>
    /// <param name="outFilePath">The optional file path for the output BEX file. If not provided, the BEX file is created in the same directory as the input file with the same name and a .bex extension.</param>
    internal static void ExportToBexFile(Bitmap bmp, string outFilePath)
    {
        HashSet<Color> palette = Palette.GetPalette(bmp);

        ExportToBexFile(palette, outFilePath);
    }

    public static void ExportToBexFile(HashSet<Color> palette, string outFilePath)
    {
        string bexPalStr = GenerateBasiePaletteString(palette, outFilePath);
        outFilePath = Path.ChangeExtension(outFilePath, "_pal.bex");

        File.WriteAllText(outFilePath, bexPalStr);
    }

    /// <summary>
    /// Generate to BEX string palette format.
    /// </summary>
    /// <param name="bmp">Bitmap image to extract palette from.</param>
    /// <param name="filePath">Path of the file to write the palette string.</param>
    public static string GenerateBasiePaletteString(HashSet<Color> palette, string filePath)
    {
        string paletteString = GenerateHexPaletteString(palette);
        string fileName = Path.GetFileNameWithoutExtension(filePath).Replace(" ", "_");
        return $"{fileName}_pal: dataint     {paletteString}";
    }

    /// <summary>
    /// Generates a hexadecimal palette string from a set of colors.
    /// </summary>
    /// <param name="palette">The set of colors to generate the palette string from.</param>
    /// <returns>A string representing the hexadecimal palette.</returns>
    private static string GenerateHexPaletteString(HashSet<Color> palette)
    {
        if (palette.Count > 16)
        {
            Console.WriteLine("Warning: Color palette has more than 16 colors");
        }

        List<UInt16> palette9bit = ConvertColorsTo9bit(palette);

        List<string> hexPalette = [.. palette9bit.Select(x => $"${x:X3}")];

        string hexPaletteString = string.Join(", ", hexPalette);

        return hexPaletteString;
    }



    /// <summary>
    /// Converts a palette of colors to 9 bit format.
    /// </summary>
    /// <param name="palette">Palette of colors to convert.</param>
    /// <returns>List of 9 bit colors.</returns>
    public static List<UInt16> ConvertColorsTo9bit(HashSet<Color> palette)
    {
        return [.. palette.Select(ConvertColorTo9bit)];
    }

    ///<summary>
    ///This algorithm is used to convert an RGB color value into a 16-bit representation.
    ///<remarks>
    ///Let's break down the steps:
    ///1.
    ///color.R >> 5: This shifts the red component of the color value to the right by 5 bits.This effectively divides the red component by 32.
    ///2.
    ///(color.R >> 5) << 9: After shifting the red component, this shifts it back to the left by 9 bits.This effectively multiplies the red component by 512.
    ///3.
    ///color.G >> 5: This shifts the green component of the color value to the right by 5 bits.This effectively divides the green component by 32.
    ///4.
    ///(color.G >> 5) << 5: After shifting the green component, this shifts it back to the left by 5 bits.This effectively multiplies the green component by 32.
    ///5.
    ///color.B >> 5: This shifts the blue component of the color value to the right by 5 bits.This effectively divides the blue component by 32.
    ///6.
    ///(color.B >> 5) << 1: After shifting the blue component, this shifts it back to the left by 1 bit.This effectively multiplies the blue component by 2.
    ///7.
    ///((color.R >> 5) << 9) | ((color.G >> 5) << 5) | ((color.B >> 5) << 1): Finally, this combines the shifted red, green, and blue components using bitwise OR operations to create a 16-bit representation of the color.The red component occupies the most significant 5 bits, the green component occupies the next 6 bits, and the blue component occupies the least significant 5 bits.
    ///    Overall, this algorithm takes an RGB color value, divides each component by 32, and combines the components into a 16-bit representation.
    ///</remarks>
    ///</summary>
    private static UInt16 ConvertColorTo9bit(Color color)
    {
        int r = color.R / 16;
        int g = color.G / 16;
        int b = color.B / 16;

        return (ushort)((r << 8) | (g << 4) | b);
    }


    /// <summary>
    /// Converts a list of 9 bit color values to a HashSet of Color objects.
    /// </summary>
    /// <param name="palette9bit">List of 9 bit color values to convert.</param>
    /// <returns>A HashSet of Color objects.</returns>
    public static HashSet<Color> Convert9bitToColors(List<int> palette9bit)
    {
        HashSet<Color> palette = [];
        foreach (var color9bit in palette9bit)
        {
            Color color = Convert9bitToColor(color9bit);
            palette.Add(color);
        }
        return palette;
    }

    /// <summary>
    /// Converts a RGB color to 9 bit format.
    /// </summary>
    /// <param name="color">Color to convert.</param>
    /// <returns>9 bit color.</returns>

    private static Color Convert9bitToColor(int color9bit)
    {
        int b = color9bit >> 9 & 0x7;
        int g = color9bit >> 5 & 0x7;
        int r = color9bit >> 1 & 0x7;
        return Color.FromArgb(r << 5, g << 5, b << 5);
    }

    /// <summary>
    /// Prints all RGB colors and their corresponding 9 bit color representation to the console.
    /// </summary>
    public static void PrintRGBto9bitSet()
    {
        for (int r = 0; r < 256; r += 16) // Increment by 32 to simulate 3 bits for red.
        {
            for (int g = 0; g < 256; g += 16) // Increment by 32 to simulate 3 bits for green.
            {
                for (int b = 0; b < 256; b += 16) // Increment by 32 to simulate 3 bits for blue.
                {
                    Color color = Color.FromArgb(r, g, b);
                    int color9bit = ConvertColorTo9bit(color);
                    //Console.WriteLine(color);

                    Console.WriteLine($"RGB({r}, {g}, {b}) => 9bit: {color9bit:X3}");
                }
            }
        }
    }
}
