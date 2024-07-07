using RetroLib.General;
using System.Drawing;

namespace RIConvert.Platforms
{
    /// <summary>
    /// Здесь содержаться методы по преобразованию изображения относящиеся к консоли Sega Mega Drive
    /// </summary>
    public class SegaGenesis
    {
        const int TILE_SIZE = 8;//pixel
        const int MAX_TILE_SIZE = 32;//pixels

        public enum SegaImgType
        {
            screen,
            sprite,
            window,
            font
        }

        public static void ConvertChrToBmp(HashSet<Color> palette, Size size, string inFilePath, string outFilePath, SegaImgType segaImgType = SegaImgType.screen)
        {
            long expectedFileSize = (size.Width * size.Height) / 2;
            long fileLength = new FileInfo(inFilePath).Length;
            Size tileLayout = CalculateTileLayout(fileLength);

            if (fileLength != expectedFileSize && fileLength > 512)
            {
                throw new ArgumentException($"Размер файла не соответствует ожидаемому размеру {expectedFileSize} байт " +
                    $"для заданных размеров изображения {size.Width}x{size.Height}." +
                    $" Подходящее разрешение {tileLayout.Width}x{tileLayout.Height}.");
            }

            Bitmap bmp = new(size.Width, size.Height);
            
            if (segaImgType == SegaImgType.screen)
            {
                List<int[,]> tiles = ReadTilesFromBinary(inFilePath, size);
            }
            else if (segaImgType == SegaImgType.sprite)
            {

                List<int[,]> tiles = ReadTilesFromBinary(inFilePath, size);
                int bmpX = 0;
                int bmpY = 0;
                foreach (var tile in tiles)
                {
                    for (int y = 0; y < TILE_SIZE; y++)
                    {
                        for (int x = 0; x < TILE_SIZE; x++)
                        {
                            bmp.SetPixel(bmpX + x, bmpY + y, palette.ElementAt(tile[y, x]));
                        }
                    }

                    if(bmpY + TILE_SIZE < size.Height)
                    {
                        bmpY += TILE_SIZE;
                    }
                    else
                    {
                        bmpY = 0;
                        bmpX += TILE_SIZE;
                    }
                }

                bmp.Save(outFilePath);                
            }
        }

