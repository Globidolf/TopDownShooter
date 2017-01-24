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
                M11 = color.R,/*M12 = 0,        M13 = 0,        M14 = 0,*/
              /*M21 = 0,      */M22 = color.G,/*M23 = 0,        M24 = 0,*/
              /*M31 = 0,        M32 = 0,      */M33 = color.B,/*M34 = 0,*/
              /*M41 = 0,        M42 = 0,        M43 = 0,      */M44 = color.A,
              /*M51 = 0,        M52 = 0,        M53 = 0,        M54 = 0*/
            };

        }

        public static Vector2 PVTranslation { get {
                return _PVTranslation;
            } }


        public static void Tick() {
            _PVTranslation = Game.instance.Location - (Game.instance._player == null ? Vector2.Zero : Game.instance._player.Location);
        }

        #region RawMatrix

        #region Statics

        public static RawMatrix M4x4Identity {
            get {
                return new RawMatrix() {
                    M11 = 1,
                    M22 = 1,
                    M33 = 1,
                    M44 = 1
                };
            }
        }

        #endregion

        #region Rotation

        public static RawMatrix CreateRotationMatrix2D(AngleSingle A) {
            return CreateRotationMatrixZ(A);
        }
        public static RawMatrix CreateRotationMatrixX(AngleSingle A) {
            return new RawMatrix() {
                M11 = 1,  /*M21 = 0,                             M31 = 0,                              M41 = 0,*/
              /*M12 = 0,*/  M22 = (float)Math.Cos(A.Radians),    M32 = -(float)Math.Sin(A.Radians),  /*M42 = 0,*/
              /*M13 = 0,*/  M23 = (float)Math.Sin(A.Radians),    M33 =  (float)Math.Cos(A.Radians),  /*M43 = 0,*/
              /*M14 = 0,    M24 = 0,                             M34 = 0,*/                            M44 = 1
            };
        }
        public static RawMatrix CreateRotationMatrixY(AngleSingle A) {
            return new RawMatrix() {
                M11 =  (float)Math.Cos(A.Radians),/*M21 = 0,*/  M31 = (float)Math.Sin(A.Radians),  /*M41 = 0,*/
              /*M12 = 0,*/                          M22 = 1,  /*M32 = 0,                             M42 = 0,*/
                M13 = -(float)Math.Sin(A.Radians),/*M23 = 0,*/  M33 = (float)Math.Cos(A.Radians),  /*M43 = 0,*/
              /*M14 = 0,                            M24 = 0,    M34 = 0,*/                           M44 = 1
            };
        }
        public static RawMatrix CreateRotationMatrixZ(AngleSingle A) {
            return new RawMatrix() {
                M11 = (float)Math.Cos(A.Radians),  M21 = -(float)Math.Sin(A.Radians),/*M31 = 0,    M41 = 0,*/
                M12 = (float)Math.Sin(A.Radians),  M22 =  (float)Math.Cos(A.Radians),/*M32 = 0,    M42 = 0,*/
              /*M13 = 0,                           M23 = 0,*/                          M33 = 1,  /*M43 = 0,*/
              /*M14 = 0,                           M24 = 0,                            M34 = 0,*/  M44 = 1
            };
        }

        public static RawMatrix Rotate2D(this RawMatrix M, AngleSingle A) {
            return M.RotateZ(A);
        }
        public static RawMatrix RotateX(this RawMatrix M, AngleSingle A) {
            return M.Multiply(CreateRotationMatrixX(A));
        }
        public static RawMatrix RotateY(this RawMatrix M, AngleSingle A) {
            return M.Multiply(CreateRotationMatrixY(A));
        }
        public static RawMatrix RotateZ(this RawMatrix M, AngleSingle A) {
            return M.Multiply(CreateRotationMatrixZ(A));
        }

        #endregion
        #region Scalation

        public static RawMatrix CreateScaleMatrix(float S) {
            return new RawMatrix() {
                M11 = S,
                M22 = S,
                M33 = S,
                M44 = S
            };
        }
        public static RawMatrix CreateScaleMatrix(Vector2 S) {
            return new RawMatrix() {
                M11 = S.X,
                M22 = S.Y,
                M33 = 1,
                M44 = 1
            };
        }
        public static RawMatrix CreateScaleMatrix(Vector3 S) {
            return new RawMatrix() {
                M11 = S.X,
                M22 = S.Y,
                M33 = S.Z,
                M44 = 1
            };
        }
        public static RawMatrix CreateScaleMatrix(Vector4 S) {
            return new RawMatrix() {
                M11 = S.X,
                M22 = S.Y,
                M33 = S.Z,
                M44 = S.W
            };
        }

        public static RawMatrix Scale(this RawMatrix M, float S) {
            return M.Multiply(CreateScaleMatrix(S));
        }
        public static RawMatrix Scale(this RawMatrix M, Vector2 S) {
           return M.Multiply(CreateScaleMatrix(S));
        }
        public static RawMatrix Scale(this RawMatrix M, Vector3 S) {
            return M.Multiply(CreateScaleMatrix(S));
        }
        public static RawMatrix Scale(this RawMatrix M, Vector4 S) {
            return M.Multiply(CreateScaleMatrix(S));
        }

        #endregion
        #region Translation

        public static RawMatrix CreateTranslationMatrix(float V) {
            return new RawMatrix() {
                M11 = 1,  /*M21 = 0,    M31 = 0,  */M41 = V,
              /*M12 = 0,*/  M22 = 1,  /*M32 = 0,  */M42 = V,
              /*M13 = 0,    M23 = 0,*/  M33 = 1,    M43 = V,
              /*M14 = 0,    M24 = 0,    M34 = 0,  */M44 = V
            };
        }
        public static RawMatrix CreateTranslationMatrix(Vector3 V) {
            return new RawMatrix() {
                M11 = 1,  /*M21 = 0,    M31 = 0,*/  M41 = V.X,
              /*M12 = 0,*/  M22 = 1,  /*M32 = 0,*/  M42 = V.Y,
              /*M13 = 0,    M23 = 0,*/  M33 = 1,    M43 = V.Z,
              /*M14 = 0,    M24 = 0,    M34 = 0,*/  M44 = 1
            };
        }
        public static RawMatrix CreateTranslationMatrix(Vector4 V) {
            return new RawMatrix() {
                M11 = 1,  /*M21 = 0,    M31 = 0,*/  M41 = V.X,
              /*M12 = 0,*/  M22 = 1,  /*M32 = 0,*/  M42 = V.Y,
              /*M13 = 0,    M23 = 0,*/  M33 = 1,    M43 = V.Z,
              /*M14 = 0,    M24 = 0,    M34 = 0,*/  M44 = V.W
            };
        }
        public static RawMatrix CreateTranslationMatrix(Vector2 V) {
            return new RawMatrix() {
                M11 = 1,  /*M21 = 0,    M31 = 0,*/  M41 = V.X,
              /*M12 = 0,*/  M22 = 1,  /*M32 = 0,*/  M42 = V.Y,
              /*M13 = 0,    M23 = 0,*/  M33 = 1,    M43 = 0,
              /*M14 = 0,    M24 = 0,    M34 = 0,*/  M44 = 1
            };
        }

        public static RawMatrix Translate(this RawMatrix M, float V) {
            return M.Multiply(CreateTranslationMatrix(V));
        }
        public static RawMatrix Translate(this RawMatrix M, Vector2 V) {
            return M.Multiply(CreateTranslationMatrix(V));
        }
        public static RawMatrix Translate(this RawMatrix M, Vector3 V) {
            return M.Multiply(CreateTranslationMatrix(V));
        }
        public static RawMatrix Translate(this RawMatrix M, Vector4 V) {
            return M.Multiply(CreateTranslationMatrix(V));
        }

        #endregion

        #region Oparators
        
        public static RawMatrix Add(this RawMatrix M1, RawMatrix M2) {
            Multiply(M1, M2);
            return new RawMatrix() {
                M11 = M1.M11 + M2.M11,
                M12 = M1.M12 + M2.M12,
                M13 = M1.M13 + M2.M13,
                M14 = M1.M14 + M2.M14,
                M21 = M1.M21 + M2.M21,
                M22 = M1.M22 + M2.M22,
                M23 = M1.M23 + M2.M23,
                M24 = M1.M24 + M2.M24,
                M31 = M1.M31 + M2.M31,
                M32 = M1.M32 + M2.M32,
                M33 = M1.M33 + M2.M33,
                M34 = M1.M34 + M2.M34,
                M41 = M1.M41 + M2.M41,
                M42 = M1.M42 + M2.M42,
                M43 = M1.M43 + M2.M43,
                M44 = M1.M44 + M2.M44,
            };
        }

        public static RawMatrix Multiply(this RawMatrix M1, RawMatrix M2) {
            return new RawMatrix() {
                M11 = M1.M11 * M2.M11 + M1.M12 * M2.M21 + M1.M13 * M2.M31 + M1.M14 * M2.M41,
                M21 = M1.M21 * M2.M11 + M1.M22 * M2.M21 + M1.M23 * M2.M31 + M1.M24 * M2.M41,
                M31 = M1.M31 * M2.M11 + M1.M32 * M2.M21 + M1.M33 * M2.M31 + M1.M34 * M2.M41,
                M41 = M1.M41 * M2.M11 + M1.M42 * M2.M21 + M1.M43 * M2.M31 + M1.M44 * M2.M41,
                M12 = M1.M11 * M2.M12 + M1.M12 * M2.M22 + M1.M13 * M2.M32 + M1.M14 * M2.M42,
                M22 = M1.M21 * M2.M12 + M1.M22 * M2.M22 + M1.M23 * M2.M32 + M1.M24 * M2.M42,
                M32 = M1.M31 * M2.M12 + M1.M32 * M2.M22 + M1.M33 * M2.M32 + M1.M34 * M2.M42,
                M42 = M1.M41 * M2.M12 + M1.M42 * M2.M22 + M1.M43 * M2.M32 + M1.M44 * M2.M42,
                M13 = M1.M11 * M2.M13 + M1.M12 * M2.M23 + M1.M13 * M2.M33 + M1.M14 * M2.M43,
                M23 = M1.M21 * M2.M13 + M1.M22 * M2.M23 + M1.M23 * M2.M33 + M1.M24 * M2.M43,
                M33 = M1.M31 * M2.M13 + M1.M32 * M2.M23 + M1.M33 * M2.M33 + M1.M34 * M2.M43,
                M43 = M1.M41 * M2.M13 + M1.M42 * M2.M23 + M1.M43 * M2.M33 + M1.M44 * M2.M43,
                M14 = M1.M11 * M2.M14 + M1.M12 * M2.M24 + M1.M13 * M2.M34 + M1.M14 * M2.M44,
                M24 = M1.M21 * M2.M14 + M1.M22 * M2.M24 + M1.M23 * M2.M34 + M1.M24 * M2.M44,
                M34 = M1.M31 * M2.M14 + M1.M32 * M2.M24 + M1.M33 * M2.M34 + M1.M34 * M2.M44,
                M44 = M1.M41 * M2.M14 + M1.M42 * M2.M24 + M1.M43 * M2.M34 + M1.M44 * M2.M44,
            };
        }

        #endregion

        #endregion
    }
}
