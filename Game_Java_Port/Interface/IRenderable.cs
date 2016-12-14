using SharpDX.Direct2D1;
using SharpDX;

namespace Game_Java_Port.Interface {
    public interface IRenderable {

        RectangleF Area { get; set; }

        void draw(RenderTarget rt);

    }
}