        private static List<int[,]> ReadTilesFromBinary(string inFilePath, Size size)
        {
            List<int[,]> tiles = new();
            int tilesPerRow = size.Width / TILE_SIZE;
            int tilesPerColumn = size.Height / TILE_SIZE;
            int totalTiles = tilesPerRow * tilesPerColumn;

            using (BinaryReader reader = new BinaryReader(File.Open(inFilePath, FileMode.Open)))
            {
                for (int tileIndex = 0; tileIndex < totalTiles; tileIndex++)
                {
                    int[,] tile = new int[TILE_SIZE, TILE_SIZE];

                    for (int row = 0; row < TILE_SIZE; row++)
                    {
                        for (int col = 0; col < TILE_SIZE; col += 2) // Читаем каждые два пикселя за раз
                        {
                            if (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                byte byteData = reader.ReadByte();
                                int firstPixel = (byteData >> 4) & 0xF; // Выделяем старшие 4 бита
                                int secondPixel = byteData & 0xF; // Выделяем младшие 4 бита

                                tile[row, col] = firstPixel;
                                if (col + 1 < TILE_SIZE)
                                {
                                    tile[row, col + 1] = secondPixel;
                                }
                            }
                        }
                    }

                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        public static void ConvertBmpToChr(string inFilePath, string outFilePath, SegaImgType segaImgType = SegaImgType.screen)
        {
            ConvertBmpToChr(new Bitmap(inFilePath), outFilePath, segaImgType);
        }

        public static void ConvertBmpToChr(Bitmap bmp, string outFilePath, SegaImgType segaImgType = SegaImgType.screen)
        {
            if(segaImgType == SegaImgType.screen)
            {
                List<int[,]> tiles = ConvertBitmapToBkgTiles(bmp);
                //TODO добавить создпние тайловой карты
                WriteTilesToBinary(tiles, outFilePath);
            }
            else if(segaImgType == SegaImgType.sprite)
            {
                List<List<int[,]>> sprites = ConvertBitmapToSprTiles(bmp);
                File.Delete(outFilePath);
                int spriteIndex = 0;
                foreach (List<int[,]> sprite in sprites)
                {
                    //var templateVars = new
                    //{
                    //    classname = baseFileName,
                    //    type = type,
                    //    x = X,
                    //    y = Y,
                    //    size = Math.Max(bmp.Width, bmp.Height),
                    //    sprite_index = string.Format("0x{0:X3}", spriteIndex),
                    //    sprite_location = string.Format("0x{0:X4}", default_spriteLocation),
                    //    next_link = $"SPRITE_INDEX + 1",
                    //    sprite_horizontal_size = bmpWidth / TILESIZE,
                    //    sprite_vertical_size = bmpHeight / TILESIZE,
                    //    patterns = patterns,
                    //    palette = hexPalette
                    //};
                    string indexedOutFilePath = Path.Combine(Path.GetDirectoryName(outFilePath), 
                        $"{Path.GetFileNameWithoutExtension(outFilePath)}_{spriteIndex++}{Path.GetExtension(outFilePath)}");//не заменять на Path.GetDirectoryName(outFilePath)}\\
                    WriteTilesToBinary(sprite, indexedOutFilePath);
                }
            }
            else if (segaImgType == SegaImgType.font)
            {
                List<int[,]> tiles = ConvertBitmapToBkgTiles(bmp);
                WriteTilesToBinary(tiles, outFilePath);
            }
            else
            {
                throw new Exception("Данный тип не поддерживается");
            }
        }

        public static List<int[,]> ConvertBitmapToBkgTiles(Bitmap bmp)
        {
            List<int[,]> tiles = [];
            HashSet<Color> palette = Palette.GetPalette(bmp);

            for (int y0 = 0; y0 < bmp.Height; y0 += 8)
            {
                for (int x0 = 0; x0 < bmp.Width; x0 += 8)
                {
                    var tile = ExtractTileFromImage(bmp, palette, x0, y0);
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        private static List<List<int[,]>> ConvertBitmapToSprTiles(Bitmap bmp)
        {

            int X = 0;
            int Y = 0;
            List<int[,]> tiles = [];
            List<List<int[,]>> sprites = [];
            HashSet<Color> palette = Palette.GetPalette(bmp);

            while (X < bmp.Width || Y < bmp.Height)
            {
                Point startPoint = new Point(X, Y);
                Size spriteSize = GetMaxTileSize(startPoint, bmp.Size);

                for (int x0 = startPoint.X; x0 < startPoint.X + spriteSize.Width; x0 += TILE_SIZE)
                {
                    for (int y0 = startPoint.Y; y0 < startPoint.Y + spriteSize.Height; y0 += TILE_SIZE)
                    {
                        var tile = ExtractTileFromImage(bmp, palette, x0, y0);
                        tiles.Add(tile);
                    }
                }

                sprites.Add(tiles.ConvertAll(tile => (int[,])tile.Clone()));
                tiles.Clear();

                #region Проверка что координаты достигли конца изображения
                X += spriteSize.Width;
                if (X >= bmp.Width)
                {
                    Y += spriteSize.Height;
                    if (Y >= bmp.Height)
                    {
                        break;
                    }
                    X = 0;
                }
                #endregion
            }
            return sprites;
        }


        public static void WriteTilesToBinary(List<int[,]> tiles, string filePath)
        {
            using BinaryWriter writer = new(File.Open(filePath, FileMode.Create));
            foreach (var tile in tiles)
            {
                for (int i = 0; i < tile.GetLength(0); i++)
                {
                    for (int j = 0; j < tile.GetLength(1); j += 2)
                    {
                        int combinedValue = (tile[i, j] << 4) | tile[i, j + 1];
                        writer.Write((byte)combinedValue);
                    }
                }
            }
        }




        /// <summary>
        /// Calculates the maximum sprite tile size within the given rectangle defined by the start point and size. (0-32)
        /// </summary>
        /// <param name="startPoint">The starting point of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        /// <returns>The maximum tile size within the rectangle.</returns>
        private static Size GetMaxTileSize(Point startPoint, Size size)
        {
            int x;
            int y;
            for (x = 0; x + startPoint.X < startPoint.X + MAX_TILE_SIZE; x += TILE_SIZE)
            {
                if (x + startPoint.X >= size.Width)
                {
                    break;
                }
            }
            for (y = 0; y + startPoint.Y < startPoint.Y + MAX_TILE_SIZE; y += TILE_SIZE)
            {
                if (y + startPoint.Y >= size.Height)
                {
                    break;
                }
            }
            return new Size(x, y);
        }

        private static int GetMaxTileSize(int startIndex, int size)
        {
            int x;
            for (x = 0; x + startIndex < startIndex + MAX_TILE_SIZE; x += 8)
            {
                if (x + startIndex >= size)
                {
                    return x;
                }
            }
            return x;
        }

        private static int[,] ExtractTileFromImage(Bitmap bmp, HashSet<Color> palette, int x0, int y0)
        {
            int[,] tile = new int[TILE_SIZE, TILE_SIZE];
            List<Color> palList = [.. palette];//TODO в дальнейшем желательно убрать

            for (int y = 0; y < TILE_SIZE; y++)
            {
                for (int x = 0; x < TILE_SIZE; x++)
                {
                    if (x0 + x >= bmp.Width || y0 + y >= bmp.Height)//TODO поменять местами условия?
                    {
                        tile[y, x] = 0;
                    }
                    else
                    {
                        Color color = bmp.GetPixel(x0 + x, y0 + y);
                        tile[y, x] = palList.IndexOf(color);
                    }
                }
            }

            return tile;
        }

        private static Size CalculateTileLayout(long fileSize)
        {
            int tileNum = (int)(fileSize / 32);  // Количество тайлов, каждый размером 32 байта
            int maxWidth = (int)Math.Sqrt(tileNum);  // Максимальная ширина, чтобы получить более квадратный прямоугольник

            // Находим максимально возможные делители числа tileNum для формирования прямоугольника
            int bestWidth = 1;
            int bestHeight = tileNum;  // Начальные значения - все тайлы в одну строку

            for (int width = 1; width <= maxWidth; width++)
            {
                if (tileNum % width == 0)  // Если width является делителем tileNum
                {
                    int height = tileNum / width;
                    if (width * height == tileNum)
                    {
                        bestWidth = width;
                        bestHeight = height;
                    }
                }
            }

            return new Size(bestWidth, bestHeight);
        }

    }
}
