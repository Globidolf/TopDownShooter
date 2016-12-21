using SharpDX.Direct2D1;
using SharpDX;

namespace Game_Java_Port.Interface {

    public enum DrawType {
        None,
        Circle,
        Rectangle,
        Polygon,
        Image
    }

    public interface IRenderable {
        
        DrawType drawType { get; set; }

        RectangleF Area { get; set; }

        void draw(RenderTarget rt);

        int Z { get; set; }
    }
}
