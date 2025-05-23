﻿using ImageMagick;
using RetroLib.General;
using RetroLib.Palettes;
using System.Drawing;
using System.Text;

namespace RetroLib.Platforms
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

        public static void ConvertBmpToBkg(Bitmap bitmap, string outFilePath)
        {
            using FileStream fs = new(outFilePath, FileMode.Create);
            using BinaryWriter writer = new(fs);

            int HTileCount = bitmap.Width / TILE_SIZE;
            int VTileCount = bitmap.Height / TILE_SIZE;

            List<List<int[,]>> uniqTiles = [];
            List<List<int>> tileMap = [];

            HashSet<Color> palette = Palette.GetPalette(bitmap);
            var (firstPal, secondPal) = SplitPalette(palette);
            List<UInt16> firstPal9bit = _9bitPalette.ConvertColorsTo9bit(firstPal);
            List<UInt16> secondPal9bit = _9bitPalette.ConvertColorsTo9bit(secondPal);

            uniqTiles.Add(GetUniqueTiles(bitmap, firstPal, HTileCount, VTileCount, isReverse: true));//Layer B
            if (secondPal != null && secondPal.Count > 0)
                uniqTiles.Add(GetUniqueTiles(bitmap, secondPal, HTileCount, VTileCount));//Layer A


            tileMap.Add(GetTileMap(bitmap, uniqTiles[0], firstPal));
            if (secondPal != null && secondPal.Count > 0)
                tileMap.Add(GetTileMap(bitmap, uniqTiles[1], secondPal, uniqTiles[0].Count));

            ushort Planes = 1;
            if (secondPal != null && secondPal.Count > 0)
                Planes = 2;

            writer.Write(['B', 'K', 'G', '\0']); // Signature
            writer.Write((ushort)0x0101); // Version

            UInt16 optimCount = 0;
            if (uniqTiles.Count > 1)
            {
                optimCount = (UInt16)(uniqTiles[0].Count + uniqTiles[1].Count);
            }
            else
            {
                optimCount = (UInt16)uniqTiles[0].Count;
            }
            byte[] optimCountBytes = BitConverter.GetBytes(optimCount);
            Array.Reverse(optimCountBytes);
            writer.Write(optimCountBytes, 0, 2);

            byte[] widthBytes = BitConverter.GetBytes((ushort)HTileCount);
            Array.Reverse(widthBytes);
            writer.Write(widthBytes, 0, 2);

            byte[] heightBytes = BitConverter.GetBytes((ushort)VTileCount);
            Array.Reverse(heightBytes);
            writer.Write(heightBytes, 0, 2);

            byte[] planesBytes = BitConverter.GetBytes(Planes);
            Array.Reverse(planesBytes);
            writer.Write(planesBytes, 0, 2);


            WriteTilesToBinary(uniqTiles, writer);
            WriteTileMapToBinary(tileMap[0], writer);
            if (tileMap.Count > 1)
                WriteTileMapToBinary(tileMap[1], writer);
            WritePaletteToBinary(firstPal9bit, writer);
            WritePaletteToBinary(secondPal9bit, writer);
        }

        public static bool AllElementsEqual<T>(List<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count == 0)
                return true; // Пустой список считается с одинаковыми элементами

            T firstElement = list[0];
            return list.All(element => EqualityComparer<T>.Default.Equals(element, firstElement));
        }

        /// <summary>
        /// функция разделяет на 2 палитры по 16 цветов каждая
        /// </summary>
        /// <param name="palette"></param>
        /// <returns></returns>
        public static (HashSet<Color>, HashSet<Color>) SplitPalette(HashSet<Color> palette)
        {
            ArgumentNullException.ThrowIfNull(palette);

            HashSet<Color> first16 = new(16);
            HashSet<Color> remaining = new(16);

            //FIXME:
            Color black = palette.FirstOrDefault(c => c.ToArgb() <= Color.FromArgb(15, 15, 15).ToArgb());

            if (black.A != 0 || black.R != 0 || black.G != 0 || black.B != 0)
            {
                first16.Add(Color.FromArgb(0, 0, 0));//черный цвет обязательно должен быть первым в палитре
                palette.Remove(black);
            }

            //NOTE: нужно для белых текстовых полей
            Color white = palette.FirstOrDefault(c => c.ToArgb() >= Color.FromArgb(224, 224, 224).ToArgb());
            if (white.A != 0 || white.R != 0 || white.G != 0 || white.B != 0)
            {
                first16.Add(white); // белый цвет обязательно должен быть в первой палитре
                palette.Remove(white);
            }

            foreach (var color in palette)
            {
                if (first16.Count < 16)
                {
                    first16.Add(color);
                }
                else
                {
                    remaining.Add(color);
                }
            }

            // Если в remaining есть элементы, добавляем черный цвет в начало
            if (remaining.Count > 0)
            {
                // Создаем новый HashSet с черным цветом в начале
                var newRemaining = new HashSet<Color> { Color.Black };
                foreach (var color in remaining)
                {
                    newRemaining.Add(color);
                }
                remaining = newRemaining;
            }

            return (first16, remaining);
        }

        public static List<int[,]> GetUniqueTiles(Bitmap bitmap)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            HashSet<Color> palette = Palette.GetPalette(bitmap);

            int widthInTiles = bitmap.Width / TILE_SIZE;
            int heightInTiles = bitmap.Height / TILE_SIZE;

            return GetUniqueTiles(bitmap, palette, widthInTiles, heightInTiles);
        }

        public static List<int[,]> GetUniqueTiles(Bitmap bitmap, HashSet<Color> palette)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            int widthInTiles = bitmap.Width / TILE_SIZE;
            int heightInTiles = bitmap.Height / TILE_SIZE;

            return GetUniqueTiles(bitmap, palette, widthInTiles, heightInTiles);
        }
        public static List<int[,]> GetUniqueTiles(Bitmap bitmap, HashSet<Color> palette, int widthInTiles, int heightInTiles, bool isReverse = false)
        {
            List<Color> palList = [.. palette];
            return GetUniqueTiles(bitmap, palList, widthInTiles, heightInTiles, isReverse);
        }
        public static List<int[,]> GetUniqueTiles(Bitmap bitmap, List<Color> palette, int widthInTiles, int heightInTiles, bool isReverse = false)
        {
            ArgumentNullException.ThrowIfNull(bitmap);
            ArgumentNullException.ThrowIfNull(palette);

            List<int[,]> uniqueTiles = [];

            //if (palette.Count == 2)
            //{
            //    uniqueTiles.Add(GetFillTile());
            //    return uniqueTiles;
            //}

            for (int y = 0; y < heightInTiles; y++)
            {
                // Определяем реальную координату Y в зависимости от isReverse
                int actualY = isReverse ? (heightInTiles - 1 - y) : y;

                for (int x = 0; x < widthInTiles; x++)
                {
                    int[,] tile = new int[TILE_SIZE, TILE_SIZE];

                    // Заполняем тайл пикселями
                    for (int ty = 0; ty < TILE_SIZE; ty++)
                    {
                        for (int tx = 0; tx < TILE_SIZE; tx++)
                        {
                            int pixelX = x * TILE_SIZE + tx;
                            int pixelY = actualY * TILE_SIZE + ty;

                            if (pixelX < bitmap.Width && pixelY < bitmap.Height)
                            {
                                Color pixel = bitmap.GetPixel(pixelX, pixelY);
                                if (palette.IndexOf(pixel) < 0)
                                {
                                    tile[ty, tx] = 0;
                                    continue;
                                }
                                tile[ty, tx] = palette.IndexOf(pixel);
                            }
                            else
                            {
                                tile[ty, tx] = 0;
                            }
                        }
                    }

                    // Проверяем, есть ли такой тайл уже в списке
                    bool isUnique = true;
                    foreach (var existingTile in uniqueTiles)
                    {
                        bool tilesEqual = true;
                        for (int i = 0; i < TILE_SIZE && tilesEqual; i++)
                        {
                            for (int j = 0; j < TILE_SIZE && tilesEqual; j++)
                            {
                                if (tile[i, j] != existingTile[i, j])
                                {
                                    tilesEqual = false;
                                }
                            }
                        }
                        if (tilesEqual)
                        {
                            isUnique = false;
                            break;
                        }
                    }

                    if (isUnique)
                    {
                        uniqueTiles.Add(tile);
                    }
                }
            }

            return uniqueTiles;
        }

        private static int[,] GetFillTile()
        {
            int[,] tile = new int[TILE_SIZE, TILE_SIZE];

            for (int ty = 0; ty < TILE_SIZE; ty++)
            {
                for (int tx = 0; tx < TILE_SIZE; tx++)
                {
                    tile[ty, tx] = 1;
                }
            }
            return tile;
        }

        public static List<int> GetTileMap(Bitmap bitmap, List<int[,]> uniqueTiles, HashSet<Color> palette, int offset = 0)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            List<int> tileMap = [];
            List<Color> palList = [.. palette];

            if (uniqueTiles.Count == 1)
            {
                for (int y = 0; y < bitmap.Height / TILE_SIZE; y++)
                {
                    for (int x = 0; x < bitmap.Width / TILE_SIZE; x++)
                    {
                        tileMap.Add((0 + offset));
                    }
                }
                return tileMap;
            }

            int widthInTiles = bitmap.Width / TILE_SIZE;
            int heightInTiles = bitmap.Height / TILE_SIZE;

            for (int y = 0; y < heightInTiles; y++)
            {
                for (int x = 0; x < widthInTiles; x++)
                {
                    int[,] tile = new int[TILE_SIZE, TILE_SIZE];

                    // Заполняем тайл пикселями
                    for (int ty = 0; ty < TILE_SIZE; ty++)
                    {
                        for (int tx = 0; tx < TILE_SIZE; tx++)
                        {
                            int pixelX = x * TILE_SIZE + tx;
                            int pixelY = y * TILE_SIZE + ty;

                            if (pixelX < bitmap.Width && pixelY < bitmap.Height)
                            {
                                Color pixel = bitmap.GetPixel(pixelX, pixelY);
                                if (palList.IndexOf(pixel) < 0)
                                {
                                    tile[ty, tx] = 0;
                                    continue;
                                }
                                tile[ty, tx] = palList.IndexOf(pixel);
                            }
                            else
                            {
                                tile[ty, tx] = 0;
                            }
                        }
                    }

                    // Находим индекс тайла с помощью специального сравнения
                    int tileIndex = FindTileIndex(uniqueTiles, tile);
                    if (tileIndex < 0) tileIndex = 0 + offset;
                    tileMap.Add(tileIndex + offset);
                }
            }

            return tileMap;
        }

        private static int FindTileIndex(List<int[,]> uniqueTiles, int[,] tile)
        {
            for (int i = 0; i < uniqueTiles.Count; i++)
            {
                if (AreTilesEqual(uniqueTiles[i], tile))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool AreTilesEqual(int[,] tile1, int[,] tile2)
        {
            if (tile1.GetLength(0) != tile2.GetLength(0) || tile1.GetLength(1) != tile2.GetLength(1))
                return false;

            for (int y = 0; y < tile1.GetLength(0); y++)
            {
                for (int x = 0; x < tile1.GetLength(1); x++)
                {
                    if (tile1[y, x] != tile2[y, x])
                        return false;
                }
            }
            return true;
        }

        // Преобразует тайл в строку для использования в Dictionary
        private static string TileToString(int[,] tile)
        {
            StringBuilder sb = new();
            for (int i = 0; i < tile.GetLength(0); i++)
            {
                for (int j = 0; j < tile.GetLength(1); j++)
                {
                    sb.Append(tile[i, j].ToString("X8")); // Формат ARGB в HEX
                }
            }
            return sb.ToString();
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

                    if (bmpY + TILE_SIZE < size.Height)
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

        public static void ConvertPcxToBkg(string inFilePath, string outFilePath)
        {
            Bitmap bmp;
            using var image = new MagickImage(inFilePath);
            using (var ms = new MemoryStream())
            {
                image.Write(ms, MagickFormat.Bmp);
                ms.Position = 0;
                bmp = new Bitmap(ms);
            }
            ConvertBmpToBkg(bmp, outFilePath);
        }

        private static List<int[,]> ReadTilesFromBinary(string inFilePath, Size size)
        {
            List<int[,]> tiles = [];
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
            if (segaImgType == SegaImgType.screen)
            {
                throw new Exception("Данный тип не поддерживается");
                //List<int[,]> tiles = ConvertBitmapToFontTiles(bmp);
                //TODO добавить создание тайловой карты
                //WriteTilesToBinary(tiles, outFilePath);
            }
            else if (segaImgType == SegaImgType.sprite)
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
                List<int[,]> tiles = GetTilesFromBitmap(bmp);
                WriteTilesToBinary(tiles, outFilePath);
            }
            else
            {
                throw new Exception("Данный тип не поддерживается");
            }
        }

        public static List<int[,]> GetTilesFromBitmap(Bitmap bmp)
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
                Point startPoint = new(X, Y);
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
            WriteTilesToBinary(tiles, writer);
        }

        public static void WriteTilesToBinary(List<List<int[,]>> tiles, BinaryWriter writer)
        {
            foreach (var tile in tiles)
            {
                WriteTilesToBinary(tile, writer);
            }
        }
        public static void WriteTilesToBinary(List<int[,]> tiles, BinaryWriter writer)
        {
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

        public static void WriteTileMapToBinary(List<int> tileMap, string filePath)
        {
            using BinaryWriter writer = new(File.Open(filePath, FileMode.Create));
            WriteTileMapToBinary(tileMap, writer);
        }

        public static void WriteTileMapToBinary(List<List<int>> tileMap, BinaryWriter writer)
        {
            foreach (var tile in tileMap)
            {
                WriteTileMapToBinary(tile, writer);
            }
        }
        public static void WriteTileMapToBinary(List<int> tileMap, BinaryWriter writer)
        {
            ArgumentNullException.ThrowIfNull(tileMap);

            foreach (int tileIndex in tileMap)
            {
                if (tileIndex < 0 || tileIndex > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(
                        nameof(tileMap),
                        $"Tile index {tileIndex} is out of range (0-{ushort.MaxValue})."
                    );

                // Записываем int как 2 байта (ushort, big-endian)
                byte[] bytes = BitConverter.GetBytes((ushort)tileIndex);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                writer.Write(bytes);
            }
        }

        public static void WritePaletteToBinary(List<UInt16> palette, string filePath)
        {
            using BinaryWriter writer = new(File.Open(filePath, FileMode.Create));
            WritePaletteToBinary(palette, writer);
        }

        public static void WritePaletteToBinary(List<List<UInt16>> palette, BinaryWriter writer)
        {
            foreach (var tile in palette)
            {
                WritePaletteToBinary(tile, writer);
            }
        }

        public static void WritePaletteToBinary(List<UInt16> palette, BinaryWriter writer)
        {
            if (palette == null || writer == null)
                throw new ArgumentNullException();

            // Записываем первые 16 цветов (или все, если их меньше)
            int colorsToWrite = Math.Min(palette.Count, 16);
            for (int i = 0; i < colorsToWrite; i++)
            {
                UInt16 color = palette[i];
                byte[] bytes = BitConverter.GetBytes(color);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes); // big-endian порядок байт
                writer.Write(bytes);
            }

            // Дополняем до 16 цветов нулями (0x0000)
            for (int i = colorsToWrite; i < 16; i++)
            {
                writer.Write([0x00, 0x00]); // 0x0000
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
            List<Color> palList = [.. palette];//TODO: в дальнейшем желательно убрать

            for (int y = 0; y < TILE_SIZE; y++)
            {
                for (int x = 0; x < TILE_SIZE; x++)
                {
                    if (x0 + x >= bmp.Width || y0 + y >= bmp.Height)//TODO: поменять местами условия?
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
