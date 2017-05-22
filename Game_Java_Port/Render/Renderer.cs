using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using Game_Java_Port.Interface;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.D3DCompiler;

namespace Game_Java_Port {
	public static class Renderer {
		#region constants

		/// <summary>
		/// no layer should be above this one
		/// </summary>
		public const float Layer_Cursor = 100;
		/// <summary>
		/// rendered on top of most things
		/// </summary>
		public const float Layer_Menu = 70;
		/// <summary>
		/// Walls, Ceilings, Trees and such
		/// </summary>
		public const float Layer_Map = 50;
		/// <summary>
		/// NPCs, Players and such
		/// </summary>
		public const float Layer_Characters = 30;
		/// <summary>
		/// Bullets go 'trough' characters, to achieve this effect, they're rendered below them
		/// </summary>
		public const float Layer_Bullets = 0;
		/// <summary>
		/// no layer should be below this one
		/// </summary>
		public const float Layer_Background = -100;
		/// <summary>
		/// Render above text on the same level
		/// </summary>
		public const float LayerOffset_Tooltip = 10;
		/// <summary>
		/// To make sure that text is above the content
		/// </summary>
		public const float LayerOffset_Text = 5;
		/// <summary>
		/// For outlines of objects
		/// </summary>
		public const float LayerOffset_Outline = -1;

		#endregion
		#region data
		private static List<IDisposable> disposables = new List<IDisposable>();

		private static DeviceContext deviceContext;
		private static Device device;
		private static DepthStencilView depthStencilView;
		private static RenderTargetView renderTargetView;

		private static List<IRenderable> Renderables = new List<IRenderable>();
		private static List<RenderData> RenderDataList = new List<RenderData>();
		private static List<Buffer> VertexBufferList = new List<Buffer>();
		private static List<Buffer> IndexBufferList = new List<Buffer>();

		private static Vector4 worldView2D;
		private static Buffer constantBuffer;

		private static bool loaded = false;
		#endregion

