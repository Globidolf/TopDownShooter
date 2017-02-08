using Game_Java_Port.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;

using static Game_Java_Port.GameStatus;
using Game_Java_Port.Logics;

namespace Game_Java_Port {

    public enum CursorTypes {
        Normal = 0,
        Inventory_Equip = 6,
        Inventory_Remove = 7,
        Inventory_Add = 5,
        Inventory_Use = 4,
        Exclamation = 1,
		Question = 2
    }

    public class CustomCursor : IRenderable {
		

		public void updateRenderData() {
			if (invalidate) {
				RenderData.mdl.VertexBuffer.ApplyRectangle(new RectangleF(MousePos.X, MousePos.Y, _Size, _Size));
				/*
				for (int i = 0 ; i < RenderData.SubObjs[0].SubObjs.Length ; i++)
					RenderData.SubObjs[0].SubObjs[i].mdl.VertexBuffer.ApplyRectangle(new RectangleF(MousePos.X + 6 * (i + 3), MousePos.Y, 6, 8));
					*/
				invalidate = false;
			}
		}

        private float _Size;

        public CursorTypes CursorType { set {
				RenderData.mdl.VertexBuffer.SetAnimationFrame((int)value, RenderData.AnimationFrameCount);
			}
		}

		public bool invalidate = false;

        public RenderData RenderData { get; set; }

        public CustomCursor(CursorTypes cursor, float Size = 8) {
            _Size = Size;
			RenderData = new RenderData
			{
				mdl = Model.Square,
				ResID = -1,
				AnimationFrameCount = new Point(4,4),
				Z = Renderer.Layer_Cursor,
				SubObjs = new[]
				{
					SpriteFont.DEFAULT.generateText("TEST nachricht",Z: 100)
				}
			};
            RenderData.mdl.VertexBuffer.ApplyRectangle(new RectangleF(MousePos.X, MousePos.Y, _Size, _Size));
        }
    }
}
