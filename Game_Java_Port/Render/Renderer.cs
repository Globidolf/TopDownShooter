using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using Game_Java_Port.Interface;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D11.Device;
using SharpDX.D3DCompiler;

namespace Game_Java_Port {
    public static class Renderer {
        private static List<IDisposable> disposables = new List<IDisposable>();
        
        private static DeviceContext deviceContext;
		private static Device device;
		private static DepthStencilView depthStencilView;
		private static RenderTargetView renderTargetView;

		private static List<IRenderable> Renderables = new List<IRenderable>();
        private static List<RenderData> RenderDataList = new List<RenderData>();
		private static List<SharpDX.Direct3D11.Buffer> VertexBufferList = new List<SharpDX.Direct3D11.Buffer>();
		private static List<SharpDX.Direct3D11.Buffer> IndexBufferList = new List<SharpDX.Direct3D11.Buffer>();

		private static Vector4 worldView2D;
		private static SharpDX.Direct3D11.Buffer constantBuffer;

		public static void init(Device device, DeviceContext context, SwapChain swapChain, bool noalpha = false) {
			unload();
			deviceContext = context;
			Renderer.device = device;
			// START

			var swapChainFullScreenDescription = new SwapChainFullScreenDescription
			{
				RefreshRate = new Rational(60, 1),
				Scaling = DisplayModeScaling.Centered,
				Windowed = true
			};
			var samplerStateDescription = new SamplerStateDescription
			{
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = TextureAddressMode.Wrap,
				Filter = Filter.MinMagMipLinear
			};

			var rasterizerStateDescription = RasterizerStateDescription.Default();
			rasterizerStateDescription.CullMode = CullMode.None;
			rasterizerStateDescription.IsFrontCounterClockwise = false;
			
            var depthBufferDescription = new Texture2DDescription {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = Program.width,
                Height = Program.height,
                SampleDescription = swapChain.Description.SampleDescription,
                BindFlags = BindFlags.DepthStencil,
            };
			
            var depthStencilViewDescription = new DepthStencilViewDescription {
                Dimension = swapChain.Description.SampleDescription.Count > 1 || swapChain.Description.SampleDescription.Quality > 0
                    ? DepthStencilViewDimension.Texture2DMultisampled
                    : DepthStencilViewDimension.Texture2D
            };
			
            var depthStencilStateDescription = new DepthStencilStateDescription {
                IsDepthEnabled = noalpha,
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.All,
                IsStencilEnabled = false,
                StencilReadMask = 0xff,
                StencilWriteMask = 0xff,
            };
			
            var blendStateDescription = new BlendStateDescription();
            blendStateDescription.RenderTarget[0] = new RenderTargetBlendDescription() {
                IsBlendEnabled = true,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                BlendOperation = BlendOperation.Add,
                SourceAlphaBlend = BlendOption.Zero,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All,
            };
			var vertexShaderBytecode = ShaderBytecode.CompileFromFile("Render/shaders.hlsl", "VSMain2D", "vs_5_0", ShaderFlags.Debug);
			var vertexShader = new VertexShader(device, vertexShaderBytecode);
			var pixelShaderBytecode = ShaderBytecode.CompileFromFile("Render/shaders.hlsl", "PSMain", "ps_5_0", ShaderFlags.Debug);
			var pixelShader = new PixelShader(device, pixelShaderBytecode);
			var inputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderBytecode), new[]
			{
					new InputElement("SV_Position", 0, Format.R32G32B32A32_Float, 0, 0),
					new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0),
					new InputElement("TEXCOORD", 0, Format.R32G32_Float, 32, 0),
				});
			var samplerState = new SamplerState(device, samplerStateDescription);
			var rasterizerState = new RasterizerState(device, rasterizerStateDescription);
			var depthBuffer = new Texture2D(device, depthBufferDescription);
			depthStencilView = new DepthStencilView(device, depthBuffer, depthStencilViewDescription);
			var depthStencilState = new DepthStencilState(device, depthStencilStateDescription);
            renderTargetView = new RenderTargetView(device, SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0));
			constantBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref worldView2D);
			var blendState = new BlendState(device, blendStateDescription);

			

            context.Rasterizer.SetViewport(0,0,Program.width, Program.height);
            context.Rasterizer.State = rasterizerState;
			
            context.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);
            context.OutputMerger.DepthStencilState = depthStencilState;
			context.OutputMerger.BlendState = blendState;
            context.InputAssembler.InputLayout = inputLayout;
			context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.VertexShader.SetConstantBuffer(0, constantBuffer);
            context.VertexShader.Set(vertexShader);
            context.PixelShader.SetConstantBuffer(0, constantBuffer);
            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetSampler(0, samplerState);

			disposables.Add(vertexShaderBytecode);
			disposables.Add(vertexShader);
			disposables.Add(pixelShaderBytecode);
			disposables.Add(pixelShader);
			disposables.Add(inputLayout);
			disposables.Add(samplerState);
			disposables.Add(rasterizerState);
			disposables.Add(depthBuffer);
			disposables.Add(depthStencilView);
			disposables.Add(depthStencilState);
			disposables.Add(renderTargetView);
			disposables.Add(constantBuffer);
			disposables.Add(blendState);
		}

		public static void add(IRenderable rend) {
			Renderables.Add(rend);
			add(rend.RenderData);
		}

		private static void add(RenderData rd) {
			//add object itself and recursively all of its children
			RenderDataList.Add(rd);
			VertexBufferList.Add(rd.mdl.CreateVertexBuffer(device, BindFlags.VertexBuffer));
			IndexBufferList.Add(rd.mdl.CreateIndexBuffer(device, BindFlags.IndexBuffer));
			if (rd.SubObjs != null)
				foreach (RenderData rd2 in rd.SubObjs)
					add(rd2);
		}
		public static void remove(IRenderable rend) {
			Renderables.Remove(rend);
			remove(rend.RenderData);
		}
		private static void remove(RenderData rd) {
			int i = RenderDataList.IndexOf(rd);
			VertexBufferList[i].Dispose();
			IndexBufferList[i].Dispose();
			VertexBufferList.RemoveAt(i);
			IndexBufferList.RemoveAt(i);
			RenderDataList.Remove(rd);
			if (rd.SubObjs != null)
				foreach (RenderData rd2 in rd.SubObjs)
					remove(rd2);
		}
		public static void clear() {
			Renderables.Clear();
			RenderDataList.Clear();
		}

        public static void unload() {
            disposables.ForEach(d => d.Dispose());
            disposables.Clear();
        }

		public static void draw() {
			deviceContext.ClearRenderTargetView(renderTargetView, Color.Transparent);
			deviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1f, 0);
			int ResID = -1;
			var array = RenderDataList.FindAll(r => r.mdl.IndexBuffer != null && r.mdl.VertexBuffer != null).OrderBy(r => -r.mdl.VertexBuffer[0].Pos.Z).ToArray();

			foreach (var r in array) {
				int i = RenderDataList.IndexOf(r);
				if (r.ResID != ResID) {
					ResID = r.ResID;
					deviceContext.PixelShader.SetShaderResource(0, dataLoader.ShaderData[ResID]);
				}
					VertexBufferBinding vbb = new VertexBufferBinding(VertexBufferList[i], Utilities.SizeOf<Vertex>(), 0);
					deviceContext.InputAssembler.SetVertexBuffers(0, vbb);
					deviceContext.InputAssembler.SetIndexBuffer(IndexBufferList[i], Format.R32_UInt, 0);
					deviceContext.DrawIndexed(r.mdl.IndexBuffer.Length * 3, 0, 0);

			}
		}

		public static void drawNoTransparencyPLZ() {
			
            List<RenderData> allData = new List<RenderData>(RenderDataList.FindAll(r => r.mdl.IndexBuffer != null && r.mdl.VertexBuffer != null));
			//deviceContext.ClearRenderTargetView(renderTargetView, Color.Transparent);
			deviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1f, 0);
			
            for(int ResID = allData.Min(r => r.ResID); ResID <= allData.Max(r => r.ResID); ResID++) {
                if(allData.Any(r => r.ResID == ResID)){
                    deviceContext.PixelShader.SetShaderResource(0, dataLoader.ShaderData[ResID]);
                    allData.FindAll(r => r.ResID == ResID).ForEach(r => {
                        using(var indexbuffer = r.mdl.CreateIndexBuffer(device, BindFlags.IndexBuffer))
                        using(var vertexbuffer = r.mdl.CreateVertexBuffer(device, BindFlags.VertexBuffer)) {
                            VertexBufferBinding vbb = new VertexBufferBinding(vertexbuffer, Utilities.SizeOf<Vertex>(), 0);
                            deviceContext.InputAssembler.SetVertexBuffers(0, vbb);
                            deviceContext.InputAssembler.SetIndexBuffer(indexbuffer, Format.R32_UInt, 0);
                            deviceContext.DrawIndexed(r.mdl.IndexBuffer.Length * 3, 0, 0);
                        }
                    });
                }
            }
		}

        public static void updatePositions() {
			worldView2D.X = MatrixExtensions.PVTranslation.X;
			worldView2D.Y = MatrixExtensions.PVTranslation.Y;
			worldView2D.Z = Program.width;
			worldView2D.W = Program.height;
			deviceContext.UpdateSubresource(ref worldView2D, constantBuffer);
			Renderables.ForEach(r => r.updateRenderData());
			for(int i = 0 ; i < RenderDataList.Count ; i++) {
				RenderDataList[i].
			}
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
        public static Vertex[] FromRectangle(RectangleF R) { return SquareBuffer.MultiplyPos(R.Size).TranslatePos(R.Location /* new Vector2(1, -1)*/); }

		// RGBA are each set to 1 (max) which represents a solid white color (0xffffffff) or (0xffff).
        public static readonly Vector4 DefaultColor = new Vector4(1);

		// Base positions are 0.5, not 1 as the difference of -0.5 and 0.5 is 1, which should represent one pixel.
        public static Vertex TopLeft { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(-0.5f, -0.5f, 0, 1), Tex = new Vector2(0, 0) }; } }
        public static Vertex TopRight { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(0.5f, -0.5f, 0, 1), Tex = new Vector2(1, 0) }; } }
        public static Vertex BottomLeft { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(-0.5f, 0.5f, 0, 1), Tex = new Vector2(0, 1) }; } }
        public static Vertex BottomRight { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(0.5f, 0.5f, 0, 1), Tex = new Vector2(1, 1) }; } }
    }

    /// <summary>
    /// Extension methods for Vertices and Vectors. Multiplications, Transformations and such.
    /// </summary>
    public static class VertexExtensions {

        //modifier methods
        public static Vertex[] ApplyRectangle(this Vertex[] V, RectangleF R) {
            Vertex[] temp = Vertex.FromRectangle(R);
            for(int i = 0; i < V.Length; i++) {
				temp[i].Pos.Z = V[i].Pos.Z;
				temp[i].Pos.W = V[i].Pos.W;
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
		public static Vertex[] ApplyTextureRepetition(this Vertex[] V, Vector2 RepetitionCount) {
			Vertex[] temp = V;

			for (int i = 0 ; i < V.Length ; i++)
				temp[i].Tex = temp[i].Tex / RepetitionCount;
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
        private static readonly TriIndex Sq_B = new TriIndex { A = 0, B = 3, C = 1 };
    }
}
