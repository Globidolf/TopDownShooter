using Game_Java_Port.Interface;
using System;
using System.IO;

using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using SharpDX;
using SharpDX.Direct3D11;

namespace Game_Java_Port {
    public class Background : IRenderable, ITickable {
		

		virtual public void updateRenderData() {
			//NO UPDATES REQUIRED
		}
        private float lifetime = 0;

        [Flags]
        public enum Settings : byte {
            /// <summary>
            /// Texture is static on-screen, stretched over it's Area, won't disappear and is rendered behind other objects.
            /// </summary>
            Default = 0x00,
            /// <summary>
            /// Texture does not stretch but repeat
            /// </summary>
            Repeat = 0x01,
            /// <summary>
            /// Texture will move with the world
            /// </summary>
            Parallax = 0x01 << 1,
			/// <summary>
			/// Texture will disappear after some time. (Set lifetime to something)
			/// </summary>
            Decay = 0x01 << 2,
			/// <summary>
			/// Texture will be drawn in front other objects instead of behind them
			/// </summary>
            Foreground = 0x01 << 3
        }

        public Settings settings = Settings.Default;
		
        public RenderData RenderData { get; set; }

        public Background(int resID, int resID2, RectangleF? Area = null, float lifetime = 0, Settings settings = Settings.Default, bool add = true) {
            Texture2D tx = dataLoader.D3DResources[resID];
            
            Area = Area.HasValue ? Area : new RectangleF(0,0, tx.Description.Width, tx.Description.Height);
			RenderData = new RenderData
			{
				mdl = new Model
				{
					VertexBuffer = Vertex.FromRectangle(Area.Value),
					IndexBuffer = TriIndex.QuadIndex
				},
				ResID = resID,
				ResID2 = resID2
			};

			RenderData.mdl.VertexBuffer.ApplyZAxis(settings.HasFlag(Settings.Foreground) ? 5 : -5);
			
            this.settings = settings;
			if (settings.HasFlag(Settings.Repeat)) {
				RenderData.mdl.VertexBuffer.ApplyTextureRepetition(new Vector2(Area.Value.Size.Width / tx.Description.Width, Area.Value.Height / tx.Description.Height));
			}
            if(lifetime > 0) {
                this.lifetime = lifetime;
                this.settings |= Settings.Decay;
            }

            if(add)
                addToGame();

        }



        public virtual void draw(SharpDX.Direct2D1.DeviceContext rt) {

        }

        public virtual void Tick() {
            if(settings.HasFlag(Settings.Decay)) {
                lifetime -= GameStatus.TimeMultiplier;
				if (lifetime <= 0)
					this.unregister();
            }
        }

        public void addToGame() {
			GameStatus.addTickable(this);
			this.register();
        }
    }
}
