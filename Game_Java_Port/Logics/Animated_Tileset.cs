using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Logics {
    public class Animated_Tileset {

        private Tileset AnimationTile;

        private float AnimationRate;

        private static Dictionary<Tileset, Animated_Tileset> buffer = new Dictionary<Tileset, Animated_Tileset>();
        
        public Size2 Size { get {
                return AnimationTile.TileSize;
            } }

        public static Animated_Tileset Bullet_Acid { get {
                if(!buffer.ContainsKey(Tileset.Anim_Bullet_Acid))
                    buffer.Add(Tileset.Anim_Bullet_Acid, new Animated_Tileset(Tileset.Anim_Bullet_Acid));
                return buffer[Tileset.Anim_Bullet_Acid];
            } }

        private Animated_Tileset(Tileset tiles, float rate = 0.1f) {
            AnimationTile = tiles;
            AnimationRate = rate;
        }
        
        public Bitmap Frame {
            get {
                int index = (int)((Program.stopwatch.Elapsed.TotalSeconds / AnimationRate) % AnimationTile.Tiles.Length);
                return AnimationTile.Tiles[index];
            }
        }

    }
}
