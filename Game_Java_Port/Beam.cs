using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    class Beam : IRenderable, ITickable, IDisposable {
        float initialDuration;
        float Duration;
        
        private readonly SolidColorBrush InitialBrush;
        private SolidColorBrush Brush;

        private readonly List<Vector2> InitialStatics;
        private List<Vector2> Statics;

        private readonly float  InitialStrokeWidth;
        private float StrokeWidth;

        //unused
        public RectangleF Area { get; set; }

        public int Z { get; set; } = 10;

        public DrawType drawType { get; set; } = DrawType.Polygon;

        private Vector2[] Line;


        public Beam(Vector2 PointA, Vector2 PointB,
            //optional values:
            float duration = 0.3f, float strokewidth = 2f,
            bool electric = false, int lightningcount = 4,
            int seed = 0, Color beamColor = new Color() { A = 0x88, B = 255 })
        {
            InitialStrokeWidth = StrokeWidth = strokewidth;
            Line = new Vector2[4];
            Line[0] = PointA;
            Line[1] = PointB;
            //new Ellipse(Area.Center + MatrixExtensions.PVTranslation, Area.Width / 2, Area.Height / 2);
            initialDuration = Duration = duration;
            InitialBrush = Brush = new SolidColorBrush(Program._RenderTarget, beamColor);
            InitialStatics = new List<Vector2>();
            Statics = new List<Vector2>();

            if(electric) {
                Random _RNG = new Random(seed);
                float distance = Vector2.Distance(PointB, PointA);
                float offset = distance * (float)_RNG.NextDouble();
                float length = 1 + (float)_RNG.NextDouble() * (distance - offset) / 2;
                AngleSingle dir = PointA.angleTo(PointB);
                Vector2 split1 = (PointA).move(dir, offset);
                InitialStatics.Add(split1);
                while(offset < distance && length > (distance - offset) / 12 && lightningcount > 0) {
                    
                    offset += distance * (float)_RNG.NextDouble();
                    length /= 2;
                    dir.Radians += (float)(-Math.PI / 2 + _RNG.NextDouble() * Math.PI);


                    split1 = split1.move(dir, length);

                    InitialStatics.Add(split1);

                    lightningcount--;
                }
            }

            foreach(Vector2 point in InitialStatics) {
                Statics.Add(point + MatrixExtensions.PVTranslation);
            }
            GameStatus.addRenderable(this);
            GameStatus.addTickable(this);
        }

        public void draw(DeviceContext rt) {
            Statics.Aggregate((pointA, pointB) =>
            {
                rt.DrawLine(pointA, pointB, Brush, StrokeWidth);
                return pointB;
            });
            if(!disposed)
                rt.DrawLine(Line[2], Line[3], Brush, StrokeWidth);
        }

        public void Tick() {
            Line[2] = Line[0] + MatrixExtensions.PVTranslation;
            Line[3] = Line[1] + MatrixExtensions.PVTranslation;

            Statics.Clear();
            foreach(Vector2 point in InitialStatics) {
                Statics.Add(point + MatrixExtensions.PVTranslation);
            }

            if(Duration <= 0) {
                    Dispose();
            } else {
                Duration -= GameStatus.TimeMultiplier;
                if(!disposed) {
                    Brush.Color = new Color4(InitialBrush.Color.R, InitialBrush.Color.G, InitialBrush.Color.B, (Duration / initialDuration) * InitialBrush.Color.A);
                    StrokeWidth = InitialStrokeWidth * (Duration / initialDuration);
                }
            }
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    GameStatus.removeRenderable(this);
                    GameStatus.removeTickable(this);
                    InitialBrush.Dispose();
                    Brush.Dispose();
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
