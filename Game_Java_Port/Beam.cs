using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port
{
	class Beam : IRenderable, ITickable
	{

		public void updateRenderData() {
			float time = Duration / initialDuration;
			RenderData.mdl.VertexBuffer = RenderData.mdl.VertexBuffer.ApplyColor(InitialColor.ToVector4() * new Vector4(1,1,1, time * InitialColor.A));
		}
		private readonly float initialDuration;
		private float Duration;

		public RenderData RenderData { get; set; }

		private readonly Color InitialColor;

		public Beam(Vector2 PointA, Vector2 PointB,
			//optional values:
			float duration = 0.3f, float strokewidth = 2f,
			bool electric = false, int lightningcount = 4,
			int seed = 0, Color? beamColor = null) {

			// get Difference of the two points
			Vector2 normdir = (PointA - PointB);
			// rotate by 90° (FAST VERSION)
			normdir = new Vector2(normdir.Y, -normdir.X);
			// Normalize
			normdir.Normalize();
			// Multiply with half strokewidth
			normdir *= strokewidth / 2;
			RenderData = new RenderData
			{
				mdl = Model.Quadrilateral(PointA + normdir, PointA- normdir, PointB + normdir, PointB - normdir),
				ResID = dataLoader.getResID()
			};
			beamColor = beamColor.HasValue ? beamColor.Value : new Color() { A = 0x88, B = 255 };
			//new Ellipse(Area.Center + MatrixExtensions.PVTranslation, Area.Width / 2, Area.Height / 2);
			initialDuration = Duration = duration;

			RenderData.mdl.VertexBuffer = RenderData.mdl.VertexBuffer.ApplyColor(beamColor.Value);

			if (electric) {
				List<Vector2> InitialStatics = new List<Vector2>();
				Random _RNG = new Random(seed);
				float distance = Vector2.Distance(PointB, PointA);
				float offset = distance * (float)_RNG.NextDouble();
				float length = 1 + (float)_RNG.NextDouble() * (distance - offset) / 2;
				AngleSingle dir = PointA.angleTo(PointB);
				Vector2 split1 = (PointA).move(dir, offset);
				InitialStatics.Add(split1);
				while (offset < distance && length > (distance - offset) / 12 && lightningcount > 0) {

					offset += distance * (float) _RNG.NextDouble();
					length /= 2;
					dir.Radians += (float) (-Math.PI / 2 + _RNG.NextDouble() * Math.PI);


					split1 = split1.move(dir, length);

					InitialStatics.Add(split1);

					lightningcount--;
				}
				RenderData.SubObjs = new RenderData[InitialStatics.Count - 1];
				int i = 0;
				InitialStatics.Aggregate((p1, p2) => {
					// get Difference of the two points
					normdir = (p2 - p1);
					// rotate by 90° (FAST VERSION)
					normdir = new Vector2(normdir.Y, -normdir.X);
					// Normalize
					normdir.Normalize();
					// Multiply with half strokewidth
					normdir *= strokewidth / 2;
					RenderData.SubObjs[i] = new RenderData
					{
						ResID = RenderData.ResID,
						mdl = Model.Quadrilateral(p1 + normdir, p1 - normdir, p2 + normdir, p2 - normdir)
					};
					RenderData.SubObjs[i].mdl.VertexBuffer = RenderData.SubObjs[i].mdl.VertexBuffer.ApplyColor(beamColor.Value);
					i++;
					return p2;
				});
			}
			this.register();//GameStatus.addRenderable(this);
			GameStatus.addTickable(this);
		}
		
		public void Tick() {
			if (Duration <= 0) {
				GameStatus.removeTickable(this);
				this.unregister();
			} else
				Duration -= GameStatus.TimeMultiplier;
		}
	}
}
