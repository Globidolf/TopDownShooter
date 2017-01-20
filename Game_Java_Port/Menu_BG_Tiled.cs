using Game_Java_Port.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;

namespace Game_Java_Port {
    public class Menu_BG_Tiled : IRenderable, IDisposable, ITickable {
        
        public static Menu_BG_Tiled Default { get { return new Menu_BG_Tiled(Tileset.Frame_Default); } }
        public static Menu_BG_Tiled Pearlescent { get { return new Menu_BG_Tiled(Tileset.Frame_Pearl); } }
        public static Menu_BG_Tiled Legendary { get { return new Menu_BG_Tiled(Tileset.Frame_Legend); } }
        public static Menu_BG_Tiled Epic { get { return new Menu_BG_Tiled(Tileset.Frame_Epic); } }
        public static Menu_BG_Tiled Rare { get { return new Menu_BG_Tiled(Tileset.Frame_Rare); } }
        public static Menu_BG_Tiled Common { get { return new Menu_BG_Tiled(Tileset.Frame_Common); } }

        private static List<Menu_BG_Tiled> menubgs = new List<Menu_BG_Tiled>();


        public bool scaleUp = false;
        
        private bool flat = false;

        private Tileset Tile;

        public RectangleF Area { get; set; }
        private RectangleF BGArea { get; set; }
        private RectangleF TopArea { get; set; }
        private RectangleF BottomArea { get; set; }
        private RectangleF LeftArea { get; set; }
        private RectangleF RightArea { get; set; }

        private RectangleF TL { get; set; }
        private RectangleF TR { get; set; }
        private RectangleF BL { get; set; }
        private RectangleF BR { get; set; }

        public DrawType drawType { get; set; } = DrawType.Image;

        public int Z { get; set; } = 1000;


        /* 
        [0][1][2] 
        [3][4][5]
        [6][7][8]
        */

        public Bitmap TopLeft {
            get {
                return Tile.Tiles[0];
            }
        }
        public Bitmap Top {
            get {
                return Tile.Tiles[1];
            }
        }
        public Bitmap TopRight {
            get {
                return Tile.Tiles[2];
            }
        }
        public Bitmap Left {
            get {
                return Tile.Tiles[3];
            }
        }
        public Bitmap BG {
            get {
                return Tile.Tiles[4];
            }
        }
        public Bitmap Right {
            get {
                return Tile.Tiles[5];
            }
        }
        public Bitmap BottomLeft {
            get {
                return Tile.Tiles[6];
            }
        }
        public Bitmap Bottom {
            get {
                return Tile.Tiles[7];
            }
        }
        public Bitmap BottomRight {
            get {
                return Tile.Tiles[8];
            }
        }
        public Bitmap FlatLeft {
            get {
                return Tile.IsFlatAvailable ? Tile.Flat.Tiles[0] : Left;
            }
        }
        public Bitmap FlatRight {
            get {
                return Tile.IsFlatAvailable ? Tile.Flat.Tiles[2] : Right;
            }
        }
        public Bitmap FlatBG {
            get {
                return Tile.IsFlatAvailable ? Tile.Flat.Tiles[1] : BG;
            }
        }


        public BitmapBrush TopBrush;
        public BitmapBrush BottomBrush;
        public BitmapBrush LeftBrush;
        public BitmapBrush RightBrush;
        public BitmapBrush BGBrush;
        public BitmapBrush FlatBrush;
        
        private Menu_BG_Tiled(Tileset tiles) {
            if(tiles.Tiles.Length != 9)
                throw new ArgumentException("The Tileset was not designed to be used for a menu. It requires 3x3 - or 9 - tiles. " + tiles.Tiles.Length + " were given...", "tiles");
            Tile = tiles;
            
            BitmapBrushProperties bbp = new BitmapBrushProperties() {
                ExtendModeX = ExtendMode.Wrap,
                ExtendModeY = ExtendMode.Wrap,
                InterpolationMode = BitmapInterpolationMode.Linear
            };

            BGBrush = new BitmapBrush(Program._RenderTarget, BG, bbp);
            TopBrush = new BitmapBrush(Program._RenderTarget, Top, bbp);
            LeftBrush = new BitmapBrush(Program._RenderTarget, Left, bbp);
            BottomBrush = new BitmapBrush(Program._RenderTarget, Bottom, bbp);
            RightBrush = new BitmapBrush(Program._RenderTarget, Right, bbp);
            FlatBrush = new BitmapBrush(Program._RenderTarget, FlatBG, bbp);
            menubgs.Add(this);
        }

