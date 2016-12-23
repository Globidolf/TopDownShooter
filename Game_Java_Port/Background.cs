using Game_Java_Port.Interface;
using System;
using System.IO;

using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using SharpDX;

namespace Game_Java_Port {
    public class Background : IRenderable, ITickable, IDisposable {

        private float lifetime = 0;

        public event EventHandler TickAction;

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
            Fill_Area = 0x01,
            /// <summary>
            /// BG is not bound to screen (Relative)
            /// </summary>
            Parallax = 0x01 << 1,

            Decay = 0x01 << 2,

            Foreground = 0x01 << 3,

            Tileset = 0x10
        }

        public Settings settings = Settings.Default;


        public ExtendMode ExtendX { get { return tb.ExtendModeX; } set { tb.ExtendModeX = value; } }
        public ExtendMode ExtendY { get { return tb.ExtendModeY; } set { tb.ExtendModeY = value; } }

        Bitmap img;
        protected Matrix3x2 transform;
        protected RectangleF offset;

        private BitmapBrush _tb;
        BitmapBrush tb {
            get {
                if(_tb == null) {
                    lock(this)
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

        public Background(Bitmap bmp, RawVector2? location = null, float lifetime = 0, Settings settings = Settings.Default, bool add = true) {
            img = bmp;
            Factory test = new Factory();
            if(location == null)
                location = new RawVector2();

            Area = new RectangleF(location.Value.X - bmp.Size.Width / 2, location.Value.Y - bmp.Size.Height / 2, bmp.Size.Width, bmp.Size.Height);
            this.settings = settings;
            if(lifetime > 0) {
                this.lifetime = lifetime;
                this.settings |= Settings.Decay;
            }
            Z = -5;
            if(add)
                addToGame();
        }



        public virtual void draw(RenderTarget rt) {
            lock(this)
                if(!disposed) {
                    if(settings.HasFlag(Settings.Fill_Area)) {
                        if(_tb != null) {
                            rt.FillRectangle(Game.instance.Area, tb);
                        }
                    } else {
                        rt.DrawBitmap(img, offset, 1, BitmapInterpolationMode.Linear, new RectangleF(0, 0, img.Size.Width, img.Size.Height));
                    }
                }
        }

        public virtual void Tick() {
            TickAction?.Invoke(this, EventArgs.Empty);
            lock(this) if(_tb != null && !disposed) {
                    transform = Matrix3x2.Identity;
                    transform.TranslationVector += MatrixExtensions.PVTranslation;
                    tb.Transform = transform;
                }

            offset = Area;
            offset.Offset(MatrixExtensions.PVTranslation);
            if(settings.HasFlag(Settings.Decay)) {
                lifetime -= 1 / GameVars.defaultGTPS;
                if(lifetime <= 0) lock(this)
                    Dispose();
            }
        }

        public void addToGame() {
            GameStatus.addTickable(this);
            GameStatus.addRenderable(this);
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    GameStatus.removeTickable(this);
                    GameStatus.removeRenderable(this);
                    if(_tb != null)
                         _tb.Dispose();
                    //DONT dispose the bitmap, set it to null instead. it is a shared resource and img is merely the reference
                    img = null;
                    TickAction = null;
                }
                disposed = true;
            }
        }
        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
