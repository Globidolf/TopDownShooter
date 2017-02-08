using Game_Java_Port.Interface;
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

        private int seed;

		public override void updateRenderData() {
			
			int width = dataLoader.D3DResources[RenderData.ResID].Description.Width,
				height = dataLoader.D3DResources[RenderData.ResID].Description.Height,
				countX = (Program.width / width + 1),
				countY = (Program.height / height + 1);
			for (int i = 0 ; i < RenderData.SubObjs.Length ; i++) {
				int x = i % countX, y = (i / countX) % countY;
				RenderData.SubObjs[i].Area = new Rectangle(
					CustomMaths.mod(x * width + (int) MatrixExtensions.PVTranslation.X, Program.width + width) - width,
					CustomMaths.mod(y * height + (int) MatrixExtensions.PVTranslation.Y, Program.height + height) - height,
					width, height);
			}
		}



		private static List<Background_Tiled> backgrounds = new List<Background_Tiled>();

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

        private void populate() {
			int width = dataLoader.D3DResources[RenderData.ResID].Description.Width,
				height = dataLoader.D3DResources[RenderData.ResID].Description.Height,
				countX = (Program.width / width + 1),
				countY = (Program.height / height + 1);
			RenderData.SubObjs = new RenderData[countX * countY];
            Random RNG = new Random(seed);
			for (int i = 0 ; i < RenderData.SubObjs.Length ; i++) {
				int x = i % countX, y = (i / countX) % countY;
				RenderData.SubObjs[i] = new RenderData
				{
					AnimationFrameCount = RenderData.AnimationFrameCount,
					mdl = Model.Square,
					ResID = RenderData.ResID,
				};
				RenderData.SubObjs[i].Area = new Rectangle(
					CustomMaths.mod(x * width + (int) MatrixExtensions.PVTranslation.X, Program.width + width) - width,
					CustomMaths.mod(y * height + (int) MatrixExtensions.PVTranslation.Y, Program.height + height) - height,
					width, height);
				RenderData.SubObjs[i].mdl.VertexBuffer
					.SetAnimationFrame(
						RNG.Next(
							RenderData.AnimationFrameCount.X *
							RenderData.AnimationFrameCount.Y),
						RenderData.AnimationFrameCount);
				RenderData.SubObjs[i].mdl.VertexBuffer.ApplyZAxis(RenderData.mdl.VertexBuffer[0].Pos.Z);
			}
        }
        public Background_Tiled(
            int ResID,
			Point Tiles,
            int? Seed = null,
            RectangleF? Area = null,
            float lifetime = 0,
            Settings settings = Settings.Default,
            bool add = true)
            :base(ResID, Area.HasValue ? Area.Value : RectangleF.Empty, lifetime, settings, add) {

            RenderData.Area = Area.HasValue ?
				new Rectangle((int) Area.Value.X, (int) Area.Value.Y, (int) Area.Value.Width, (int) Area.Value.Height) :
				new Rectangle(0,0,dataLoader.D3DResources[ResID].Description.Width, dataLoader.D3DResources[ResID].Description.Height);

			RenderData.AnimationFrameCount = Tiles;
            seed = Seed.HasValue ? Seed.Value : new Random().Next();
			// Remove old indices
			if (add) 
				((IRenderable) this).unregister();
			// populate children
            populate();
			// add new indices
			if (add) 
				((IRenderable) this).register();
				
			// do not render this specific instance, only the children
			RenderData.mdl.VertexBuffer.ApplyColor(Color.Transparent);
        }
    }
}
