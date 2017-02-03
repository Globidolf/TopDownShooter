using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;

namespace Game_Java_Port {
    public static class Renderer {
        private static List<IDisposable> disposables = new List<IDisposable>();
        
        private static DeviceContext Context;

        private static List<RenderData> renderables = new List<RenderData>();

        public static void init() {
            unload();
            
        }

        public static void unload() {
            disposables.ForEach(d => d.Dispose());
            disposables.Clear();
        }

        public static void draw(Device device) {
            List<RenderData> allData = new List<RenderData>(renderables);
            renderables.FindAll(r => r.SubObjs != null && r.SubObjs.Length > 0).ForEach(r => allData.AddRange(r.SubObjs));

            for(int ResID = allData.Min(r => r.ResID); ResID <= allData.Max(r => r.ResID); ResID++) {
                if(allData.Any(r => r.ResID == ResID)){
                    Context.PixelShader.SetShaderResource(0, dataLoader.ShaderData[ResID]);
                    allData.FindAll(r => r.ResID == ResID).ForEach(r => {
                        using(var indexbuffer = r.mdl.CreateIndexBuffer(device, BindFlags.IndexBuffer))
                        using(var vertexbuffer = r.mdl.CreateVertexBuffer(device, BindFlags.VertexBuffer)) {
                            VertexBufferBinding vbb = new VertexBufferBinding(vertexbuffer, Utilities.SizeOf<Vertex>(), 0);
                            Context.InputAssembler.SetVertexBuffers(0, vbb);
                            Context.InputAssembler.SetIndexBuffer(indexbuffer, SharpDX.DXGI.Format.R32_UInt, 0);
                            Context.DrawIndexed(r.mdl.IndexBuffer.Length * 3, 0, 0);
                        }
                    });
                }
            }
        }

        public static void update() {

        }
        

    }

    public class RenderData {
        public Model mdl;
        public int ResID;

        public RenderData[] SubObjs;

        //animation
        public Point AnimationFrameCount;
        public int[] AnimationIndices;
        public float AnimationOffset;
        public float AnimationSpeed;

        public RectangleF Area { set {
                mdl.VertexBuffer = mdl.VertexBuffer.ApplyRectangle(value);
            } }

    }

    public struct Model {
        public Vertex[] VertexBuffer;
        public TriIndex[] IndexBuffer;

        public static Model Quadrilateral(Vector2 TL, Vector2 TR, Vector2 BL, Vector2 BR) {
            return new Model { IndexBuffer = TriIndex.QuadIndex, VertexBuffer = new[] { new Vertex { } } };
        }

