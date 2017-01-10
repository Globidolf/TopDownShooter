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

        public static Vector2 PVTranslation { get {
                return _PVTranslation;
            } }

        public static void Tick() {
            _PVTranslation = Game.instance.Location - (Game.instance._player == null ? Vector2.Zero : Game.instance._player.Location);
        }

    }
}
