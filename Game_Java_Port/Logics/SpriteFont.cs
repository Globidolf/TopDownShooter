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

		private readonly RenderData[] buffer;

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
			if (s == null || s == "")
				return max;
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
        public RenderData generateText(string s, float X = 0, float Y = 0, float Z = 0, float Width = 0, float Height = 0, Color? color = null, Color? shadow = null) { return generateText(s, new Rectangle((int) X, (int) Y, (int) Width, (int) Height), Z	,color,shadow); }
        public RenderData generateText(string s, Point pos, float Z = 0			, Color? color = null, Color? shadow = null) { return generateText(s, new Rectangle(pos.X, pos.Y, 0, 0), Z												,color,shadow); }
        public RenderData generateText(string s, Vector2 pos, float Z = 0		, Color? color = null, Color? shadow = null) { return generateText(s, new Rectangle((int) pos.X, (int) pos.Y, 0,0), Z												,color,shadow); }
        public RenderData generateText(string s, Size2 size, float Z = 0		, Color? color = null, Color? shadow = null) { return generateText(s, new Rectangle(0,0,size.Width, size.Height), Z									,color,shadow); }
        public RenderData generateText(string s, Size2F size, float Z = 0		, Color? color = null, Color? shadow = null) { return generateText(s, new Rectangle(0, 0, (int) size.Width, (int) size.Height), Z									,color,shadow); }
        public RenderData generateText(string s, RectangleF area, float Z = 0	, Color? color = null, Color? shadow = null) { return generateText(s, new Rectangle((int) area.X, (int) area.Y, (int) area.Width, (int) area.Height), Z						,color,shadow); }
        #endregion

        public RenderData generateText(string s, Rectangle Area, float Z = 0, Color? color = null, Color? shadow = null) {

            Size2 Size = Size2.Empty;

            // gets lines seperated by \n's and the final size of the text
            string[] substr = prepare(s, new Size2(Area.Width, Area.Height), out Size);


			Area.Width = Size.Width;
			Area.Height = Size.Height;

            RenderData result = _Font.ValueCopy();

            result.Area = Area;
			result.Z = Z;

            drawText(substr, result);

            return result;
        }

        private void drawText(string[] s, RenderData output, Color? color = null, Color? shadow = null) {

			shadow = shadow.HasValue ? shadow : Color.Black;
			color = color.HasValue ? color : Color.White;

			List<RenderData> characters = new List<RenderData>();

            int y = LineSpacing;
            int x = 0;
            foreach(string substring in s) { 

                foreach(char c in substring) {
					RenderData rd = translate(c);
					rd.mdl.VertexBuffer.ApplyRectangle(new RectangleF(output.mdl.VertexBuffer[0].Pos.X + x, output.mdl.VertexBuffer[0].Pos.Y + y, TileSize.Width, TileSize.Height));
					rd.mdl.VertexBuffer.ApplyColor(shadow.Value);
					rd.mdl.VertexBuffer.TranslatePos(1);
					rd.Z = output.mdl.VertexBuffer[0].Pos.Z + Renderer.LayerOffset_Text;
					characters.Add(rd);
					rd = rd.ValueCopy();
					rd.mdl.VertexBuffer.ApplyColor(color.Value);
					rd.Z = output.mdl.VertexBuffer[0].Pos.Z + Renderer.LayerOffset_Text + Renderer.LayerOffset_Outline;
					rd.mdl.VertexBuffer.TranslatePos(-1);
					characters.Add(rd);

					x += TileSize.Width;

                    if (x > dataLoader.Font.Description.Width) { // line break
                        y += TileSize.Height + LineSpacing;
                        x = 0;
                    }
                }

                y += TileSize.Height + ParagraphSpacing;// new line
                x = 0;
            }
			output.mdl = characters.Merge(output.ResID, output.ResID2).mdl;
        }

        public RenderData translate(char c) {
			if (buffer.Length >= c - 32)
				return buffer[c - 32].ValueCopy();
			else return buffer[buffer.Length - 1].ValueCopy();
        }

        private SpriteFont(int cols = 32, int rows = 3) {
            TileSize = new Size2(
                dataLoader.Font.Description.Width / cols,
                dataLoader.Font.Description.Height / rows);
            _Font = new RenderData {
                AnimationFrameCount = new Point(cols, rows),
                Area = new Rectangle(0,0,TileSize.Width, TileSize.Height),
                ResID = -1,
            };
			_Font.mdl.VertexBuffer.ApplyColor(Color.Transparent);
			buffer = new RenderData[cols * rows];
			for (int i = 0 ; i < rows ; i++)
				for (int j = 0 ; j < cols ; j++) {
					RenderData result = new RenderData
					{
						AnimationFrameCount = _Font.AnimationFrameCount,
						mdl = _Font.mdl.ValueCopy(),
						ResID = _Font.ResID,
						ResID2 = _Font.ResID2
					};
					result.mdl.VertexBuffer.ApplyColor(Color.White);

					result.mdl.VertexBuffer.SetAnimationFrame(i * cols + j, result.AnimationFrameCount);

					buffer[i * cols + j] = result;
				}
        }

        private static SpriteFont _DEFAULT;

        public static SpriteFont DEFAULT { get {
                if (_DEFAULT == null) {
                    _DEFAULT = new SpriteFont();
                }
                return _DEFAULT;
            } }

    }
}
