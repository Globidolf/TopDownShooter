using Game_Java_Port.Interface;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.DirectWrite;

namespace Game_Java_Port {
    public class Tooltip : IRenderable, ITickable {

        bool show = false;

        public float Padding { get; set; } = 2;

        internal string Text { get; set; }

        public RectangleF Area { get; set; }
        private RectangleF _LabelArea;
        RectangleF relArea;
        RectangleF relLabel;

        public int Z { get; set; } = 10000;

        public DrawType drawType { get; set; } = DrawType.Rectangle;

        public Tooltip(string text, Func<Vector2> Location = null, Func<bool> Validation = null, bool ticksInternal = false) {

            if(Location != null)
                getTarget = Location;
            if(Validation != null)
                doDraw = Validation;

            Size2F size;

            using(TextLayout tl = new TextLayout(Program.DW_Factory, text, GameStatus.MenuFont, 500, 500))
                size = new Size2F(tl.Metrics.Width, tl.Metrics.Height);

            Area = new RectangleF(0, 0, size.Width + Padding * 2, size.Height + Padding * 2);

            _LabelArea = Area;

            _LabelArea.Location += Padding;

            _LabelArea.Size = size;

            Text = text;

            if (!ticksInternal)
                GameStatus.addTickable(this);
        }

        public void draw(RenderTarget rt) {
            Matrix3x2 translation = Matrix3x2.Identity;
            translation.TranslationVector = relArea.Location;
            lock(GameStatus.BGBrush) {
                GameStatus.BGBrush.Transform = translation;
                rt.FillRectangle(relArea, GameStatus.BGBrush);
                GameStatus.BGBrush.Transform = Matrix3x2.Identity;
            }
            rt.DrawText(Text, GameStatus.MenuFont, relLabel, GameStatus.MenuTextBrush);
        }

        /// <summary>
        /// Dynamic calculation of target location. set to another Func&lt;Vector2&gt; returning your prefered target location.
        /// default is the current mouse location.
        /// </summary>
        public Func<Vector2> getTarget = () => GameStatus.MousePos;

        public Func<bool> doDraw = () => true;

        public void Tick() {

            if (doDraw?.Invoke() == true != show) {
                if(show)
                    GameStatus.removeRenderable(this);
                else
                    GameStatus.addRenderable(this);
                lock(this)
                    show ^= true;
            }

            if(show) {
                relArea = Area;
                relLabel = _LabelArea;

                doDraw.Invoke();

                Vector2 target = getTarget();
                Vector2 Offset = new Vector2(target.X - Area.Width / 2, target.Y + 20);

                if(Area.Height > GameStatus.ScreenHeight / 2 && Offset.Y + Area.Height > GameStatus.ScreenHeight) {
                    Offset.Y = GameStatus.ScreenHeight - Area.Height;
                } else if(Offset.Y + Area.Height > GameStatus.ScreenHeight)
                    Offset.Y -= Area.Height + 40;

                relArea.Offset(Offset);
                relLabel.Offset(Offset);
            }
        }

        public void Show() {
            lock(this) {
                if(!show) {
                    show = true;
                    GameStatus.addRenderable(this);
                }
            }
        }

        public void Hide() {
            lock(this)
                if(show) {
                    show = false;
                    GameStatus.removeRenderable(this);
                }
        }

        public void Dispose() {
            doDraw = null;
            getTarget = null;
            Hide();
        }
    }
}
