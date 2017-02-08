using Game_Java_Port.Interface;
using Game_Java_Port.Logics;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.State
{
	public class UI_Bar : IRenderable
	{
		public RenderData RenderData{ get; set; }

		private bool changed;
		private float _Value = 1;

		private string stringbuffer;
		/// <summary>
		/// Set between 0 and 1
		/// </summary>
		public float Value { set {
				_Value = value;
				changed = true;
			} }
		
		public void updateRenderData() {
			if (changed) {

			}
		}

		public UI_Bar(Rectangle Area, Func<string> textProvider, Color front, Color back) : this(Area, textProvider(), front, back) { }
		public UI_Bar(Rectangle Area, string text				, Color front, Color back) {
			stringbuffer = text;
			int ID_colormap = dataLoader.getResID("cmap_bar");
			int ID_barborder = dataLoader.getResID("border_bar");
			RenderData = new RenderData { // base, filled color bar
				Area = Area,
				ResID = ID_colormap,
				SubObjs = new[] {
					new RenderData { Area = Area, ResID = ID_colormap }, //index 0, empty color bar
					new RenderData { Area = Area, ResID = ID_barborder }, //index 1, bar border
					SpriteFont.DEFAULT.generateText(text, Area), // index 2, Text
					SpriteFont.DEFAULT.generateText(text, Area) } }; // index 3, Text outline

			RenderData.mdl.VertexBuffer.ApplyColor(front); // apply color to front

			RenderData.SubObjs[0].mdl.VertexBuffer.ApplyColor(back); // apply color to back

			for(int i = 0 ; i < RenderData.SubObjs[3].SubObjs.Length ; i++) {
				RenderData.SubObjs[3].SubObjs[i].mdl.VertexBuffer.ApplyColor(Color.Black);
				RenderData.SubObjs[3].SubObjs[i].mdl.VertexBuffer = RenderData.SubObjs[3].mdl.VertexBuffer.MultiplyPos(1.1f);
			}
		}
	}
}
