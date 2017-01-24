using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Logics {
    public class SpriteFont {

        public int LineSpacing { get; set; } = 2;
        public int ParagraphSpacing { get; set; } = 5;

        private Tileset _Font;

        private string[] prepare(string s, Size2 max, out Size2 actualSize) {

            string[] substrings = s.Split('\n');
            int[] charcounts = new int[substrings.Length];
            int[] calculatedwidths = new int[substrings.Length];

            int lettersPerLine = max.Width / _Font.TileSize.Width;
            int maxLettersTotal = (max.Height / _Font.TileSize.Height) * lettersPerLine;

            int calculatedheight = 0;

            Size2 Size = new Size2(
                lettersPerLine * _Font.TileSize.Width,
                (max.Height / _Font.TileSize.Height) * _Font.TileSize.Height);


            int lastchar = 0;
            int lastlineindex = 0;

            for(int i = 0; i < substrings.Length; i++) {
                calculatedheight += _Font.TileSize.Height + ParagraphSpacing;
                charcounts[i] = substrings[i].Length;
                calculatedwidths[i] = charcounts[i] * _Font.TileSize.Width;


                if(max.Width > 0 && charcounts[i] > lettersPerLine) {
                    if (lettersPerLine == 0)
                        calculatedheight += (_Font.TileSize.Height + LineSpacing);
                    else
                        calculatedheight += ((charcounts[i] - 1) / lettersPerLine + 1) * (_Font.TileSize.Height + LineSpacing);
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

            int lettersPerLine = max.Width / _Font.TileSize.Width;
            int maxLettersTotal = (max.Height / _Font.TileSize.Height) * lettersPerLine;

            int calculatedheight = 0;

            Size2 Size = new Size2(
                lettersPerLine * _Font.TileSize.Width,
                (max.Height/ _Font.TileSize.Height) * _Font.TileSize.Height);
            

            int lastchar = 0;
            int lastlineindex = 0;

            for(int i = 0; i < substrings.Length; i++) {
                calculatedheight += _Font.TileSize.Height + ParagraphSpacing;
                charcounts[i] = substrings[i].Length;
                calculatedwidths[i] = charcounts[i] * _Font.TileSize.Width;


                if(max.Width > 0 && charcounts[i] > lettersPerLine) {
                    calculatedheight += ((charcounts[i] - 1) / lettersPerLine + 1) * (_Font.TileSize.Height + LineSpacing);
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

        public Bitmap generateText(string s, Size2 size) {
            return generateText(s, size.Width, size.Height);
        }
        public Bitmap generateText(string s, Size2F size) {
            return generateText(s, (int)size.Width, (int)size.Height);
        }

        public Bitmap generateText(string s, RectangleF area) {
            return generateText(s, (int)area.Width, (int)area.Height);
        }

        public Bitmap generateText(string s, Rectangle area) {
            return generateText(s, area.Width, area.Height);
        }
        
        public Bitmap generateText(string s, int textwidth = 0, int textheight = 0) {

            Size2 Size = Size2.Empty;

            // gets lines seperated by \n's and the final size of the text
            string[] substr = prepare(s, new Size2(textwidth,textheight), out Size);

            // prepare an absolutely empty bitmap buffer with transparency
            byte[] buffer = new byte[Size.Height * Size.Width * 4];


            Bitmap result;
            
            // this block creates a pointer to the buffer which is required for the datapointer constructor.
            // the normal constructor of the sharpdx bitmap wrapper class is bugged. it randomly inserts red pixels.
            // to prevent that from happening we create our own empty buffer as dataset for the empty bitmap.
            unsafe {
                fixed (byte* pointer = buffer) {
                    IntPtr ptr = new IntPtr(pointer);
                    result = new Bitmap(Program.D2DContext, Size, new DataPointer(pointer, buffer.Length), 4 * Size.Width, new BitmapProperties(Program.PForm));
                }
            }
            
            //draws the text into the bitmap while preserving the bitmaps transparency.
            drawText(substr, result);

            return result;
        }

        public void directDrawText(string text, RectangleF Area, DeviceContext rt) {
            Size2 size;
            string[] s = prepare(text, new Size2((int)Area.Width, (int)Area.Height), out size);
            directDrawText(s, Area, rt);
        }
        public void directDrawText(string text, RectangleF Area, DeviceContext rt, Color filter) {
            Size2 size;
            string[] s = prepare(text, new Size2((int)Area.Width, (int)Area.Height), out size);
            
            directDrawColoredText(s, Area, rt, filter);
        }

        /* matrix preset
         new SharpDX.Mathematics.Interop.RawMatrix5x4() 
            {
                M11 = 0, M12 = 0, M13 = 0, M14 = 0,
                M21 = 0, M22 = 0, M23 = 0, M24 = 0,
                M31 = 0, M32 = 0, M33 = 0, M34 = 0,
                M41 = 0, M42 = 0, M43 = 0, M44 = 0,
                M51 = 0, M52 = 0, M53 = 0, M54 = 0
            };
         */
        private void directDrawText(string[] s, RectangleF Area, DeviceContext rt) {
            
            int y = LineSpacing;

            int x = 1;
            foreach(string substring in s) {

                foreach(char c in substring) {
                    

                    rt.DrawBitmap(translate(c), new RectangleF(Area.X + x, Area.Y + y, _Font.TileSize.Width, _Font.TileSize.Height), 1, BitmapInterpolationMode.NearestNeighbor);
                    

                    x += _Font.TileSize.Width;

                    if(x > Area.Width) { // line break
                        y += _Font.TileSize.Height + LineSpacing;
                        x = 0;
                    }
                }

                y += _Font.TileSize.Height + ParagraphSpacing;// new line
                x = 0;
            }
        }

        private void directDrawColoredText(string[] s, RectangleF Area, DeviceContext rt, Color filter) {

            SharpDX.Direct2D1.Effects.ColorMatrix effect = new SharpDX.Direct2D1.Effects.ColorMatrix(rt);

            effect.Matrix = filter.toColorMatrix();
            
            int y = LineSpacing;

            int x = 1;
            foreach(string substring in s) {

                foreach(char c in substring) {

                    effect.SetInput(0, translate(c), true);

                    

                    Image temp = effect.Output;

                    rt.DrawImage(temp, new Vector2(Area.X + x, Area.Y + y));

                    temp.Dispose();
                    
                    x += _Font.TileSize.Width;

                    if(x > Area.Width) { // line break
                        y += _Font.TileSize.Height + LineSpacing;
                        x = 0;
                    }
                }

                y += _Font.TileSize.Height + ParagraphSpacing;// new line
                x = 0;
            }

            //effect.Output.Dispose();

            effect.Dispose();
        }

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

        private void drawText(string[] s, Bitmap output) {
            // 
            int y = ParagraphSpacing;

            int x = 0;
            foreach(string substring in s) { 

                foreach(char c in substring) {
                    
                    output.CopyFromBitmap(translate(c), new Point(x,y));
                    
                    x += _Font.TileSize.Width;

                    if (x > output.PixelSize.Width) { // line break
                        y += _Font.TileSize.Height + LineSpacing;
                        x = 0;
                    }
                }

                y += _Font.TileSize.Height + ParagraphSpacing;// new line
                x = 0;
            }
        }

        public Bitmap translate(char c) {
            if(c < 32 || c - 32 > _Font.Tiles.Length)
                return _Font.Tiles.Last(); // Not available
            return _Font.Tiles[c - 32]; // translation
        }

        private SpriteFont(Tileset font) {
            _Font = font;
        }

        private static SpriteFont _DEFAULT;

        public static SpriteFont DEFAULT { get {
                if (_DEFAULT == null) {
                    _DEFAULT = new SpriteFont(Tileset.Font_Default);
                }
                return _DEFAULT;
            } }

    }
}