        public static Model Square { get { return new Model { VertexBuffer = Vertex.SquareBuffer, IndexBuffer = TriIndex.QuadIndex }; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex {
        /// <summary>
        /// Position of the vertex. in XYZW. Z defines if it is rendered on top of the other objects or behind. W is unused.
        /// </summary>
        public Vector4 Pos;
        /// <summary>
        /// Color of the vertex. in RGBA.
        /// </summary>
        public Vector4 Color;
        /// <summary>
        /// Texture position of the Vertex in normalized XY
        /// </summary>
        public Vector2 Tex;

        /// <summary>
        /// Returns a vertex buffer defining a 1x1 pixel square
        /// </summary>
        public static Vertex[] SquareBuffer { get { return new[] {TopLeft, TopRight, BottomLeft, BottomRight }; } }

        #region operators

        public static Vertex operator /(Vertex V, float div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() / div, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator /(Vertex V, Point div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() / div, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator /(Vertex V, Vector2 div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() / div, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator /(Vertex V, Size2F div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Divide(div), V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator *(Vertex V, float mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() * mult, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator *(Vertex V, Vector2 mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() * mult, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator *(Vertex V, Point mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() * mult, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator *(Vertex V, Size2F mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Multiply(mult), V.Pos.Z, V.Pos.W), Tex = V.Tex }; }

        public static Vertex operator -(Vertex V, float sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() - sub, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator -(Vertex V, Point sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() - sub, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator -(Vertex V, Vector2 sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() - sub, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator -(Vertex V, Size2F sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Subtract(sub), V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator +(Vertex V, float add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() + add, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator +(Vertex V, Point add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() + add, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator +(Vertex V, Vector2 add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() + add, V.Pos.Z, V.Pos.W), Tex = V.Tex }; }
        public static Vertex operator +(Vertex V, Size2F add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Add(add), V.Pos.Z, V.Pos.W), Tex = V.Tex }; }

        #endregion

        /// <summary>
        /// Create a basic vertex buffer to form a Rectangle
        /// </summary>
        /// <param name="R">Source Rectangle</param>
        /// <returns>Returns an array of vertices</returns>
        public static Vertex[] FromRectangle(RectangleF R) { return SquareBuffer.MultiplyPos(R.Size).TranslatePos(R.Location); }
        public static readonly Vector4 DefaultColor = new Vector4(1);
        public static Vertex TopLeft { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(-1, -1, 0, 1), Tex = new Vector2(0, 0) }; } }
        public static Vertex TopRight { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(1, -1, 0, 1), Tex = new Vector2(1, 0) }; } }
        public static Vertex BottomLeft { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(-1, 1, 0, 1), Tex = new Vector2(0, 1) }; } }
        public static Vertex BottomRight { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(-1, -1, 0, 1), Tex = new Vector2(1, 1) }; } }
    }

    /// <summary>
    /// Extension methods for Vertices and Vectors. Multiplications, Transformations and such.
    /// </summary>
    public static class VertexExtensions {

        //modifier methods
        public static Vertex[] ApplyRectangle(this Vertex[] V, RectangleF R) {
            Vertex[] temp = Vertex.FromRectangle(R);
            for(int i = 0; i < V.Length; i++) {
                temp[i].Color = V[i].Color;
                temp[i].Tex = V[i].Tex;
            }
            return temp;
        }
		public static Vertex[] ApplyPositions(this Vertex[] V, Vector2 TL, Vector2 TR, Vector2 BL, Vector2 BR) {
			Vertex[] temp = V;
			temp[0].Pos = new Vector4(TL, temp[0].Pos.Z, temp[0].Pos.W);
			temp[1].Pos = new Vector4(TR, temp[1].Pos.Z, temp[1].Pos.W);
			temp[2].Pos = new Vector4(BR, temp[2].Pos.Z, temp[2].Pos.W);
			temp[3].Pos = new Vector4(BL, temp[3].Pos.Z, temp[3].Pos.W);
			return temp;
		}
        public static Vertex[] ApplyZAxis(this Vertex[] V, float Z) {
            Vertex[] temp = V;
            for(int i = 0; i < V.Length; i++)
                temp[i].Pos.Z = Z;
            return temp;
        }
        public static Vertex[] ApplyRotation(this Vertex[] V, Vector3 Direction, float Rotation) {
            Vertex[] temp = V;
            for(int i = 0; i < V.Length; i++)
                temp[i].Pos = Vector4.Transform(temp[i].Pos, new Quaternion(Direction, Rotation));
            return temp;
        }

        public static Vertex[] ApplyColor(this Vertex[] V, Color C) { return V.ApplyColor(C.ToVector4()); }

        public static Vertex[] ApplyColor(this Vertex[] V, Vector4 C) {
            Vertex[] temp = V;
            for(int i = 0; i < V.Length; i++)
                temp[i].Color = C;
            return temp;
        }

        //animation methods
        public static Vertex[] SetAnimationFrame(this Vertex[] V, int index, Point AnimationFrameCount) {
            int x = index % AnimationFrameCount.X;
            int y = (index / AnimationFrameCount.X) % AnimationFrameCount.Y; // % count.Y: wraps around the animation to start anew

            return V.MultiplyTex(AnimationFrameCount).TranslatePos(new Vector2(x,y));
        }

        //operator methods
        public static Vertex TranslateTex(this Vertex V, float add) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex + add }; }
        public static Vertex TranslateTex(this Vertex V, Point add) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex + add }; }
        public static Vertex TranslateTex(this Vertex V, Vector2 add ) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex + add  }; }
        public static Vertex TranslateTex(this Vertex V, Size2F add) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex.Add(add) }; }
        public static Vertex MultiplyTex (this Vertex V, float mult  ) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex * mult }; }
        public static Vertex MultiplyTex(this Vertex V, Point mult) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex * mult }; }
        public static Vertex MultiplyTex (this Vertex V, Vector2 mult) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex * mult }; }
        public static Vertex MultiplyTex(this Vertex V, Size2F mult) { return new Vertex { Color = V.Color, Pos = V.Pos, Tex = V.Tex.Multiply(mult) }; }

        public static Vertex[] TranslateTex(this Vertex[] buffer, float add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.TranslateTex(add));
            return temp.ToArray();
        }
        public static Vertex[] TranslateTex(this Vertex[] buffer, Point add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.TranslateTex(add));
            return temp.ToArray();
        }
        public static Vertex[] TranslateTex(this Vertex[] buffer, Vector2 add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.TranslateTex(add));
            return temp.ToArray();
        }
        public static Vertex[] TranslateTex(this Vertex[] buffer, Size2F add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.TranslateTex(add));
            return temp.ToArray();
        }
        public static Vertex[] MultiplyTex(this Vertex[] buffer, float mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.MultiplyTex(mult));
            return temp.ToArray();
        }
        public static Vertex[] MultiplyTex(this Vertex[] buffer, Point mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.MultiplyTex(mult));
            return temp.ToArray();
        }
        public static Vertex[] MultiplyTex(this Vertex[] buffer, Vector2 mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.MultiplyTex(mult));
            return temp.ToArray();
        }
        public static Vertex[] MultiplyTex(this Vertex[] buffer, Size2F mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v.MultiplyTex(mult));
            return temp.ToArray();
        }

        public static Vertex[] TranslatePos(this Vertex[] buffer, float add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v+add);
            return temp.ToArray();
        }
        public static Vertex[] TranslatePos(this Vertex[] buffer, Point add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v + add);
            return temp.ToArray();
        }
        public static Vertex[] TranslatePos(this Vertex[] buffer, Vector2 add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v + add);
            return temp.ToArray();
        }
        public static Vertex[] TranslatePos(this Vertex[] buffer, Size2F add) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v + add);
            return temp.ToArray();
        }
        public static Vertex[] MultiplyPos(this Vertex[] buffer, float mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v*mult);
            return temp.ToArray();
        }
        public static Vertex[] MultiplyPos(this Vertex[] buffer, Point mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v * mult);
            return temp.ToArray();
        }
        public static Vertex[] MultiplyPos(this Vertex[] buffer, Vector2 mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v * mult);
            return temp.ToArray();
        }
        public static Vertex[] MultiplyPos(this Vertex[] buffer, Size2F mult) {
            List<Vertex> temp = new List<Vertex>();
            foreach(Vertex v in buffer)
                temp.Add(v * mult);
            return temp.ToArray();
        }

        //HelperExtensions
        public static Vector2 Multiply(this Vector2 V, Size2F R) { return new Vector2(V.X * R.Width, V.Y * R.Height); }
        public static Vector2 Divide(this Vector2 V, Size2F R) { return new Vector2(V.X / R.Width, V.Y / R.Height); }
        public static Vector2 Add(this Vector2 V, Size2F R) { return new Vector2(V.X + R.Width, V.Y + R.Height); }
        public static Vector2 Subtract(this Vector2 V, Size2F R) { return new Vector2(V.X - R.Width, V.Y - R.Height); }

        //Convert V3 to V2
        public static Vector2 XY(this Vector3 v) { return new Vector2(v.X, v.Y); }
        public static Vector2 XZ(this Vector3 v) { return new Vector2(v.X, v.Z); }
        public static Vector2 YZ(this Vector3 v) { return new Vector2(v.Y, v.Z); }

        //Convert V4 to V2
        public static Vector2 XY(this Vector4 v) { return new Vector2(v.X, v.Y); }
        public static Vector2 XZ(this Vector4 v) { return new Vector2(v.X, v.Z); }
        public static Vector2 YZ(this Vector4 v) { return new Vector2(v.Y, v.Z); }

        public static Vector2 XW(this Vector4 v) { return new Vector2(v.X, v.W); }
        public static Vector2 YW(this Vector4 v) { return new Vector2(v.Y, v.W); }
        public static Vector2 ZW(this Vector4 v) { return new Vector2(v.Z, v.W); }

        //Convert V4 to V3
        public static Vector3 XYZ(this Vector4 v) { return new Vector3(v.X, v.Y, v.Z); }
        public static Vector3 XYW(this Vector4 v) { return new Vector3(v.X, v.Y, v.W); }
        public static Vector3 XZW(this Vector4 v) { return new Vector3(v.X, v.Z, v.W); }
        public static Vector3 YZW(this Vector4 v) { return new Vector3(v.Y, v.Z, v.W); }

        //Buffer Creation

        public static SharpDX.Direct3D11.Buffer CreateBuffer<T>( Device device, BindFlags bindFlags, params T[] items) where T : struct {
            var len = Utilities.SizeOf(items);
            var stream = new DataStream(len, true, true);
            foreach(var item in items)
                stream.Write(item);
            stream.Position = 0;
            var buffer = new SharpDX.Direct3D11.Buffer(device, stream, len, ResourceUsage.Default,
                bindFlags, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            stream.Dispose();
            return buffer;
        }
        public static SharpDX.Direct3D11.Buffer CreateBuffer(this Vertex[] item, Device device, BindFlags bindFlags) { return CreateBuffer(device, bindFlags, item); }
        public static SharpDX.Direct3D11.Buffer CreateBuffer(this TriIndex[] item, Device device, BindFlags bindFlags) { return CreateBuffer(device, bindFlags, item); }
        public static SharpDX.Direct3D11.Buffer CreateIndexBuffer(this Model item, Device device, BindFlags bindFlags) { return CreateBuffer(device, bindFlags, item.IndexBuffer); }
        public static SharpDX.Direct3D11.Buffer CreateVertexBuffer(this Model item, Device device, BindFlags bindFlags) { return CreateBuffer(device, bindFlags, item.VertexBuffer); }
    }

    /// <summary>
    /// Structure defining a triangle made of three vertices.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TriIndex {
        /// <summary>
        /// Index of the first vertex of the triangle
        /// </summary>
        public uint A;
        /// <summary>
        /// Index of the second vertex of the triangle
        /// </summary>
        public uint B;
        /// <summary>
        /// Index of the third vertex of the triangle
        /// </summary>
        public uint C;

        //public TriIndex(uint a, uint b, uint c) { A = a; B = b; C = c; }

        /// <summary>
        /// Returns an array containing two triangles which can be used with a 4-Point vertex buffer to form a quadrilateral.
        /// </summary>
        //to prevent mutation new instances have to be created...
        public static TriIndex[] QuadIndex { get { return new[] { Sq_A, Sq_B }; } }

        //First part of a Quad triangle index
        private static readonly TriIndex Sq_A = new TriIndex { A = 0, B = 2, C = 3 };
        //Second part
        private static readonly TriIndex Sq_B = new TriIndex { A = 0, B = 1, C = 2 };
    }
}
