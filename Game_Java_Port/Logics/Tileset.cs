using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    /*
    public class Tileset  {

        //public Bitmap Source;
        //public readonly Size2 TileSize;
        //private Bitmap[] _Tiles;

        private Tileset _Flat;

        public bool IsFlatAvailable { get { return _Flat != null; } }

        public Tileset Flat { get { return _Flat; } }

        private static Dictionary<string, Tileset> Tilesets = new Dictionary<string, Tileset>();

        private Tileset(int TileSize, string SourceName) : this(TileSize, TileSize, dataLoader.get2D(SourceName)) {
            Bitmap flat = dataLoader.get2D(SourceName + "_flat");

            if(flat != null) {
                _Flat = new Tileset(TileSize, TileSize, flat);
            }
        }

        private Tileset(int TileWidth, int TileHeight, string SourceName) : this(TileWidth, TileHeight, dataLoader.get2D(SourceName)) {

            Bitmap flat = dataLoader.get2D(SourceName + "_flat");

            if(flat != null) {
                _Flat = new Tileset(TileWidth, TileHeight, flat);
            }
        }

        private Tileset(int TileWidth, int TileHeight, Bitmap Source) {

            TileSize = new Size2(TileWidth, TileHeight);
            this.Source = Source;
            int cols = Source.PixelSize.Width / TileSize.Width;
            int rows = Source.PixelSize.Height / TileSize.Height;
            //_Tiles = new Bitmap[cols * rows];

            for(int y = 0; y < rows; y++) {
                for(int x = 0; x < cols; x++) {
                    _Tiles[x + y * cols] = new Bitmap(Program.D2DContext, TileSize, new BitmapProperties(new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)));
                    _Tiles[x + y * cols].CopyFromBitmap(Source, Point.Zero, new Rectangle(x * TileSize.Width, y * TileSize.Height, TileSize.Width, TileSize.Height));
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
        *//*
        //implemented
        public static Tileset Frame_Pearl { get { return getTileset("tiled_menu_pearl2_32_96", 32); } }
        public static Tileset Frame_Legend { get { return getTileset("tiled_menu_legend_32_96", 32); } }
        public static Tileset Frame_Epic { get { return getTileset("tiled_menu_epic_32_96", 32); } }
        public static Tileset Frame_Rare { get { return getTileset("tiled_menu_rare_32_96", 32); } }
        public static Tileset Frame_Common { get { return getTileset("tiled_menu_common_32_96", 32); } }
        /*
        public static Tileset Frame_Uncommon { get { return getTileset("tiled_menu_unc_32_96", 32); } }
        public static Tileset Frame_Garbage { get { return getTileset("tiled_menu_garb_32_96", 32); } }
        *//*

        public static Tileset Frame_Default { get { return getTileset("tiled_menu_default_32_96", 32); } }

        public static Tileset Font_Default { get { return getTileset("font_default_6_8", 6, 8); } }

        public static Tileset Anim_Bullet_Acid { get { return getTileset("bullet_acid_16_32", 16, 16); } }

        public static Tileset Anim_Bullet_Rock { get { return getTileset("bullet_rock_16_32", 16, 16); } }


        private static Tileset getTileset(string name, int resX, int resY) {
            if(!Tilesets.ContainsKey(name))
                Tilesets.Add(name, new Tileset(resX, resY, name));
            return Tilesets[name];
        }
        private static Tileset getTileset(string name, int res) {

            if(!Tilesets.ContainsKey(name))
                Tilesets.Add(name, new Tileset(res, name));

            return Tilesets[name];
        }

        public static void Clear() {
            foreach(Tileset set in Tilesets.Values) {
                if(set.IsFlatAvailable && set._Flat != null)
                    foreach(Bitmap bmp in set._Flat._Tiles)
                        bmp.Dispose();
                foreach(Bitmap bmp in set._Tiles)
                    bmp.Dispose();
                set.Source.Dispose();
            }
        }

        public static void Regenerate() {
            foreach(KeyValuePair<string, Tileset> pair in Tilesets) {
                Tileset set = pair.Value;
                set.Source = dataLoader.get2D(pair.Key);
                int cols = set.Source.PixelSize.Width / set.TileSize.Width;
                int rows = set.Source.PixelSize.Height / set.TileSize.Height;
                set._Tiles = new Bitmap[cols * rows];
                for(int y = 0; y < rows; y++) {
                    for(int x = 0; x < cols; x++) {
                        set._Tiles[x + y * cols] = new Bitmap(Program.D2DContext, set.TileSize, new BitmapProperties(new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)));
                        set._Tiles[x + y * cols].CopyFromBitmap(set.Source, Point.Zero, new Rectangle(x * set.TileSize.Width, y * set.TileSize.Height, set.TileSize.Width, set.TileSize.Height));
                    }
                }
                if(set.IsFlatAvailable && set._Flat != null) {

                    set._Flat.Source = dataLoader.get2D(pair.Key + "_flat");

                    int fl_cols = set._Flat.Source.PixelSize.Width / set._Flat.TileSize.Width;
                    int fl_rows = set._Flat.Source.PixelSize.Height / set._Flat.TileSize.Height;
                    set._Flat._Tiles = new Bitmap[fl_cols * fl_rows];
                    for(int y = 0; y < fl_rows; y++) {
                        for(int x = 0; x < cols; x++) {
                            set._Flat._Tiles[x + y * fl_cols] = new Bitmap(Program.D2DContext, set._Flat.TileSize, new BitmapProperties(new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)));
                            set._Flat._Tiles[x + y * fl_cols].CopyFromBitmap(set._Flat.Source, Point.Zero, new Rectangle(x * set._Flat.TileSize.Width, y * set._Flat.TileSize.Height, set._Flat.TileSize.Width, set._Flat.TileSize.Height));
                        }
                    }
                }
            }
        }
    }
    */
}
