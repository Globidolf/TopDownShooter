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
        Normal,
        Inventory_Equip,
        Inventory_Remove,
        Inventory_Add,
        Inventory_Use,
        Interact
    }

    public class CustomCursor : IRenderable {
		

		public void updateRenderData() {
			if (invalidate) {
				RenderData.mdl.VertexBuffer.ApplyRectangle(new RectangleF(MousePos.X, MousePos.Y, _Size, _Size));
				for (int i = 0 ; i < RenderData.SubObjs[0].SubObjs.Length ; i++)
					RenderData.SubObjs[0].SubObjs[i].mdl.VertexBuffer.ApplyRectangle(new RectangleF(MousePos.X + 6 * (i + 3), MousePos.Y, 6, 8));
				invalidate = false;
			}
		}

        private float _Size;

        public CursorTypes CursorType { set {
				RenderData.ResID = Cursors[value];
			}
		}

		public bool invalidate = false;

        public RenderData RenderData { get; set; }

        static Dictionary<CursorTypes, int> Cursors = new Dictionary<CursorTypes, int>()
        {   { CursorTypes.Normal,           dataLoader.getResID("cursor")      },
            { CursorTypes.Inventory_Equip,  dataLoader.getResID("cursor_invE") },
            { CursorTypes.Inventory_Add,    dataLoader.getResID("cursor_invA") },
            { CursorTypes.Inventory_Remove, dataLoader.getResID("cursor_invR") },
            { CursorTypes.Inventory_Use,    dataLoader.getResID("cursor_invU") },
            { CursorTypes.Interact,         dataLoader.getResID("cursor_U")    }
        };

        public CustomCursor(CursorTypes cursor, float Size = 8) {
            _Size = Size;
			RenderData = new RenderData
			{
				mdl = Model.Square,
				ResID = Cursors[cursor],
				Z = Renderer.Layer_Cursor,
				SubObjs = new[]
				{
					SpriteFont.DEFAULT.generateText("TEST nachricht ablablablabsdfsa jfwoie jfwaiesjf alheijhf soikjfnlrgnedngksdjrngsdlkjngslkdjngslker",Z: 100)
				}
			};
            RenderData.mdl.VertexBuffer.ApplyRectangle(new RectangleF(MousePos.X, MousePos.Y, _Size, _Size));
        }
    }
}
