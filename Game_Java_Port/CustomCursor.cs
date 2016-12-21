using Game_Java_Port.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;

using static Game_Java_Port.GameStatus;

namespace Game_Java_Port {

    public enum CursorTypes {
        Normal,
        Inventory_Equip,
        Inventory_Remove,
        Inventory_Add,
        Inventory_Use,
        Interact
    }

    public class CustomCursor : IRenderable, ITickable {
        public float Size { get; set; }

        public RectangleF Area { get; set; }

        private CursorTypes CurrentCursorType = CursorTypes.Normal;

        public DrawType drawType { get; set; } = DrawType.Image;

        public int Z { get; set; } = 100000;

        public CursorTypes CursorType { get; set; } = CursorTypes.Normal;

        static Dictionary<CursorTypes, Bitmap> Cursors = new Dictionary<CursorTypes, Bitmap>()
        {   { CursorTypes.Normal,           dataLoader.get("cursor")      },
            { CursorTypes.Inventory_Equip,  dataLoader.get("cursor_invE") },
            { CursorTypes.Inventory_Add,    dataLoader.get("cursor_invA") },
            { CursorTypes.Inventory_Remove, dataLoader.get("cursor_invR") },
            { CursorTypes.Inventory_Use,    dataLoader.get("cursor_invU") },
            { CursorTypes.Interact,         dataLoader.get("cursor_U")    }
        };

        public CustomCursor(CursorTypes cursor, float Size = 8) {
            this.Size = Size;
            Area = new RectangleF(MousePos.X, MousePos.Y, Size, Size);
            CursorType = cursor;
        }

        public void draw(RenderTarget rt) {
            rt.DrawBitmap(Cursors[CurrentCursorType], Area, 1, BitmapInterpolationMode.Linear);
        }

        public void Tick() {
            RectangleF temp = Area;
            temp.Location = MousePos;
            Area = temp;
        }

        public void Apply() {
            CurrentCursorType = CursorType;
        }
    }
}
