using Game_Java_Port.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;

namespace Game_Java_Port {
    public class Menu_BG_Tiled : IRenderable, ITickable {

		private const string _pre = "tiled_menu_";
		private const string _post = "_32_96";

        public static Menu_BG_Tiled Default { get { return new Menu_BG_Tiled(dataLoader.getResID(_pre + "default" + _post)); } }
        public static Menu_BG_Tiled Pearlescent { get { return new Menu_BG_Tiled(dataLoader.getResID(_pre + "pearl" + _post)); } }
        public static Menu_BG_Tiled Legendary { get { return new Menu_BG_Tiled(dataLoader.getResID(_pre + "legend" + _post)); } }
        public static Menu_BG_Tiled Epic { get { return new Menu_BG_Tiled(dataLoader.getResID(_pre + "epic" + _post)); } }
        public static Menu_BG_Tiled Rare { get { return new Menu_BG_Tiled(dataLoader.getResID(_pre + "rare" + _post)); } }
        public static Menu_BG_Tiled Common { get { return new Menu_BG_Tiled(dataLoader.getResID(_pre + "common" + _post)); } }
        private static List<Menu_BG_Tiled> menubgs = new List<Menu_BG_Tiled>();
		
		public RenderData RenderData { get; set; }

        public bool scaleUp = false;
        
        private bool flat = false;

        //private Tileset Tile;

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
		
        public int Z { get; set; } = 1000;


        /* 
        [0][1][2] 
        [3][4][5]
        [6][7][8]
        */
		
        /*
        public BitmapBrush TopBrush;
        public BitmapBrush BottomBrush;
        public BitmapBrush LeftBrush;
        public BitmapBrush RightBrush;
        public BitmapBrush BGBrush;
        public BitmapBrush FlatBrush;
        */
        private Menu_BG_Tiled(int ResID) {

			RenderData = new RenderData
			{
				SubObjs = new[]
				{
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
					new RenderData { mdl = Model.Square, ResID = ResID, AnimationFrameCount = new Point(3,3) },
				}
			};
            
            BitmapBrushProperties bbp = new BitmapBrushProperties() {
                ExtendModeX = ExtendMode.Wrap,
                ExtendModeY = ExtendMode.Wrap,
                InterpolationMode = BitmapInterpolationMode.Linear
            };

            menubgs.Add(this);
        }
		
        public void draw(DeviceContext rt) {
			/*
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
				*/
        }
        
        public void Tick() {
            RectangleF temptop, templeft, tempbottom, tempright, tl, tr, bl, br, temp;
            
            temp = Area;
            // fix size and center location
			/*
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

			*/
            Matrix3x2 transform = Matrix3x2.Identity;
            transform.TranslationVector = Area.Floor().TopLeft;


            if(BGArea.Height <= 0)
                flat = true;
        }
		
    }
}
