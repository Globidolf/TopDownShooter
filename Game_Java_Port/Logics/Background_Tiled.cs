using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    class Background_Tiled : Background {

        private bool disposed = false;

        public Tileset Tiles;

        private List<TileArea> buffer = new List<TileArea>();

        private class TileArea {
            public int X;
            public int Y;
            public Bitmap Tile;
            public RectangleF Area;
            public TileArea(Point pos, Bitmap tile, RectangleF? area = null) {
                X = pos.X;
                Y = pos.Y;
                Area = area.HasValue ? area.Value : RectangleF.Empty;
                Tile = tile;
            }

        }

        public Background_Tiled(
            Tileset Tiles,
            int? Seed = null,
            RectangleF? Area = null,
            float lifetime = 0,
            Settings settings = Settings.Default,
            bool add = true)
            :base(Tiles.Source, Area.HasValue ? Area.Value.TopLeft : Vector2.Zero, lifetime, settings, add) {
            this.Area = Area.HasValue ? Area.Value : this.Area;
            this.Tiles = Tiles;
            Random RNG = Seed.HasValue ? new Random(Seed.Value) : new Random();
                for(int y = 0; (y - 1) * Tiles.TileSize.Height < this.Area.Height; y++) 
                    for(int x = 0; (x - 1) * Tiles.TileSize.Width < this.Area.Width; x++) 
                        buffer.Add(new TileArea(new Point(x, y), Tiles.getRandomTile(RNG)));
        }

        public override void draw(RenderTarget rt) {
            foreach(TileArea tile in buffer) {
                
                rt.DrawBitmap(tile.Tile, tile.Area, 1, BitmapInterpolationMode.Linear);
            }
        }

        public override void Tick() {
            //base.Tick();
            List<TileArea> backbuffer = new List<TileArea>();
                buffer.ForEach(t =>
                {
                    RectangleF bounds = new RectangleF(
                        Area.X + t.X * Tiles.TileSize.Width,
                        Area.Y + t.Y * Tiles.TileSize.Height,
                        Tiles.TileSize.Width, Tiles.TileSize.Height);
                    switch(settings) {
                        case Settings.Parallax | Settings.Fill_Area:
                            bounds.X = CustomMaths.mod(bounds.X + MatrixExtensions.PVTranslation.X, Area.Width + Tiles.TileSize.Width) - Tiles.TileSize.Width;
                            bounds.Y = CustomMaths.mod(bounds.Y + MatrixExtensions.PVTranslation.Y, Area.Height + Tiles.TileSize.Height) - Tiles.TileSize.Height;
                            break;
                        case Settings.Parallax:
                            bounds.Location += MatrixExtensions.PVTranslation;
                            break;
                    }
                    bounds.X = (float)Math.Floor(bounds.X);
                    bounds.Y = (float)Math.Floor(bounds.Y);
                    backbuffer.Add(new TileArea(
                        new Point(t.X, t.Y), t.Tile,
                        bounds));
                });
                buffer = backbuffer;
        }

        protected override void Dispose(bool disposing) {
            if(disposed)
                return;
            if(disposing)
                buffer.Clear();
            disposed = true;
            base.Dispose(disposing);
        }
    }
}