		public static void init(Device device, DeviceContext context, SwapChain swapChain, bool noalpha = false) {
			if (loaded)
				unload();
			deviceContext = context;
			Renderer.device = device;
			// START
			var samplerStateDescription = new SamplerStateDescription
			{
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp,
				AddressW = TextureAddressMode.Clamp,
				Filter = Filter.MinMagMipPoint
			};

			var rasterizerStateDescription = RasterizerStateDescription.Default();
			rasterizerStateDescription.CullMode = CullMode.Front;

			var depthBufferDescription = new Texture2DDescription
			{
				Format = Format.D32_Float_S8X24_UInt,
				ArraySize = 1,
				MipLevels = 1,
				Width = Program.width,
				Height = Program.height,
				SampleDescription = swapChain.Description.SampleDescription,
				BindFlags = BindFlags.DepthStencil,
			};

			var depthStencilViewDescription = new DepthStencilViewDescription
			{
				Dimension = swapChain.Description.SampleDescription.Count > 1 || swapChain.Description.SampleDescription.Quality > 0
					? DepthStencilViewDimension.Texture2DMultisampled
					: DepthStencilViewDimension.Texture2D
			};

			var depthStencilStateDescription = new DepthStencilStateDescription
			{
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
					new InputElement("SV_Position", 0, Format.R32G32B32A32_Float, 0, 0), // X, Y, Z, ??? no clue sry
					new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0),
					new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 32, 0), // X, Y, Texture1, Texture2
				});
			var samplerState = new SamplerState(device, samplerStateDescription);
			var rasterizerState = new RasterizerState(device, rasterizerStateDescription);
			var depthBuffer = new Texture2D(device, depthBufferDescription);
			depthStencilView = new DepthStencilView(device, depthBuffer, depthStencilViewDescription);
			var depthStencilState = new DepthStencilState(device, depthStencilStateDescription);
			Texture2D res = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0);
			renderTargetView = new RenderTargetView(device, res);
			constantBuffer = Buffer.Create(device, BindFlags.ConstantBuffer, ref worldView2D);
			var blendState = new BlendState(device, blendStateDescription);



			context.Rasterizer.SetViewport(0, 0, Program.width, Program.height);
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
			context.PixelShader.SetShaderResource(0, dataLoader.FontResource);
			context.PixelShader.SetShaderResource(1, dataLoader.ShaderResources);


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
			disposables.Add(res);
			loaded = true;
		}

		public static void add(IRenderable rend) {
			Renderables.Add(rend);
			add(rend.RenderData);
		}
		public static void add(RenderData rd) {
			if (rd == null)
				return;
			//add object itself and recursively all of its children
			RenderDataList.Add(rd);
			IndexBufferList.Add(rd.mdl.CreateIndexBuffer(device, BindFlags.IndexBuffer));
			VertexBufferList.Add(rd.mdl.CreateVertexBuffer(device, BindFlags.VertexBuffer, CpuAccessFlags.Write));
			if (rd.SubObjs != null)
				foreach (RenderData rd2 in rd.SubObjs)
					add(rd2);
		}
		public static void remove(IRenderable rend) {
			Renderables.Remove(rend);
			remove(rend.RenderData);
		}
		public static void remove(RenderData rd) {
			if (rd == null || !RenderDataList.Contains(rd))
				return;
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
			while (Renderables.Count > 0)
				remove(Renderables[0]);
		}

		public static void unload() {
			if (loaded) {
				deviceContext.OutputMerger.ResetTargets();
				disposables.ForEach(d => d.Dispose());
				disposables.Clear();
				loaded = false;
			}
		}

		public static void draw() {
			//deviceContext.ClearRenderTargetView(renderTargetView, Color.Wheat);
			deviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1f, 0);
			var array = RenderDataList.FindAll(r => r.mdl.IndexBuffer != null && r.mdl.VertexBuffer != null).OrderBy(r => r.mdl.VertexBuffer[0].Pos.Z).ToArray();
			foreach (var r in array) {
				int i = RenderDataList.IndexOf(r);
				VertexBufferBinding vbb = new VertexBufferBinding(VertexBufferList[i], Utilities.SizeOf<Vertex>(), 0);
				deviceContext.InputAssembler.SetVertexBuffers(0, vbb);
				deviceContext.InputAssembler.SetIndexBuffer(IndexBufferList[i], Format.R32_UInt, 0);
				deviceContext.DrawIndexed(r.mdl.IndexBuffer.Length * 3, 0, 0);
			}
		}

		public static void updatePositions() {
			double time = GameStatus.CurrentTick * 1000;
			worldView2D.X = MatrixExtensions.PVTranslation.X;
			worldView2D.Y = MatrixExtensions.PVTranslation.Y;
			worldView2D.Z = Program.width;
			worldView2D.W = Program.height;
			deviceContext.UpdateSubresource(ref worldView2D, constantBuffer);

			for (int i = 0 ; i < RenderDataList.Count ; i++) {
				//Animate?
				if (RenderDataList[i].animate) {
					int animationframe = (int)
						((RenderDataList[i].AnimationFrameCount.X * RenderDataList[i].AnimationFrameCount.Y + 1) *
						(RenderDataList[i].AnimationSpeed > 0 ?
						((time + RenderDataList[i].AnimationOffset) % RenderDataList[i].AnimationSpeed) / RenderDataList[i].AnimationSpeed :
						(time + RenderDataList[i].AnimationOffset) % 1));
					RenderDataList[i].mdl.VertexBuffer.SetAnimationFrame(animationframe, RenderDataList[i].AnimationFrameCount);
				}
				DataStream temp;
				deviceContext.MapSubresource(VertexBufferList[i], MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out temp);
				temp.Position = 0;
				foreach (Vertex v in RenderDataList[i].mdl.VertexBuffer)
					temp.Write(v);
				deviceContext.UnmapSubresource(VertexBufferList[i], 0);
			}
		}
	}

	public class RenderData {
		public bool animate;
		public Model mdl;
		private int _ResID = 0;
		public int ResID {
			get { return _ResID; }
			set {
				_ResID = value;
				if (mdl.VertexBuffer != null) {
					for (int i = 0 ; i < mdl.VertexBuffer.Length ; i++)
						mdl.VertexBuffer[i].Resource = value;
				}
			}
		}
		private int _ResID2 = -1;
		public int ResID2 {
			get { return _ResID2; }
			set {
				_ResID2 = value;
				if (mdl.VertexBuffer != null) {
					for (int i = 0 ; i < mdl.VertexBuffer.Length ; i++)
						mdl.VertexBuffer[i].Resource2 = value;
				}
			}
		}
		public RenderData[] SubObjs;

		//animation
		public Point AnimationFrameCount;
		public int[] AnimationIndices;
		public float AnimationOffset;
		public float AnimationSpeed;

		//utility setters
		public Rectangle Area {
			set {
				if (mdl.VertexBuffer == null)
					mdl = Model.Square;
				mdl.VertexBuffer.ApplyRectangle(value);
				ResID = _ResID;
				ResID2 = _ResID2;
			}
		}
		public float Z {
			set {
				if (mdl.VertexBuffer == null)
					mdl = Model.Square;
				mdl.VertexBuffer = mdl.VertexBuffer.ApplyZAxis(value);
			}
		}
		/// <summary>
		/// Only set after the Model has been set directly, or inderectly via Area, AND after the animationFrameCount has been set.
		/// will apply the new texture coordinates to the internal vertexbuffer to display the new region of the texture.
		/// </summary>
		public int Frameindex { set { mdl.VertexBuffer.SetAnimationFrame(value, AnimationFrameCount); } }

		public Color Color { set { mdl.VertexBuffer.ApplyColor(value); } }

		public AngleSingle XRotation { set { mdl.VertexBuffer.ApplyRotation(Vector3.UnitX, value.Radians); } }
		public AngleSingle YRotation { set { mdl.VertexBuffer.ApplyRotation(Vector3.UnitY, value.Radians); } }
		public AngleSingle ZRotation { set { mdl.VertexBuffer.ApplyRotation(Vector3.UnitZ, value.Radians); } }
		public void ApplyRotation(Vector3 Direction, AngleSingle Rotation) { mdl.VertexBuffer.ApplyRotation(Direction, Rotation.Radians); }
		public void ApplyRotation(AngleSingle Rotation) { ZRotation = Rotation; }
		public void ApplyRotation(Vector3 Direction, float Rotation) { mdl.VertexBuffer.ApplyRotation(Direction, Rotation); }
		public void ApplyRotation(float Rotation) { mdl.VertexBuffer.ApplyRotation(Vector3.UnitZ, Rotation); }

		public Vector2 TextureRepetition { set { mdl.VertexBuffer.ApplyTextureRepetition(value); } }

		/// <summary>
		/// Copies all values of this object to a new instance. Changes to the new instance are guaranteed to not change this object.
		/// </summary>
		/// <returns>Return an independent copy of this object</returns>
		public RenderData ValueCopy() {
			RenderData result = new RenderData
			{
				AnimationFrameCount = AnimationFrameCount,
				AnimationIndices = AnimationIndices,
				AnimationOffset = AnimationOffset,
				AnimationSpeed = AnimationSpeed,
				mdl = mdl.ValueCopy(),
				ResID = ResID,
				ResID2 = ResID2,
				SubObjs = SubObjs == null ? null : new RenderData[SubObjs.Length]
			};
			if (result.SubObjs != null)
				for (int i = 0 ; i < SubObjs.Length ; i++)
					result.SubObjs[i] = SubObjs[i].ValueCopy();
			return result;
		}
	}

	public struct Model {
		public Vertex[] VertexBuffer;
		public TriIndex[] IndexBuffer;

		public static Model Quadrilateral(Vector2 TL, Vector2 TR, Vector2 BL, Vector2 BR) {
			return new Model { IndexBuffer = TriIndex.QuadIndex, VertexBuffer = new[] { new Vertex { } } };
		}

		public static Model Square { get { return new Model { VertexBuffer = Vertex.SquareBuffer, IndexBuffer = TriIndex.QuadIndex }; } }
		public Model ValueCopy() {
			Model result = new Model();
			result.IndexBuffer = new TriIndex[IndexBuffer.Length];
			result.VertexBuffer = new Vertex[VertexBuffer.Length];
			for (int i = 0 ; i < IndexBuffer.Length ; i++)
				result.IndexBuffer[i] = IndexBuffer[i];
			for (int i = 0 ; i < VertexBuffer.Length ; i++)
				result.VertexBuffer[i] = VertexBuffer[i];
			return result;
		}
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
		public Vector2 TexCoord { get { return Tex.XY(); } set { Tex.X = value.X; Tex.Y = value.Y; } }
		public float Resource { get { return Tex.Z; } set { Tex.Z = value; } }
		public float Resource2 { get { return Tex.W; } set { Tex.W = value; } }
		public Vector4 Tex;
		/// <summary>
		/// Returns a vertex buffer defining a 1x1 pixel square
		/// </summary>
		public static Vertex[] SquareBuffer { get { return new[] { TopLeft, TopRight, BottomLeft, BottomRight }; } }

		#region operators

		public static Vertex operator /(Vertex V, float div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() / div, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator /(Vertex V, Point div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() / div, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator /(Vertex V, Vector2 div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() / div, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator /(Vertex V, Size2F div) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Divide(div), V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator *(Vertex V, float mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() * mult, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator *(Vertex V, Vector2 mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() * mult, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator *(Vertex V, Point mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() * mult, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator *(Vertex V, Size2F mult) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Multiply(mult), V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }


		public static Vertex operator -(Vertex V, float sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() - sub, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator -(Vertex V, Point sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() - sub, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator -(Vertex V, Vector2 sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() - sub, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator -(Vertex V, Size2F sub) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Subtract(sub), V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator +(Vertex V, float add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() + add, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator +(Vertex V, Point add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() + add, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator +(Vertex V, Vector2 add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY() + add, V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }
		public static Vertex operator +(Vertex V, Size2F add) { return new Vertex { Color = V.Color, Pos = new Vector4(V.Pos.XY().Add(add), V.Pos.Z, V.Pos.W), TexCoord = V.TexCoord }; }

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
		public static Vertex TopLeft { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(-0.5f, -0.5f, 0, 1), Tex = new Vector4(0, 0, dataLoader.getResID(), -1) }; } }
		public static Vertex TopRight { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(0.5f, -0.5f, 0, 1), Tex = new Vector4(1, 0, dataLoader.getResID(), -1) }; } }
		public static Vertex BottomLeft { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(-0.5f, 0.5f, 0, 1), Tex = new Vector4(0, 1, dataLoader.getResID(), -1) }; } }
		public static Vertex BottomRight { get { return new Vertex { Color = DefaultColor, Pos = new Vector4(0.5f, 0.5f, 0, 1), Tex = new Vector4(1, 1, dataLoader.getResID(), -1) }; } }
	}

	/// <summary>
	/// Extension methods for Vertices and Vectors. Multiplications, Transformations and such.
	/// </summary>
	public static class VertexExtensions {

		public static RenderData Merge(this List<RenderData> list, int? ResID = null, int? ResID2 = null) { return list.ToArray().Merge(ResID, ResID2); }

		public static RenderData Merge(this RenderData[] list, int? ResID = null, int? ResID2 = null) {
			List<Vertex> vertices = new List<Vertex>();
			List<TriIndex> indices = new List<TriIndex>();
			for (int i = 0 ; i < list.Length ; i++) {
				int[] newindexbuffer = new int[list[i].mdl.IndexBuffer.Length];
				for (int j = 0 ; j < list[i].mdl.VertexBuffer.Length ; j++) {
					if (!vertices.Contains(list[i].mdl.VertexBuffer[j]))
						vertices.Add(list[i].mdl.VertexBuffer[j]);
				}
				for (int j = 0 ; j < list[i].mdl.IndexBuffer.Length ; j++) {
					indices.Add(new TriIndex {
						A = (uint) vertices.IndexOf(list[i].mdl.VertexBuffer[list[i].mdl.IndexBuffer[j].A]),
						B = (uint) vertices.IndexOf(list[i].mdl.VertexBuffer[list[i].mdl.IndexBuffer[j].B]),
						C = (uint) vertices.IndexOf(list[i].mdl.VertexBuffer[list[i].mdl.IndexBuffer[j].C])
					});
				}
			}
			RenderData result = new RenderData {
				mdl = new Model
			{
					VertexBuffer = vertices.ToArray(),
					IndexBuffer = indices.ToArray()
				}
			};
			if (ResID.HasValue)
				result.ResID = ResID.Value;
			if (ResID2.HasValue)
				result.ResID2 = ResID2.Value;
			return result;
		}

		//modifier methods
		public static Vertex[] FromRectangle(this Vertex[] V, RectangleF R) {
			Vertex[] temp = Vertex.FromRectangle(R);
			for (int i = 0 ; i < V.Length ; i++) {
				temp[i].Pos.Z = V[i].Pos.Z;
				temp[i].Pos.W = V[i].Pos.W;
				temp[i].Color = V[i].Color;
				temp[i].TexCoord = V[i].TexCoord;
			}
			return temp;
		}
		public static Vertex[] FromPositions(this Vertex[] V, Vector2 TL, Vector2 TR, Vector2 BL, Vector2 BR) {
			Vertex[] temp = V;
			temp[0].Pos = new Vector4(TL, temp[0].Pos.Z, temp[0].Pos.W);
			temp[1].Pos = new Vector4(TR, temp[1].Pos.Z, temp[1].Pos.W);
			temp[2].Pos = new Vector4(BL, temp[2].Pos.Z, temp[2].Pos.W);
			temp[3].Pos = new Vector4(BR, temp[3].Pos.Z, temp[3].Pos.W);
			return temp;
		}

		public static void ApplyRectangle(this Vertex[] V, RectangleF R) { V.ApplyPositions(R.TopLeft, R.TopRight, R.BottomLeft, R.BottomRight); }
		public static void ApplyRectangle(this Vertex[] V, Rectangle R) {
			V.ApplyPositions(
				new Vector2(R.Left, R.Top),
				new Vector2(R.Right, R.Top),
				new Vector2(R.Left, R.Bottom),
				new Vector2(R.Right, R.Bottom));
		}

		public static void ApplyPositions(this Vertex[] V, Vector2 TL, Vector2 TR, Vector2 BL, Vector2 BR) {
			V[0].Pos = new Vector4(TL, V[0].Pos.Z, V[0].Pos.W);
			V[1].Pos = new Vector4(TR, V[1].Pos.Z, V[1].Pos.W);
			V[2].Pos = new Vector4(BL, V[2].Pos.Z, V[2].Pos.W);
			V[3].Pos = new Vector4(BR, V[3].Pos.Z, V[3].Pos.W);
		}
		public static Vertex[] ApplyZAxis(this Vertex[] V, float Z) {
			Vertex[] temp = V;
			for (int i = 0 ; i < V.Length ; i++)
				temp[i].Pos.Z = Z;
			return temp;
		}
		public static Vertex[] ApplyRotation(this Vertex[] V, Vector3 Direction, float Rotation) {
			Vertex[] temp = V;
			for (int i = 0 ; i < V.Length ; i++)
				temp[i].Pos = Vector4.Transform(temp[i].Pos, new Quaternion(Direction, Rotation));
			return temp;
		}
		public static Vertex[] ApplyTextureRepetition(this Vertex[] V, Vector2 RepetitionCount) {
			Vertex[] temp = V;

			for (int i = 0 ; i < V.Length ; i++)
				temp[i].TexCoord = temp[i].TexCoord / RepetitionCount;
			return temp;
		}

		public static Vertex[] ApplyColor(this Vertex[] V, Color C) { return V.ApplyColor(C.ToVector4()); }

		public static Vertex[] ApplyColor(this Vertex[] V, Vector4 C) {
			Vertex[] temp = V;
			for (int i = 0 ; i < V.Length ; i++)
				temp[i].Color = C;
			return temp;
		}

		//animation methods
		public static void SetAnimationFrame(this Vertex[] V, int index, Point AnimationFrameCount) {
			float i = index % AnimationFrameCount.X;
			float j = (index / AnimationFrameCount.X) % AnimationFrameCount.Y; // % count.Y: wraps around the animation to start anew
			float x1 = i / AnimationFrameCount.X;
			float y1 = j / AnimationFrameCount.Y;
			float x2 = 1f / AnimationFrameCount.X + x1;
			float y2 = 1f / AnimationFrameCount.Y + y1;
			V[0].TexCoord = new Vector2(x1, y1);
			V[1].TexCoord = new Vector2(x2, y1);
			V[2].TexCoord = new Vector2(x1, y2);
			V[3].TexCoord = new Vector2(x2, y2);
		}
		public static Vertex[] GetAnimationFrame(this Vertex[] V, int index, Point AnimationFrameCount) {
			int x = index % AnimationFrameCount.X;
			int y = (index / AnimationFrameCount.X) % AnimationFrameCount.Y; // % count.Y: wraps around the animation to start anew

			return V.MultiplyTex(AnimationFrameCount).TranslateTex(new Vector2(x, y));
		}

		//operator methods
		public static Vertex TranslateTex(this Vertex V, float add) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord + add }; }
		public static Vertex TranslateTex(this Vertex V, Point add) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord + add }; }
		public static Vertex TranslateTex(this Vertex V, Vector2 add) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord + add }; }
		public static Vertex TranslateTex(this Vertex V, Size2F add) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord.Add(add) }; }
		public static Vertex MultiplyTex(this Vertex V, float mult) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord * mult }; }
		public static Vertex MultiplyTex(this Vertex V, Point mult) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord * mult }; }
		public static Vertex MultiplyTex(this Vertex V, Vector2 mult) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord * mult }; }
		public static Vertex MultiplyTex(this Vertex V, Size2F mult) { return new Vertex { Color = V.Color, Pos = V.Pos, TexCoord = V.TexCoord.Multiply(mult) }; }

		public static Vertex[] TranslateTex(this Vertex[] buffer, float add) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.TranslateTex(add));
			return temp.ToArray();
		}
		public static Vertex[] TranslateTex(this Vertex[] buffer, Point add) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.TranslateTex(add));
			return temp.ToArray();
		}
		public static Vertex[] TranslateTex(this Vertex[] buffer, Vector2 add) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.TranslateTex(add));
			return temp.ToArray();
		}
		public static Vertex[] TranslateTex(this Vertex[] buffer, Size2F add) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.TranslateTex(add));
			return temp.ToArray();
		}
		public static Vertex[] MultiplyTex(this Vertex[] buffer, float mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.MultiplyTex(mult));
			return temp.ToArray();
		}
		public static Vertex[] MultiplyTex(this Vertex[] buffer, Point mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.MultiplyTex(mult));
			return temp.ToArray();
		}
		public static Vertex[] MultiplyTex(this Vertex[] buffer, Vector2 mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.MultiplyTex(mult));
			return temp.ToArray();
		}
		public static Vertex[] MultiplyTex(this Vertex[] buffer, Size2F mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v.MultiplyTex(mult));
			return temp.ToArray();
		}

		public static void TranslatePos(this Vertex[] buffer, float add) {
			buffer[0] += add;
			buffer[1] += add;
			buffer[2] += add;
			buffer[3] += add;
		}
		public static Vertex[] TranslatePos(this Vertex[] buffer, Point add) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v + add);
			return temp.ToArray();
		}
		public static Vertex[] TranslatePos(this Vertex[] buffer, Vector2 add) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v + add);
			return temp.ToArray();
		}
		public static Vertex[] TranslatePos(this Vertex[] buffer, Size2F add) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v + add);
			return temp.ToArray();
		}
		public static Vertex[] MultiplyPos(this Vertex[] buffer, float mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v * mult);
			return temp.ToArray();
		}
		public static Vertex[] MultiplyPos(this Vertex[] buffer, Point mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v * mult);
			return temp.ToArray();
		}
		public static Vertex[] MultiplyPos(this Vertex[] buffer, Vector2 mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
				temp.Add(v * mult);
			return temp.ToArray();
		}
		public static Vertex[] MultiplyPos(this Vertex[] buffer, Size2F mult) {
			List<Vertex> temp = new List<Vertex>();
			foreach (Vertex v in buffer)
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

		public static Buffer CreateBuffer<T>(Device device, BindFlags bindFlags, T[] items, CpuAccessFlags cpuaccflags = CpuAccessFlags.None) where T : struct {
			var len = Utilities.SizeOf(items);
			var stream = new DataStream(len, true, true);
			foreach (var item in items)
				stream.Write(item);
			stream.Position = 0;
			var buffer = new Buffer(device, stream, len, cpuaccflags == CpuAccessFlags.None ? ResourceUsage.Default : ResourceUsage.Dynamic,
				bindFlags, cpuaccflags, ResourceOptionFlags.None, 0);
			stream.Dispose();
			return buffer;
		}
		public static Buffer CreateBuffer(this Vertex[] item, Device device, BindFlags bindFlags, CpuAccessFlags cpuaccflags = CpuAccessFlags.None) { return CreateBuffer(device, bindFlags, item, cpuaccflags); }
		public static Buffer CreateBuffer(this TriIndex[] item, Device device, BindFlags bindFlags, CpuAccessFlags cpuaccflags = CpuAccessFlags.None) { return CreateBuffer(device, bindFlags, item, cpuaccflags); }
		public static Buffer CreateIndexBuffer(this Model item, Device device, BindFlags bindFlags, CpuAccessFlags cpuaccflags = CpuAccessFlags.None) { return CreateBuffer(device, bindFlags, item.IndexBuffer, cpuaccflags); }
		public static Buffer CreateVertexBuffer(this Model item, Device device, BindFlags bindFlags, CpuAccessFlags cpuaccflags = CpuAccessFlags.None) { return CreateBuffer(device, bindFlags, item.VertexBuffer, cpuaccflags); }
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