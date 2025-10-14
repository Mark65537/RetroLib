using System.Drawing;

namespace RetroLib.Entity
{
    public class Tile
    {
        /// <summary>
        /// Default tile size
        /// </summary>
        const int TILE_SIZE = 8;

        public Color[,] pixel;

        public Tile()
        {
            pixel = new Color[TILE_SIZE, TILE_SIZE];
        }
        public Tile(uint size)
        {
            pixel = new Color[size, size];
        }

        public Tile(uint width, uint height)
        {
            pixel = new Color[width, height];
        }

        public Tile(Color[,] pixel)
        {
            this.pixel = pixel;
        }

    }
}
