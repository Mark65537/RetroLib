using System.Drawing;
using System.Drawing.Imaging;
//using ImageMagick;

namespace RetroLib.General
{
    public class Palette
    {
        //TODO что делать с этой функцией
        private static Color GetMostFrequentColor(Bitmap bmp)
        {
            List<Color> colors = [];

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color color = bmp.GetPixel(x, y);
                    colors.Add(color);
                }
            }

            Color mostFrequentColor = FindMostFrequentColor(colors);
            return mostFrequentColor;
        }

        public static Color FindMostFrequentColor(List<Color> colors)
        {
            Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();

            foreach (Color color in colors)
            {
                if (colorCounts.ContainsKey(color))
                {
                    colorCounts[color]++;
                }
                else
                {
                    colorCounts.Add(color, 1);
                }
            }

            int maxCount = colorCounts.Values.Max();
            Color mostFrequentColor = colorCounts.FirstOrDefault(x => x.Value == maxCount).Key;

            return mostFrequentColor;
        }

        public static Color FindClosestPaletteColor(Color color, HashSet<Color> palette)
        {
            int minDistanceSquared = int.MaxValue;
            Color closestColor = Color.Empty;

            foreach (Color paletteColor in palette)
            {
                int Rdiff = color.R - paletteColor.R;
                int Gdiff = color.G - paletteColor.G;
                int Bdiff = color.B - paletteColor.B;
                int distanceSquared = Rdiff * Rdiff + Gdiff * Gdiff + Bdiff * Bdiff;

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    closestColor = paletteColor;
                }
            }

            return closestColor;
        }

        private static Color GetMostFrequentColor(string filePath)
        {
            return GetMostFrequentColor(new Bitmap(filePath));
        }

        public static Color FindMostSimilarColor(HashSet<Color> colors, Color pixelColor)
        {
            if (colors.Contains(pixelColor))
            {
                return pixelColor;
            }

            double minDistance = double.MaxValue;
            Color mostSimilarColor = Color.Empty;

            foreach (Color color in colors)
            {
                double distance = CalculateColorDistance(color, pixelColor);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    mostSimilarColor = color;
                }
            }

            return mostSimilarColor;
        }

        private static double CalculateColorDistance(Color color1, Color color2)
        {
            double redDifference = color1.R - color2.R;
            double greenDifference = color1.G - color2.G;
            double blueDifference = color1.B - color2.B;

            double distance = Math.Sqrt(redDifference * redDifference + greenDifference * greenDifference + blueDifference * blueDifference);

            return distance;
        }

        //<summary>
        //переводит в граничные цвета
        public static Bitmap ConvertTo16Color(Bitmap originalImage)
        {
            // Create a new 16-color bitmap
            Bitmap convertedImage = originalImage.Clone(
                new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                PixelFormat.Format4bppIndexed);

            // Set the palette to a 16-color palette
            ColorPalette palette = convertedImage.Palette;
            for (int i = 0; i < 16; i++)
            {

                //int r = (i & 0x4) != 0 ? 0xFF : 0x00;
                //int g = (i & 0x2) != 0 ? 0xFF : 0x00;
                //int b = (i & 0x1) != 0 ? 0xFF : 0x00;
                //palette.Entries[i] = Color.FromArgb(r, g, b);

                //palette.Entries[i] = Color.FromArgb(i * 16, i * 16, i * 16);//BAD algoritm. Used for GrayScale

            }
            convertedImage.Palette = palette;

            // Create a blank bitmap with the same dimensions
            Bitmap tempBitmap = new Bitmap(convertedImage.Width, convertedImage.Height);
            tempBitmap.Palette = palette;
            // Draw the original image onto the converted bitmap using a Graphics object
            using (Graphics g = Graphics.FromImage(tempBitmap))
            {
                g.DrawImage(convertedImage, 0, 0, originalImage.Width, originalImage.Height);
            }

            return tempBitmap;
        }

        public static Bitmap ConvertTo4Color(Bitmap bmp)
        {
            Bitmap result = new Bitmap(bmp.Width, bmp.Height);

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color color = bmp.GetPixel(x, y);
                    Color convertedColor = ConvertTo4Color(color);
                    result.SetPixel(x, y, convertedColor);
                }
            }

            return result;
        }

        private static Color ConvertTo4Color(Color color)
        {
            int red = color.R;
            int green = color.G;
            int blue = color.B;

            if (red < 128)
                red = 0;
            else
                red = 255;

            if (green < 128)
                green = 0;
            else
                green = 255;

            if (blue < 128)
                blue = 0;
            else
                blue = 255;

            Color convertedColor = Color.FromArgb(red, green, blue);
            return convertedColor;
        }

        public static Bitmap ConvertTo4ColorWithGrayscale(Bitmap bmp)
        {
            Bitmap result = new Bitmap(bmp.Width, bmp.Height);

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color color = bmp.GetPixel(x, y);
                    Color convertedColor = ConvertTo4ColorWithGrayscale(color);
                    result.SetPixel(x, y, convertedColor);
                }
            }

            return result;
        }



        private static Color ConvertTo4ColorWithGrayscale(Color color)
        {
            int gray = (color.R + color.G + color.B) / 3;
            int threshold = 128;

            if (gray < threshold)
            {
                return Color.FromArgb(0, 0, 0); // Black
            }
            else if (gray < threshold * 2)
            {
                return Color.FromArgb(85, 85, 85); // Dark Gray
            }
            else if (gray < threshold * 3)
            {
                return Color.FromArgb(170, 170, 170); // Light Gray
            }
            else
            {
                return Color.FromArgb(255, 255, 255); // White
            }
        }


        public static Bitmap ConvertTo16(Bitmap image)
        {
            Bitmap newBitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format16bppRgb555);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(image, new Rectangle(0, 0, newBitmap.Width, newBitmap.Height));
            }
            return newBitmap;
        }
        /// <summary>
        /// Сначала извлекается палитра быстрым способом, то береться прямой способ
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="maxColorsCount">Максимальное доступное колличество цветов в палитре</param>
        /// <returns></returns>
        public static HashSet<Color> GetPalette(Bitmap bmp, bool isFast = false)
        {
            HashSet<Color> palette;
            if (isFast)
            {
                palette = GetPaletteFast(bmp);//NOTE: работает быстро, но не надежно
            }
            else
            {
                palette = GetPaletteStright(bmp);
            }
            return palette;
        }

        public static HashSet<Color> GetPaletteStright(Bitmap image)
        {
            // Create a HashSet to store the unique colors
            HashSet<Color> uniqueColors = [];

            // Iterate over each pixel in the image
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // Get the color of the current pixel
                    Color pixelColor = image.GetPixel(x, y);

                    // Add the color to the HashSet
                    uniqueColors.Add(pixelColor);
                }
            }
            return uniqueColors;
        }
        public static HashSet<Color> GetPalette(string filePath)
        {
            return GetPalette(new Bitmap(filePath));
        }

        //TODO не всегда правильно показывает палитру. Добавить тест с тестовым изображением в тесте
        /// <summary>
        /// Быстрый способ получения палитры
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static HashSet<Color> GetPaletteFast(Bitmap image)
        {
            HashSet<Color> uniqueColors = [];
            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int bytesPerPixel = Image.GetPixelFormatSize(image.PixelFormat) / 8;
                    int heightInPixels = bmpData.Height;
                    int widthInPixels = bmpData.Width;

                    for (int y = 0; y < heightInPixels; y++)
                    {
                        byte* currentLine = ptr + y * bmpData.Stride;
                        for (int x = 0; x < widthInPixels; x++)
                        {
                            int pixelIndex = x * bytesPerPixel;
                            int b = currentLine[pixelIndex];
                            int g = currentLine[pixelIndex + 1];
                            int r = currentLine[pixelIndex + 2];
                            int a = bytesPerPixel > 3 ? currentLine[pixelIndex + 3] : 255; // Если формат 32bpp
                            uniqueColors.Add(Color.FromArgb(a, r, g, b));
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(bmpData);
            }
            return uniqueColors;
        }

        public static void Check16Colors(Bitmap bitmap)
        {
            if (!Has16Colors(bitmap))
            {
                throw new Exception("в изображении больше чем 16 цветов");
            }
        }
        public static bool Has16Colors(Bitmap bitmap)
        {
            // Create a HashSet to store the unique colors
            HashSet<Color> uniqueColors = GetPalette(bitmap);

            // Return true if the HashSet contains exactly 16 colors
            return uniqueColors.Count <= 16;
        }


        public static Bitmap MakeTransperentColor(Image img, Color color)
        {
            return MakeTransperentColor(new Bitmap(img), color);
        }

        public static Bitmap MakeTransperentColor(Bitmap bmp, Color color)
        {
            bmp.MakeTransparent(color);
            return bmp;
        }


        public static Bitmap ChangeColor(Image bmp, Color prevColor, Color newColor)
        {
            return ChangeColor(new Bitmap(bmp), prevColor, newColor);
        }

        //TODO удалить данную функцию так как она изменяет исходное изображение
        public static Bitmap ChangeColor(Bitmap bmp, Color prevColor, Color newColor)
        {
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    // Get the color of the current pixel
                    Color pixelColor = bmp.GetPixel(x, y);
                    if (pixelColor.Equals(prevColor))
                    {
                        bmp.SetPixel(x, y, newColor);
                    }
                }

            return bmp;
        }

        //public Image ConvertTo16BitGrayscale(string inputImagePath)
        //{
        //    using (MagickImage image = new MagickImage(inputImagePath))
        //    {
        //        // Set the color type to grayscale
        //        image.ColorType = ColorType.Grayscale;

        //        // Set the bit depth to 16
        //        image.Depth = 16;

        //        // Сохраняем преобразованное изображение
        //        using (var memStream = new MemoryStream())
        //        {
        //            // Write the image to the memorystream
        //            image.Write(memStream);

        //            return new Bitmap(memStream);
        //        }
        //    }
        //}

        public static void ExportImg(HashSet<Color> palette, ImageFormat imgFormat, int squareSize = 8, int squaresPerRow = 8, int squareMerge = 0, string filePath = "output")
        {
            int squaresCount = palette.Count;
            int rowsCount = (int)Math.Ceiling((double)squaresCount / squaresPerRow);
            int borderWidth = 1;
            int bitmapWidth = squaresPerRow * squareSize + squaresPerRow * squareMerge - squareMerge + borderWidth;
            int bitmapHeight = rowsCount * squareSize + rowsCount * squareMerge - squareMerge + borderWidth;

            Bitmap outputBmp = new Bitmap(bitmapWidth, bitmapHeight);
            Graphics g = Graphics.FromImage(outputBmp);
            g.Clear(Color.White);
            int x = 0;
            int y = 0;
            Pen borderPen = new Pen(Color.Black, borderWidth); // Pen for square borders
            foreach (Color color in palette)
            {
                g.FillRectangle(new SolidBrush(color), x, y, squareSize, squareSize);
                g.DrawRectangle(borderPen, x, y, squareSize, squareSize); // Draw square border
                x += squareSize + squareMerge;
                if (x >= bitmapWidth)
                {
                    x = 0;
                    y += squareSize + squareMerge;
                }
            }
            g.Dispose();

            outputBmp.Save($"{filePath}.{imgFormat}", imgFormat);
        }
        internal static void ExportImg(Bitmap inputBmp, ImageFormat imgFormat, int squareSize = 8, int squaresPerRow = 8, int squareMerge = 0, string filePath = "output")
        {
            HashSet<Color> palette = GetPalette(inputBmp);
            ExportImg(palette, imgFormat, squareSize, squaresPerRow, squareMerge, filePath);
        }

    }
}
