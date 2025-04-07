using RetroLib.General;
using RetroLib.Palettes;
using System.Drawing;
using static RetroLib.Platforms.SegaGenesis;

namespace RetroLib.Engines
{
    public class Basiegaxor
    {
        public static void Convert(Bitmap bmp, string outFilePath, SegaImgType segaImgType = SegaImgType.screen)
        {
            string bexPalStr = _9bitPalette.GenerateBasiePaletteString(Palette.GetPalette(bmp), outFilePath);
            string fileName = Path.GetFileNameWithoutExtension(outFilePath).Replace(" ", "_");

            if (segaImgType == SegaImgType.screen)
            {
                string bexBkgStr = $"{fileName}:\t\t\tdatafile\t{fileName.ToLower()}.BIN,BIN";
                string bexStr = $"{bexBkgStr}\n{bexPalStr}";
                File.WriteAllText(Path.ChangeExtension(outFilePath, "bex"), bexStr);
            }
            else
            {
                throw new Exception("в данный момент не поддерживается");
            }
        }
    }
}
