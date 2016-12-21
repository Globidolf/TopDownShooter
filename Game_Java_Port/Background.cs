using Game_Java_Port.Interface;
using System;
using System.IO;

using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using SharpDX;

namespace Game_Java_Port {
    public class Background : IRenderable, ITickable {

        private float lifetime = 0;

        public event Action TickAction;
        
        public RectangleF Area { get; set; }

        [Flags]
        public enum Settings : byte {
            /// <summary>
            /// BG is single object and bound to screen (Absolute)
            /// </summary>
            Default = 0x00,
            /// <summary>
            /// BG fills complete screen
            /// </summary>
            Fill_Screen = 0x01,
            /// <summary>
            /// BG is not bound to screen (Relative)
            /// </summary>
            Parallax = 0x01 << 1,

            Decay = 0x01 << 2,

            Foreground = 0x01 << 3,
        }

        public Settings settings = Settings.Default;

        
        public ExtendMode ExtendX { get { return tb.ExtendModeX; } set { tb.ExtendModeX = value; } }
        public ExtendMode ExtendY { get { return tb.ExtendModeY; } set { tb.ExtendModeY = value; } }
        
        Bitmap img;
        private Matrix3x2 transform;
        private RectangleF offset;

        private BitmapBrush _tb;
        BitmapBrush tb {
            get {
                if(_tb == null) {
                    lock(img)
                        _tb = new BitmapBrush(Program._RenderTarget, img);
                }
                return _tb;
            }
        }

        private int _Z;

        public int Z {
            get {
                return settings.HasFlag(Settings.Foreground) ? _Z + 100 : _Z - 100;
            }

            set {
                _Z = value;
            }
        }

        public DrawType drawType { get; set; } = DrawType.Image;

        public Background(Bitmap bmp , RawVector2? location = null, float lifetime = 0, Settings settings = Settings.Default, bool add = true) {
            img = bmp;
            Factory test = new Factory();
            if(location == null)
                location = new RawVector2();

            Area = new RectangleF(location.Value.X - bmp.Size.Width / 2, location.Value.Y - bmp.Size.Height / 2, bmp.Size.Width, bmp.Size.Height);
            this.settings = settings;
            if (lifetime > 0) {
                this.lifetime = lifetime;
                this.settings |= Settings.Decay;
            }
            if (add)
                addToGame();
        }



        public void draw(RenderTarget rt) {
            if(settings.HasFlag(Settings.Fill_Screen)) {
                if (_tb != null) lock(_tb) if(!_tb.IsDisposed) {
                    rt.FillRectangle(Game.instance.Area, tb);
                }
            } else {
                rt.DrawBitmap(img, offset, 1, BitmapInterpolationMode.Linear, new RectangleF(0,0,img.Size.Width, img.Size.Height));
            }
        }

        public void Tick() {
            TickAction?.Invoke();
            if (_tb != null) lock(_tb) if (!_tb.IsDisposed){
                    transform = Matrix3x2.Identity;
                    transform.TranslationVector += MatrixExtensions.PVTranslation;
                    tb.Transform = transform;
                }
            offset = Area;
            offset.Offset(MatrixExtensions.PVTranslation);
            if(settings.HasFlag(Settings.Decay)) {
                lifetime -= 1 / GameVars.defaultGTPS;
                if(lifetime <= 0)
                    removeFromGame();
            }
        }

        public void addToGame() {
            GameStatus.addTickable(this);
            GameStatus.addRenderable(this);
        }

        public void removeFromGame() {
            GameStatus.removeTickable(this);
            GameStatus.removeRenderable(this);
            if (_tb != null)
                lock (_tb)
                    _tb.Dispose();
        }
    }
}
