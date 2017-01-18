
using System;
using System.Diagnostics;
using System.Linq;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;


using D3DTexture = SharpDX.Direct3D11.Texture2D;
using D2DEffect = SharpDX.Direct2D1.Effect;
using D2DDeviceContext = SharpDX.Direct2D1.DeviceContext;
using D2DDevice = SharpDX.Direct2D1.Device;
using D2DAlphaMode = SharpDX.Direct2D1.AlphaMode;
using D3DDevice = SharpDX.Direct3D11.Device;
using DXGIFactory2 = SharpDX.DXGI.Factory2;
using Game_Java_Port.Interface;
using System.Collections.Generic;

namespace Game_Java_Port
{
    static class Program
    {
        public static List<string> DebugLog = new List<string>();

        public static SharpDX.DirectWrite.Factory DW_Factory;

        public static RenderForm form;
        public static D3DDevice device;
        public static D2DDeviceContext _RenderTarget;
        static SwapChain swapChain;
        static DXGIFactory2 factory;
        static SharpDX.Direct2D1.Factory d2dFactory;
        static Texture2D backBuffer;
        static SharpDX.DirectWrite.TextFormat Font;
        static RenderTargetView renderView;
        public static Surface surface;
        static SolidColorBrush brush;
        public static Stopwatch stopwatch;

        static Vector2 scale = Vector2.One;
        public static object RenderLock = new object();
        public static int width;
        public static int height;


        public static void formResized() {

            scale.X = (float)form.ClientSize.Width / width;
            scale.Y = (float)form.ClientSize.Height / height;
        }
        public static string test3 = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            int i = 0;
            long i2 = 0;
            int fps = 0;
            // Main loop


            init();

            _RenderTarget.StrokeWidth = 2;

            loadImages();

            System.Windows.Forms.Cursor.Hide();
            GameStatus.Cursor = new CustomCursor(CursorTypes.Normal, 16);

            form.MaximizeBox = false;

            form.MouseWheel += (obj, args) => GameStatus.MouseWheel(args);

            form.MouseUp += (obj, args) => GameStatus.SetMouseState(args, false);
            form.MouseDown += (obj, args) => GameStatus.SetMouseState(args);

            form.MouseMove += (obj, args) =>
            {
                GameStatus.MousePos = new Vector2(args.Location.X, args.Location.Y) / scale;
                RectangleF temp = GameStatus.Cursor.Area;

                temp.Location = GameStatus.MousePos;

                GameStatus.Cursor.Area = temp;
            };

            form.KeyUp += (obj, args) => GameStatus.SetKeyState(args, false);
            form.KeyDown += (obj, args) => GameStatus.SetKeyState(args);

            //ResizeBegin += (obj, args) => Renderer.pauseRenderer() ;




            //clean stop
            form.FormClosing += (obj, args) => GameStatus.finalize();



            form.ResizeEnd += (obj, args) => formResized();

            GameStatus.init();

            GameStatus.Running = true;


            RenderLoop.Run(form, () =>
                {

                    GameStatus.LastTick = GameStatus.CurrentTick;
                    GameStatus.CurrentTick = stopwatch.Elapsed.TotalMilliseconds;
                    GameStatus.MsPassed = GameStatus.CurrentTick - GameStatus.LastTick;
                    if(GameStatus.Running)
                        GameStatus.tick(false);

                    _RenderTarget.BeginDraw();
                    _RenderTarget.BeginDraw();

                    IRenderable[] renderables;

                    lock(GameStatus.Renderables)
                        renderables = GameStatus.Renderables.ToArray();

                    foreach(IRenderable nonbg in renderables) {
                        nonbg.draw(_RenderTarget);
                    }
                    
                    //_RenderTarget.DrawText(test3, Font, new RectangleF(0, 0, width, height), brush);
                    _RenderTarget.DrawText(
                        fps.ToString(), Font, new RectangleF(0, 0, width, height), brush);
                    i++;


                    if(stopwatch.ElapsedMilliseconds - i2 > 1000) {
                        fps = i;
                        i = 0;
                        i2 = stopwatch.ElapsedMilliseconds;
                    }
                    
                    //_RenderTarget.EndDraw();
                    _RenderTarget.EndDraw();

                    swapChain.Present(0, PresentFlags.None);
                });

            // Release all resources
            dispose();
        }

        static void loadImages() {
            dataLoader.LoadAll(_RenderTarget);
        }

        public static void Resize() {
            
                device.ImmediateContext.ClearState();
                renderView.Dispose();
                _RenderTarget.Dispose();
                surface.Dispose();
                backBuffer.Dispose();

                swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);

                backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                renderView = new RenderTargetView(device, backBuffer);
                surface = backBuffer.QueryInterface<Surface>();
                //_RenderTarget = new RenderTarget(d2dFactory, surface, new RenderTargetProperties(new PixelFormat(Format.R8G8B8A8_UNorm, D2DAlphaMode.Premultiplied)));
        }

        static void init() {



            

            form = new RenderForm("Shooter Game");
            form.Width = 1024;
            form.Height = 800;
            form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            form.AllowDrop = false;
            
            SwapChainDescription desc = new SwapChainDescription() {
                BufferCount = 1,
                ModeDescription =
                                   new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                                       new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Sequential,
                Usage = Usage.RenderTargetOutput,
                Flags = SwapChainFlags.None
            };



            
            

            // Create Device and SwapChain
            D3DDevice.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, new SharpDX.Direct3D.FeatureLevel[] { D3DDevice.GetSupportedFeatureLevel() }, desc, out device, out swapChain);



            //D2DDevice test = new D2DDevice(device.QueryInterface<SharpDX.DXGI.Device>());


            //new SharpDX.Direct2D1.DeviceContext(Surface.)

            //new SharpDX.Direct2D1.Device1(test2, testfactory.Adapters[0].Outputs[0].)


            _RenderTarget = new D2DDeviceContext(new D2DDevice(device.QueryInterface<SharpDX.DXGI.Device>()), DeviceContextOptions.None);
            
            d2dFactory = new SharpDX.Direct2D1.Factory(FactoryType.MultiThreaded);

            width = form.ClientSize.Width;
            height = form.ClientSize.Height;

            // Ignore all windows events
            factory = swapChain.GetParent<DXGIFactory2>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.None);
            DW_Factory = new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared);

            

            Font = new SharpDX.DirectWrite.TextFormat(DW_Factory, "arial", 12);
            
            // New RenderTargetView from the backbuffer
            backBuffer = D3DTexture.FromSwapChain<D3DTexture>(swapChain, 0);
            renderView = new RenderTargetView(device, backBuffer);
            
            surface = backBuffer.QueryInterface<Surface>();
            
            


            _RenderTarget = new RenderTarget(d2dFactory, surface,
                                                            new RenderTargetProperties(new PixelFormat(Format.R8G8B8A8_UNorm, D2DAlphaMode.Premultiplied)));

            
            
            
            brush = new SolidColorBrush(_RenderTarget, Color.White);
            



            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        static void dispose() {
            renderView.Dispose();
            backBuffer.Dispose();
            device.ImmediateContext.ClearState();
            device.ImmediateContext.Flush();
            device.Dispose();
            swapChain.Dispose();
            factory.Dispose();
            d2dFactory.Dispose();
            Font.Dispose();
            lock(DW_Factory)
                DW_Factory.Dispose();
            brush.Dispose();
            _RenderTarget.Dispose();
            surface.Dispose();
            form.Dispose();
        }

    }
}
