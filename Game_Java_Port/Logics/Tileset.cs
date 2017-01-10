using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    
    public class Tileset {
        
        public readonly Bitmap Source;
        public readonly int TileSize;
        private Bitmap[] _Tiles;

        private Tileset _Flat;

        public bool IsFlatAvailable { get { return _Flat != null; } }

        public Tileset Flat { get { return _Flat; } }

        private static Dictionary<string, Tileset> Tilesets = new Dictionary<string, Tileset>();
        

        private Tileset(int TileSize, string SourceName) : this(TileSize, dataLoader.get(SourceName)){

            Bitmap flat = dataLoader.get(SourceName + "_flat");

            if(flat != null) {
                _Flat = new Tileset(TileSize, flat);
            }
        }

        private Tileset(int TileSize, Bitmap Source) {
            
            this.TileSize = TileSize;
            this.Source = Source;
            int cols = Source.PixelSize.Width / TileSize;
            int rows = Source.PixelSize.Height / TileSize;
            _Tiles = new Bitmap[cols * rows];
            for(int y = 0; y < rows; y++) {
                for (int x = 0; x < cols; x++) {
                    _Tiles[x + y * cols] = new Bitmap(Program._RenderTarget, new Size2(TileSize, TileSize), new BitmapProperties(new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)));
                    _Tiles[x + y * cols].CopyFromBitmap(Source, Point.Zero, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
                }
            }
        }
        
        public Bitmap[] Tiles { get { return _Tiles; } }

        public Bitmap getRandomTile(Random rng) {
            return _Tiles[rng.Next(_Tiles.Length)];
        }

        //implemented
        public static Tileset BG_Rock { get { return getTileset("tiles_rock_32_64", 32); } }
        public static Tileset BG_Grass { get { return getTileset("tiles_grass_16_64", 16); } }
        public static Tileset BG_Dark_Grass { get { return getTileset("tiles_grass_darker_16_64", 16); } }
        /*
        public static Tileset Frame_Set { get { return getTileset("tiled_menu_set_32_96", 32); } }
        public static Tileset Frame_Dev { get { return getTileset("tiled_menu_dev_32_96", 32); } }
        */
        //implemented
        public static Tileset Frame_Pearl { get { return getTileset("tiled_menu_pearl_32_96", 32); } }
        public static Tileset Frame_Legend { get { return getTileset("tiled_menu_legend_32_96", 32); } }
        public static Tileset Frame_Epic { get { return getTileset("tiled_menu_epic_32_96", 32); } }
        public static Tileset Frame_Rare { get { return getTileset("tiled_menu_rare_32_96", 32); } }
        /*
        public static Tileset Frame_Uncommon { get { return getTileset("tiled_menu_unc_32_96", 32); } }
        public static Tileset Frame_Common { get { return getTileset("tiled_menu_common_32_96", 32); } }
        public static Tileset Frame_Garbage { get { return getTileset("tiled_menu_garb_32_96", 32); } }
        */

        public static Tileset Frame_Default { get { return getTileset("tiled_menu_default_32_96", 32); } }
        private static Tileset getTileset(string name, int res) {

            if(!Tilesets.ContainsKey(name))
                Tilesets.Add(name, new Tileset(res, name));

            return Tilesets[name];
        }
    }
}
