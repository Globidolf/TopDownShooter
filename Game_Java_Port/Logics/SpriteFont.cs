using SharpDX;
//using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Logics {
    public class SpriteFont {

        public int LineSpacing { get; set; } = 2;
        public int ParagraphSpacing { get; set; } = 5;

        public Size2 TileSize;

        public RenderData _Font;
        //private Tileset _Font;

        private string[] prepare(string s, Size2 max, out Size2 actualSize) {

            string[] substrings = s.Split('\n');
            int[] charcounts = new int[substrings.Length];
            int[] calculatedwidths = new int[substrings.Length];
            
            int lettersPerLine = max.Width / TileSize.Width;
            int maxLettersTotal = (max.Height / TileSize.Height) * lettersPerLine;

            int calculatedheight = 0;

            Size2 Size = new Size2(
                lettersPerLine * TileSize.Width,
                (max.Height / TileSize.Height) * TileSize.Height);


            int lastchar = 0;
            int lastlineindex = 0;

            for(int i = 0; i < substrings.Length; i++) {
                calculatedheight += TileSize.Height + ParagraphSpacing;
                charcounts[i] = substrings[i].Length;
                calculatedwidths[i] = charcounts[i] * TileSize.Width;


                if(max.Width > 0 && charcounts[i] > lettersPerLine) {
                    if (lettersPerLine == 0)
                        calculatedheight += (TileSize.Height + LineSpacing);
                    else
                        calculatedheight += ((charcounts[i] - 1) / lettersPerLine + 1) * (TileSize.Height + LineSpacing);
                    if(calculatedheight > Size.Height) {
                        int lastline = calculatedheight - Size.Height;
                        for(int j = 0; j < i; j++) {
                            lastchar += substrings[j].Length + 1; // +1 = newline char
                        }
                        lastchar += lastline * lettersPerLine;
                        lastlineindex = i;
                        break;
                    }
                }
            }

            if(lastlineindex != 0)
                calculatedwidths = calculatedwidths.Take(lastlineindex).ToArray();

            if(lastchar != 0) {
                s = new string(s.Take(lastchar - 3).ToArray()) + "...";
                substrings = s.Split('\n');
            }

            if(max.Width == 0 || calculatedwidths.All(w => w < max.Width))
                Size.Width = calculatedwidths.Max();

            if(max.Height == 0 || calculatedheight < max.Height)
                Size.Height = calculatedheight;

            actualSize = Size;

            return substrings;
        }

        public Size2 MeasureString(string s) {
            return MeasureString(s, Size2.Empty);
        }
        public Size2 MeasureString(string s, Size2 max) {

            string[] substrings = s.Split('\n');
            int[] charcounts = new int[substrings.Length];
            int[] calculatedwidths = new int[substrings.Length];

            int lettersPerLine = max.Width / TileSize.Width;
            int maxLettersTotal = (max.Height / TileSize.Height) * lettersPerLine;

            int calculatedheight = 0;

            Size2 Size = new Size2(
                lettersPerLine * TileSize.Width,
                (max.Height/ TileSize.Height) * TileSize.Height);
            

            int lastchar = 0;
            int lastlineindex = 0;

            for(int i = 0; i < substrings.Length; i++) {
                calculatedheight += TileSize.Height + ParagraphSpacing;
                charcounts[i] = substrings[i].Length;
                calculatedwidths[i] = charcounts[i] * TileSize.Width;


                if(max.Width > 0 && charcounts[i] > lettersPerLine) {
                    calculatedheight += ((charcounts[i] - 1) / lettersPerLine + 1) * (TileSize.Height + LineSpacing);
                    if(calculatedheight > Size.Height) {
                        int lastline = calculatedheight - Size.Height;
                        for(int j = 0; j < i; j++) {
                            lastchar += substrings[j].Length + 1; // +1 = newline char
                        }
                        lastchar += lastline * lettersPerLine;
                        lastlineindex = i;
                        break;
                    }
                }
            }

            if(lastlineindex != 0)
                calculatedwidths = calculatedwidths.Take(lastlineindex).ToArray();

            if(lastchar != 0) {
                s = new string(s.Take(lastchar - 3).ToArray()) + "...";
                substrings = s.Split('\n');
            }

            if(max.Width == 0 || calculatedwidths.All(w => w < max.Width))
                Size.Width = calculatedwidths.Max();

            if(max.Height == 0 || calculatedheight < max.Height)
                Size.Height = calculatedheight;

            return Size;
        }

        #region generateText overloads
        public RenderData generateText(string s, float X = 0, float Y = 0, float Width = 0, float Height = 0) { return generateText(s, new RectangleF(X, Y, Width, Height)); }
        public RenderData generateText(string s, int X = 0, int Y = 0, int Width = 0, int Height = 0) { return generateText(s, new RectangleF(X,Y,Width,Height)); }
        public RenderData generateText(string s, Point pos) { return generateText(s, new RectangleF(pos.X, pos.Y, 0, 0)); }
        public RenderData generateText(string s, Vector2 pos) { return generateText(s, new RectangleF(pos.X, pos.Y, 0,0)); }
        public RenderData generateText(string s, Size2 size) { return generateText(s, new RectangleF(0,0,size.Width, size.Height)); }
        public RenderData generateText(string s, Size2F size) { return generateText(s, new RectangleF(0, 0, size.Width, size.Height)); }
        public RenderData generateText(string s, Rectangle area) { return generateText(s, new RectangleF(area.X, area.Y, area.Width, area.Height)); }
        #endregion

        public RenderData generateText(string s, RectangleF? Area = null) {

            RectangleF ActualArea = Area.GetValueOrDefault();


            Size2 Size = Size2.Empty;

            // gets lines seperated by \n's and the final size of the text
            string[] substr = prepare(s, new Size2((int)ActualArea.Width, (int)ActualArea.Height), out Size);


            ActualArea.Size = new Size2F(Size.Width,Size.Height);

            RenderData result = _Font;

            result.Area = ActualArea;

            /*
            // this block creates a pointer to the buffer which is required for the datapointer constructor.
            // the normal constructor of the sharpdx bitmap wrapper class is bugged. it randomly inserts red pixels.
            // to prevent that from happening we create our own empty buffer as dataset for the empty bitmap.
            unsafe {
                fixed (byte* pointer = buffer) {
                    IntPtr ptr = new IntPtr(pointer);
                    result = new Bitmap(Program.D2DContext, Size, new DataPointer(pointer, buffer.Length), 4 * Size.Width, new BitmapProperties(Program.PForm));
                }
            }
            */

            //draws the text into the bitmap while preserving the bitmaps transparency.
            drawText(substr, result);

            return result;
        }

		/*
        /// <summary>
        /// Fills the bitmap with the last character in the spriteset
        /// </summary>
        /// <param name="result"></param>
        private void fill(Bitmap result) {
            for(int y = 0; y < result.PixelSize.Height; y += _Font.TileSize.Height) {
                for(int x = 0; x < result.PixelSize.Width; x += _Font.TileSize.Width) {
                    result.CopyFromBitmap(_Font.Tiles.Last(), new Point(x, y));
                }
            }
        }
		*/

        private void drawText(string[] s, RenderData output) {
            // 
            int y = ParagraphSpacing;

            int x = 0;
            foreach(string substring in s) { 

                foreach(char c in substring) {
                    
                    //output.CopyFromBitmap(translate(c), new Point(x,y));
                    
                    x += TileSize.Width;

                    if (x > dataLoader.D3DResources[output.ResID].Description.Width) { // line break
                        y += TileSize.Height + LineSpacing;
                        x = 0;
                    }
                }

                y += TileSize.Height + ParagraphSpacing;// new line
                x = 0;
            }
        }

        public RenderData translate(char c) {
			RenderData result = new RenderData {
				AnimationFrameCount = _Font.AnimationFrameCount,
				ResID = _Font.ResID,
				mdl = _Font.mdl
			};
            if(c < 32 || c - 32 > _Font.AnimationFrameCount.X * _Font.AnimationFrameCount.Y)
				result.mdl.VertexBuffer.SetAnimationFrame(result.AnimationFrameCount.X * result.AnimationFrameCount.Y - 1, result.AnimationFrameCount);
			else
				result.mdl.VertexBuffer.SetAnimationFrame(c - 32, result.AnimationFrameCount);
            return result;
        }

        private SpriteFont(int ResID, int cols = 32, int rows = 3) {
            TileSize = new Size2(
                dataLoader.D3DResources[ResID].Description.Width / cols,
                dataLoader.D3DResources[ResID].Description.Height / rows);
            _Font = new RenderData {
                AnimationFrameCount = new Point(cols, rows),
                mdl = Model.Square,
                ResID = ResID,
                Area = new RectangleF(0,0,TileSize.Width, TileSize.Height)
            };

        }

        private static SpriteFont _DEFAULT;

        public static SpriteFont DEFAULT { get {
                if (_DEFAULT == null) {
                    _DEFAULT = new SpriteFont(dataLoader.getResID("font_default_6_8"));
                }
                return _DEFAULT;
            } }

    }
}