        public static void Regenerate() {

            BitmapBrushProperties bbp = new BitmapBrushProperties() {
                ExtendModeX = ExtendMode.Wrap,
                ExtendModeY = ExtendMode.Wrap,
                InterpolationMode = BitmapInterpolationMode.Linear
            };
            foreach(Menu_BG_Tiled menubg in menubgs) {
                menubg.BGBrush    .Dispose();
                menubg.TopBrush   .Dispose();
                menubg.LeftBrush  .Dispose();
                menubg.BottomBrush.Dispose();
                menubg.RightBrush .Dispose();
                menubg.FlatBrush.Dispose();


                menubg.BGBrush      = new BitmapBrush(Program._RenderTarget,     menubg.BG,      bbp);
                menubg.TopBrush     = new BitmapBrush(Program._RenderTarget,    menubg.Top,     bbp);
                menubg.LeftBrush    = new BitmapBrush(Program._RenderTarget,   menubg.Left,    bbp);
                menubg.BottomBrush  = new BitmapBrush(Program._RenderTarget, menubg.Bottom,  bbp);
                menubg.RightBrush   = new BitmapBrush(Program._RenderTarget,  menubg.Right,   bbp);
                menubg.FlatBrush    = new BitmapBrush(Program._RenderTarget,   menubg.FlatBG,  bbp);
            }
        }


        public void draw(DeviceContext rt) {
                if(!disposed) {
                    if(!flat) {
                        rt.FillRectangle(BGArea, BGBrush);
                        rt.FillRectangle(TopArea, TopBrush);
                        rt.FillRectangle(LeftArea, LeftBrush);
                        rt.FillRectangle(RightArea, RightBrush);
                        rt.FillRectangle(BottomArea, BottomBrush);
                        rt.DrawBitmap(TopLeft, TL, 1, BitmapInterpolationMode.Linear);
                        rt.DrawBitmap(TopRight, TR, 1, BitmapInterpolationMode.Linear);
                        rt.DrawBitmap(BottomLeft, BL, 1, BitmapInterpolationMode.Linear);
                        rt.DrawBitmap(BottomRight, BR, 1, BitmapInterpolationMode.Linear);
                    } else {
                        rt.FillRectangle(TopArea, FlatBrush);
                        rt.DrawBitmap(FlatLeft, TL, 1, BitmapInterpolationMode.Linear);
                        rt.DrawBitmap(FlatRight, TR, 1, BitmapInterpolationMode.Linear);
                    }
                }
        }
        
        public void Tick() {
            RectangleF temptop, templeft, tempbottom, tempright, tl, tr, bl, br, temp;
            
            temp = Area;
            // fix size and center location
            if((temp.Width % Tile.TileSize.Width) + (temp.Height % Tile.TileSize.Height) != 0) {
                Vector2 offset = Vector2.Zero;
                if(scaleUp) {
                    offset.X = (int)Math.Ceiling(temp.Width / Tile.TileSize.Width + 1) * Tile.TileSize.Width - temp.Width;
                    offset.Y = (int)Math.Ceiling(temp.Height / Tile.TileSize.Height + 1) * Tile.TileSize.Height - temp.Height;
                } else {
                    offset.X = ((int)temp.Width / Tile.TileSize.Width + 1) * Tile.TileSize.Width - temp.Width;
                    offset.Y = ((int)temp.Height / Tile.TileSize.Height + 1) * Tile.TileSize.Height - temp.Height;
                }
                temp.Width += offset.X;
                temp.Height += offset.Y;
                temp.Offset( -offset / 2);
                temp.X = (int)temp.X;
                temp.Y = (int)temp.Y;
                Area = temp;
            }

            tl = tr = bl = br = new RectangleF(Area.X, Area.Y, Tile.TileSize.Width, Tile.TileSize.Height);

            temp.Width -= Tile.TileSize.Width * 2;
            temp.Height -= Tile.TileSize.Height * 2;

            temptop = templeft = tempbottom = tempright = temp;

            temp.Offset(Tile.TileSize.Width, Tile.TileSize.Height);

            templeft.Width = tempright.Width = temptop.Height = tempbottom.Height = Tile.TileSize.Height;

            templeft.Offset(0, Tile.TileSize.Height);
            tempright.Offset(temp.Width + Tile.TileSize.Width, Tile.TileSize.Height);
            tempbottom.Offset(Tile.TileSize.Width, temp.Height + Tile.TileSize.Height);
            temptop.Offset(Tile.TileSize.Width, 0);
            
            br.X = tr.X = tempright.X;
            br.Y = bl.Y = tempbottom.Y;

            BGArea = temp.Floor();
            TopArea = temptop.Floor();
            LeftArea = templeft.Floor();
            BottomArea = tempbottom.Floor();
            RightArea = tempright.Floor();

            TL = tl.Floor();
            TR = tr.Floor();
            BL = bl.Floor();
            BR = br.Floor();

            Matrix3x2 transform = Matrix3x2.Identity;
            transform.TranslationVector = Area.Floor().TopLeft;

            FlatBrush.Transform = BottomBrush.Transform = LeftBrush.Transform = RightBrush.Transform = TopBrush.Transform = BGBrush.Transform = transform;
            if(BGArea.Height <= 0)
                flat = true;
        }

        #region IDisposable Support
        private bool disposed = false;

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    BGBrush.Dispose();
                    TopBrush.Dispose();
                    LeftBrush.Dispose();
                    RightBrush.Dispose();
                    BottomBrush.Dispose();
                    FlatBrush.Dispose();
                }

                menubgs.Remove(this);

                disposed = true;
            }
        }
        public void Dispose() {
                Dispose(true);
        }
        #endregion
    }
}
