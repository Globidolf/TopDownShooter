using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.WIC;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System.Diagnostics;
using System.IO;

namespace Game_Java_Port {
    public static class CubeApp {
        
        public static void Run() {
            var featureLevels = new[]
            {
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0
            };
            const DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug;

            const int width = 640;
            const int height = 480;

            using(var form = new RenderForm {
                Width = width,
                Height = height
            }) {
                var swapChainDescription = new SwapChainDescription1 {
                    Width = form.ClientSize.Width,
                    Height = form.ClientSize.Height,
                    Format = Format.R8G8B8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                    BufferCount = 1,
                    Scaling = Scaling.Stretch,
                    SwapEffect = SwapEffect.Discard,
                    Flags = SwapChainFlags.AllowModeSwitch
                };
                var swapChainFullScreenDescription = new SwapChainFullScreenDescription {
                    RefreshRate = new Rational(60, 1),
                    Scaling = DisplayModeScaling.Centered,
                    Windowed = true
                };
                var samplerStateDescription = new SamplerStateDescription {
                    AddressU = TextureAddressMode.Wrap,
                    AddressV = TextureAddressMode.Wrap,
                    AddressW = TextureAddressMode.Wrap,
                    Filter = Filter.MinMagMipLinear
                };
                var rasterizerStateDescription = RasterizerStateDescription.Default();
                rasterizerStateDescription.IsFrontCounterClockwise = true;

                // Set up the graphics devices
                using(SharpDX.WIC.ImagingFactory2 imgfactory = new SharpDX.WIC.ImagingFactory2())
                using(var device0 = new SharpDX.Direct3D11.Device(DriverType.Hardware, creationFlags, featureLevels))
                using(var device1 = device0.QueryInterface<SharpDX.Direct3D11.Device1>())
                using(var context = device0.ImmediateContext.QueryInterface<DeviceContext1>())

                // Create shaders and related resources

                using(var vertexShaderBytecode = ShaderBytecode.CompileFromFile("test/shaders.hlsl", "VSMain", "vs_5_0", ShaderFlags.Debug))
                using(var vertexShader = new VertexShader(device1, vertexShaderBytecode))
                using(var pixelShaderBytecode = ShaderBytecode.CompileFromFile("test/shaders.hlsl", "PSMain", "ps_5_0", ShaderFlags.Debug))
                using(var pixelShader = new PixelShader(device1, pixelShaderBytecode))
                using(var inputLayout = new InputLayout(device1, ShaderSignature.GetInputSignature(vertexShaderBytecode), new[]
                {
                    new InputElement("SV_Position", 0, Format.R32G32B32A32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 32, 0),
                }))
                using(var worldViewProjectionBuffer = new SharpDX.Direct3D11.Buffer(device1, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0))
                //using(var bmpsrc = LoadBitmap(imgfactory, "data/img/Pistol.bmp"))
                //using(var tex2d = CreateTexture2DFromBitmap(device0, bmpsrc))
                using(var tex2d = dataLoader.Load(device0, "data/img/Pistol.bmp"))
                using(var textureView = new ShaderResourceView(device0, tex2d,
                    new ShaderResourceViewDescription() {
                        Format = Format.R8G8B8A8_UNorm,
                        Dimension = ShaderResourceViewDimension.Texture2D,
                        Texture2D = new ShaderResourceViewDescription.Texture2DResource() { MipLevels = 1, MostDetailedMip = 0 }
                    }))
                using(var samplerState = new SamplerState(device1, samplerStateDescription))
                // Prepare rendering targets and related resources
                using(var dxgiDevice2 = device1.QueryInterface<SharpDX.DXGI.Device2>())
                using(var dxgiFactory2 = dxgiDevice2.Adapter.GetParent<Factory2>())
                using(var swapChain = new SwapChain1(dxgiFactory2, device1, form.Handle, ref swapChainDescription, swapChainFullScreenDescription))
                using(var backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0))
                using(var rasterizerState = new RasterizerState(device1, rasterizerStateDescription))
                using(var renderTargetView = new RenderTargetView(device1, backBuffer)) {
                    var viewport = new ViewportF(0, 0, backBuffer.Description.Width, backBuffer.Description.Height);
                    context.Rasterizer.SetViewport(viewport);
                    context.Rasterizer.State = rasterizerState;

                    var depthBufferDescription = new Texture2DDescription {
                        Format = Format.D32_Float_S8X24_UInt,
                        ArraySize = 1,
                        MipLevels = 1,
                        Width = backBuffer.Description.Width,
                        Height = backBuffer.Description.Height,
                        SampleDescription = swapChain.Description.SampleDescription,
                        BindFlags = BindFlags.DepthStencil,
                    };
                    var depthStencilViewDescription = new DepthStencilViewDescription {
                        Dimension = swapChain.Description.SampleDescription.Count > 1 || swapChain.Description.SampleDescription.Quality > 0
                            ? DepthStencilViewDimension.Texture2DMultisampled
                            : DepthStencilViewDimension.Texture2D
                    };
                    var depthStencilStateDescription = new DepthStencilStateDescription {
                        IsDepthEnabled = true,
                        DepthComparison = Comparison.Less,
                        DepthWriteMask = DepthWriteMask.All,
                        IsStencilEnabled = false,
                        StencilReadMask = 0xff,
                        StencilWriteMask = 0xff,
                        FrontFace = new DepthStencilOperationDescription {
                            Comparison = Comparison.Always,
                            PassOperation = StencilOperation.Keep,
                            FailOperation = StencilOperation.Keep,
                            DepthFailOperation = StencilOperation.Increment
                        },
                        BackFace = new DepthStencilOperationDescription {
                            Comparison = Comparison.Always,
                            PassOperation = StencilOperation.Keep,
                            FailOperation = StencilOperation.Keep,
                            DepthFailOperation = StencilOperation.Decrement
                        }
                    };

                    using(var depthBuffer = new Texture2D(device1, depthBufferDescription))
                    using(var depthStencilView = new DepthStencilView(device1, depthBuffer, depthStencilViewDescription))
                    using(var depthStencilState = new DepthStencilState(device1, depthStencilStateDescription)) {
                        context.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);
                        
                        context.OutputMerger.DepthStencilState = depthStencilState;
                        context.InputAssembler.InputLayout = inputLayout;
                        context.VertexShader.SetConstantBuffer(0, worldViewProjectionBuffer);
                        context.VertexShader.Set(vertexShader);
                        context.PixelShader.Set(pixelShader);
                        context.PixelShader.SetShaderResource(0, textureView);
                        context.PixelShader.SetSampler(0, samplerState);
                        form.Show();


                        var model = GetCubeModel();
                        using(var vertexBuffer = CreateBuffer(device1, BindFlags.VertexBuffer, model.Vertices))
                        using(var indexBuffer = CreateBuffer(device1, BindFlags.IndexBuffer, model.Triangles)) {
                            var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);
                            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
                            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
                            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                            
                            var description = new BlendStateDescription();
                            description.RenderTarget[0] = new RenderTargetBlendDescription() {
                                IsBlendEnabled = true,
                                SourceBlend = BlendOption.SourceAlpha,
                                DestinationBlend = BlendOption.InverseSourceAlpha,
                                BlendOperation = BlendOperation.Add,
                                SourceAlphaBlend = BlendOption.Zero,
                                DestinationAlphaBlend = BlendOption.Zero,
                                AlphaBlendOperation = BlendOperation.Add,
                                RenderTargetWriteMask = ColorWriteMaskFlags.All,
                            };

                            context.OutputMerger.BlendState = new BlendState(device0, description);

                            Stopwatch time = new Stopwatch();
                            time.Start();
                            RenderLoop.Run(form, () =>
                            {

                                var cameraPosition = new Vector3(1.5f, 1.8f, -3);
                                var cameraTarget = new Vector3((float)Math.Sin(time.Elapsed.TotalSeconds % (Math.PI * 2)), (float)Math.Cos(time.Elapsed.TotalSeconds % (Math.PI * 2)), 0);
                                var cameraUp = Vector3.UnitY;
                                var worldMatrix = Matrix.Identity;
                                var viewMatrix = Matrix.LookAtLH(cameraPosition, cameraTarget, cameraUp); // reorient everything to camera space
                                var projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 3f, form.ClientSize.Width / (float)form.ClientSize.Height, .5f, 100f); // create a generic perspective projection matrix
                                var viewProjection = Matrix.Multiply(viewMatrix, projectionMatrix); // apply the perspective projection to the view matrix so that we're performing both operations
                                var worldViewProjection = worldMatrix * viewProjection; // include world translation with the view projection matrix
                                worldViewProjection.Transpose();
                                context.UpdateSubresource(ref worldViewProjection, worldViewProjectionBuffer);

                                context.ClearRenderTargetView(renderTargetView, Color.CornflowerBlue);
                                context.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1f, 0);

                                context.DrawIndexed(model.Triangles.Length * 3, 0, 0);

                                swapChain.Present(0, PresentFlags.None);
                            });
                        }
                    }
                }
            }
        }




        public static SharpDX.Direct3D11.Buffer CreateBuffer<T>(SharpDX.Direct3D11.Device1 device, BindFlags bindFlags, params T[] items)
            where T : struct {
            var len = Utilities.SizeOf(items);
            var stream = new DataStream(len, true, true);
            foreach(var item in items)
                stream.Write(item);
            stream.Position = 0;
            var buffer = new SharpDX.Direct3D11.Buffer(device, stream, len, ResourceUsage.Default,
                bindFlags, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            return buffer;
        }

        private static Model GetCubeModel() {
            var baseVertices = new[]
            {
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f),  // TLB 0
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f),   // TRB 1
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f),    // TRF 2
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f),   // TLF 3
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), // BLB 4
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f),  // BRB 5
                new Vector4(1.0f, -1.0f, 1.0f, 1.0f),   // BRF 6
                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f)   // BLF 7
            };

            var colors = new[]
            {
                new Color4(0.0f, 0.0f, 1.0f, 0.5f),
                new Color4(0.0f, 1.0f, 0.0f, 0.5f),
                new Color4(0.0f, 1.0f, 1.0f, 0.5f),
                new Color4(1.0f, 0.0f, 0.0f, 0.5f),
                new Color4(1.0f, 0.0f, 1.0f, 0.5f),
                new Color4(1.0f, 1.0f, 0.0f, 0.5f),
                new Color4(1.0f, 1.0f, 1.0f, 0.5f),
                new Color4(0.0f, 0.0f, 0.0f, 0.5f)
            };

            var faces = new[]
            {
                new uint[] { 3, 2, 6, 7 }, // Front
                new uint[] { 1, 0, 4, 5 }, // Back
                new uint[] { 0, 3, 7, 4 }, // Left
                new uint[] { 2, 1, 5, 6 }, // Right
                new uint[] { 0, 1, 2, 3 }, // Top
                new uint[] { 7, 6, 5, 4 }, // Bottom
            };

            var uv = new[]
            {
                new[] { 0f, 0f },
                new[] { 1f, 0f },
                new[] { 1f, 1f },
                new[] { 0f, 1f },
            };

            var vertices = new List<Vertex>();
            var triangles = new List<TriangleIndex>();
            for(uint i = 0; i < faces.Length; i++) {
                var face = faces[i];
                vertices.AddRange(face.Select((f, j) => new Vertex(baseVertices[f], colors[f], new Vector2(uv[j][0], uv[j][1]))));
                var k = i * 4;
                triangles.Add(new TriangleIndex(k, k + 2, k + 3));
                triangles.Add(new TriangleIndex(k, k + 1, k + 2));
            }

            return new Model(vertices.ToArray(), triangles.ToArray());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TriangleIndex {
        public TriangleIndex(uint a, uint b, uint c) {
            A = a;
            B = b;
            C = c;
        }

        public uint A;
        public uint B;
        public uint C;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex {
        public Vertex(Vector4 position, Color4 color, Vector2 textureUV) {
            Position = position;
            Color = color;
            TextureUV = textureUV;
        }

        public Vector4 Position;
        public Color4 Color;
        public Vector2 TextureUV;
    }

    public class Model {
        public Vertex[] Vertices { get; set; }
        public TriangleIndex[] Triangles { get; set; }

        public Model(Vertex[] vertices, TriangleIndex[] triangles) {
            Vertices = vertices;
            Triangles = triangles;
        }
    }

    
}