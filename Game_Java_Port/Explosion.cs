using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    class Explosion : IRenderable, ITickable, IDisposable {
        float initialDuration;
        float Duration;
        
        SolidColorBrush brush;

        public RectangleF Area { get; set; }

        public int Z { get; set; } = 10;

        public DrawType drawType { get; set; } = DrawType.Circle;

        private Ellipse ellipse;

        public Explosion(RectangleF Area, float duration = 0.3f) {
            this.Area = Area;
            ellipse = new Ellipse(Area.Center + MatrixExtensions.PVTranslation, Area.Width / 2, Area.Height / 2);
            initialDuration = Duration = duration;
            Color temp = Color.Firebrick;
            temp.A = 0x88;
            brush = new SolidColorBrush(Program.D2DContext, temp);
            GameStatus.addRenderable(this);
            GameStatus.addTickable(this);
        }

        public void draw(DeviceContext rt) {
                if(!disposed)
                    rt.FillEllipse(ellipse, brush);
        }

        public void Tick() {
            ellipse = new Ellipse(Area.Center + MatrixExtensions.PVTranslation, Area.Width / 2, Area.Height / 2);
            if(Duration <= 0) {
                    Dispose();
            } else {
                Duration -= GameStatus.TimeMultiplier;
                    if (!disposed)
                        brush.Color = new Color4(brush.Color.R, brush.Color.G, brush.Color.B, (Duration / initialDuration));
            }
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    GameStatus.removeRenderable(this);
                    GameStatus.removeTickable(this);
                    brush.Dispose();
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
