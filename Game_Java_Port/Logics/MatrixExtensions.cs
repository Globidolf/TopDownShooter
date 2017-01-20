using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    public static class MatrixExtensions {

        private static Vector2 _PVTranslation;

        public static RawMatrix5x4 toColorMatrix(this Color color) {
            return new RawMatrix5x4() 
            {
                M11 = color.R,  M12 = 0,        M13 = 0,        M14 = 0,
                M21 = 0,        M22 = color.G,  M23 = 0,        M24 = 0,
                M31 = 0,        M32 = 0,        M33 = color.B,  M34 = 0,
                M41 = 0,        M42 = 0,        M43 = 0,        M44 = color.A,
                M51 = 0,        M52 = 0,        M53 = 0,        M54 = 0
            };
        }

        public static Vector2 PVTranslation { get {
                return _PVTranslation;
            } }

        public static void Tick() {
            _PVTranslation = Game.instance.Location - (Game.instance._player == null ? Vector2.Zero : Game.instance._player.Location);
        }



    }
}
