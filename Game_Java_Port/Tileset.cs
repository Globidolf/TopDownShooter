using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    
    class Tileset {



        public readonly Bitmap Source;
        public readonly int TileSize;
        private Bitmap[] Tiles;

        private static Dictionary<string, Tileset> Tilesets = new Dictionary<string, Tileset>();
        

        private Tileset(int TileSize, Bitmap Source) {
            this.TileSize = TileSize;
            this.Source = Source;
            int cols = Source.PixelSize.Width / TileSize;
            int rows = Source.PixelSize.Height / TileSize;
            Tiles = new Bitmap[cols * rows];
            for(int y = 0; y < rows; y++) {
                for (int x = 0; x < cols; x++) {
                    Tiles[x + y * cols] = new Bitmap(Program._RenderTarget, new Size2(TileSize, TileSize), new BitmapProperties(new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)));
                    Tiles[x + y * cols].CopyFromBitmap(Source, Point.Zero, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
                }
            }
        }
        
        public Bitmap getRandomTile(Random rng) {
            return Tiles[rng.Next(Tiles.Length)];
        }

        public static Tileset Rock { get {
                string name = "tiles_rock_32_64";
                if(!Tilesets.ContainsKey(name))
                    Tilesets.Add(name, new Tileset(32, dataLoader.get(name)));
                return Tilesets[name];
            } }

        public static Tileset Grass { get {
                string name = "tiles_grass_16_64";
                if(!Tilesets.ContainsKey(name))
                    Tilesets.Add(name, new Tileset(16, dataLoader.get(name)));

                return Tilesets[name];
            } }
    }
}
