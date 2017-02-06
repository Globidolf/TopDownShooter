
using System;
using System.Diagnostics;
using System.Linq;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;

using D2DDeviceContext = SharpDX.Direct2D1.DeviceContext;
using D3DDevice = SharpDX.Direct3D11.Device1;
using D3DDeviceContext1 = SharpDX.Direct3D11.DeviceContext1;
using DXGIFactory2 = SharpDX.DXGI.Factory2;
using Game_Java_Port.Interface;
using System.Collections.Generic;
using Game_Java_Port.Logics;

namespace Game_Java_Port
{
    static class Program
    {
        public static List<string> DebugLog = new List<string>();

        public static PixelFormat PForm = new PixelFormat(Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied);
        
        public static RenderForm form;
        public static D3DDevice device;
        public static D2DDeviceContext D2DContext;
        public static D3DDeviceContext1 D3DContext;
#if DEBUG
        private static DeviceDebug debugger;
#endif
        static SwapChain1 swapChain;
        public static Stopwatch stopwatch;
        private static bool _togleFullScreen = false;

        static Vector2 scale = Vector2.One;
        public static object RenderLock = new object();
		public static Vector2 center { get { return new Vector2(width/2, height/2); } }
        public static int width { get {
                return (swapChain != null && swapChain.IsFullScreen) || (swapChain == null && Settings.UserSettings.StartFullscreen) ?
                    Settings.UserSettings.FullscreenResolution.Width :
                    Settings.UserSettings.WindowResolution.Width;
            } }
        public static int height { get {
                return (swapChain != null && swapChain.IsFullScreen) || (swapChain == null && Settings.UserSettings.StartFullscreen) ?
                    Settings.UserSettings.FullscreenResolution.Height :
                    Settings.UserSettings.WindowResolution.Height;
            } }

        public static void formResized() {
            scale.X = (float)form.ClientSize.Width / width;
            scale.Y = (float)form.ClientSize.Height / height;
        }

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

            D2DContext.StrokeWidth = 2;

            loadImages();

            System.Windows.Forms.Cursor.Hide();
            float CursorSize = 16;
            GameStatus.Cursor = new CustomCursor(CursorTypes.Normal, CursorSize);

            form.MaximizeBox = false;

            form.MouseWheel += (obj, args) => GameStatus.MouseWheel(args);

            form.MouseUp += (obj, args) => GameStatus.SetMouseState(args, false);
            form.MouseDown += (obj, args) => GameStatus.SetMouseState(args);

            form.MouseMove += (obj, args) =>
            {
                GameStatus.MousePos = new Vector2(args.Location.X, args.Location.Y) / scale;
				GameStatus.Cursor.invalidate = true;
            };

            form.KeyUp += (obj, args) => GameStatus.SetKeyState(args, false);
            form.KeyDown += (obj, args) => GameStatus.SetKeyState(args);


            //ResizeBegin += (obj, args) => Renderer.pauseRenderer() ;




            //clean stop
            form.FormClosing += (obj, args) => GameStatus.finalize();



            //form.MonitorChanged += (obj, args) => formResized(obj, args, true);
            form.ResizeEnd += (obj, args) => formResized();

            form.Show();

            

            GameStatus.init();

            GameStatus.Running = true;

            

            RenderLoop.Run(form, () =>
                {
                    if(_togleFullScreen)
                        Resize(true);
                    GameStatus.LastTick = GameStatus.CurrentTick;
                    GameStatus.CurrentTick = stopwatch.Elapsed.TotalMilliseconds;
                    GameStatus.MsPassed = GameStatus.CurrentTick - GameStatus.LastTick;
                    if(GameStatus.Running)
                        GameStatus.tick(false);

					#region old code
					//_RenderTarget.Target = Target;
					/*
                    D2DContext.BeginDraw();

                    IRenderable[] renderables;
                    
                        renderables = GameStatus.Renderables.ToArray();

                    foreach(IRenderable nonbg in renderables) {
                        nonbg.draw(D2DContext);
                    }

                    //_RenderTarget.DrawText(test3, Font, new RectangleF(0, 0, width, height), brush);
                    //SpriteFont.DEFAULT.directDrawText(fps.ToString() + "\n" + renderables.Count() + "\n" + GameStatus.GameObjects.Count, new RectangleF(0, 0, width, height), D2DContext, Color.Black);

                    D2DContext.EndDraw();
					*/
					#endregion

					Renderer.updatePositions();

					Renderer.draw();
					
                    i++;

                    if(stopwatch.ElapsedMilliseconds - i2 > 1000) {
                        fps = i;
                        i = 0;
                        i2 = stopwatch.ElapsedMilliseconds;
						Console.WriteLine(fps);
                    }
                    swapChain.Present(0, PresentFlags.None);
                });


            // Release all resources
            dispose();
        }

        static void loadImages() {
            dataLoader.LoadAll(device);
        }

        public static void PrepareToggleFullscreen() {
            _togleFullScreen = true;
        }

        public static void Resize(bool toggledisplaymode = false) {
            _togleFullScreen = false;
            dataLoader.unLoadAll();
            //Tileset.Clear();

            device.ImmediateContext.ClearState();
            device.ImmediateContext.Flush();

            //_RenderTarget.Flush();
            D2DContext.Dispose();
            ModeDescription desc;
            if(toggledisplaymode) {
                //form.IsFullscreen ^= true;
                form.IsFullscreen ^= true;
                if(form.IsFullscreen) {
                    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    form.Location = System.Drawing.Point.Empty;
                    //swapChain.ContainingOutput.GetDisplayModeList(Format.R8G8B8A8_UNorm, DisplayModeEnumerationFlags.);

                    desc = new ModeDescription() {
                        Width = Settings.UserSettings.FullscreenResolution.Width,
                        Height = Settings.UserSettings.FullscreenResolution.Height,
                        RefreshRate = new Rational(0 , 1)
                    };
                } else {
                    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
                    desc = new ModeDescription() {
                        Width = Settings.UserSettings.WindowResolution.Width,
                        Height = Settings.UserSettings.WindowResolution.Height,
                        RefreshRate = new Rational(0, 1)
                    };
                }

                swapChain.ResizeTarget(ref desc);
                form.Refresh();
                swapChain.SetFullscreenState(!swapChain.IsFullScreen, null);
            }



            swapChain.ResizeBuffers(0, width, height, Format.Unknown, SwapChainFlags.AllowModeSwitch);

            
            using(Surface surface = swapChain.GetBackBuffer<Surface>(0)) {
                D2DContext = new D2DDeviceContext(surface, new CreationProperties());
            }
            D2DContext.UnitMode = UnitMode.Pixels;
            D2DContext.StrokeWidth = 2;

			Renderer.init(device, D3DContext, swapChain);

            dataLoader.LoadAll(device);
            //Tileset.Regenerate();
            //Background_Tiled.Regenerate();
            //Menu_BG_Tiled.Regenerate();
            //GameStatus.Regenerate();
            //CustomCursor.Regenerate();
            formResized();
        }

        static void init() {

            form = new RenderForm("Shooter Game");
            form.ClientSize = new System.Drawing.Size(width, height);
            form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            form.AllowDrop = false;

            device = new SharpDX.Direct3D11.Device(DriverType.Hardware,
                DeviceCreationFlags.BgraSupport |
                DeviceCreationFlags.SingleThreaded
#if !DEBUG
                ).QueryInterface<D3DDevice>();
#else
                | DeviceCreationFlags.Debug).QueryInterface<D3DDevice>();
#endif
#if DEBUG
            debugger = new DeviceDebug(device);
            debugger.ReportLiveDeviceObjects(ReportingLevel.Detail);
#endif

            createswapchain(device);
            swapChain.DebugName = "THE CHAIN OF SWAPPING";

            D3DContext = device.ImmediateContext.QueryInterface<D3DDeviceContext1>();

            //RenderTarget RenderTar

            //D3DContext.OutputMerger.SetRenderTargets()



            using(Surface surface = swapChain.GetBackBuffer<Surface>(0)) 
                D2DContext = new D2DDeviceContext(surface, new CreationProperties());
			
			Renderer.init(device, D3DContext, swapChain);

            D2DContext.UnitMode = UnitMode.Pixels;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }
        

        static void createswapchain(D3DDevice device) {

            SwapChainDescription1 desc = new SwapChainDescription1() {
                BufferCount = 1,
                Format = Format.R8G8B8A8_UNorm,
                Height = 0,
                Width = 0,
                SampleDescription = new SampleDescription(1, 0),
                Scaling = Scaling.Stretch,
                Stereo = false,
                SwapEffect = SwapEffect.Sequential,
                Usage = Usage.RenderTargetOutput
            };

            using(SharpDX.Direct3D11.Device1 d3d11device = device.QueryInterface<SharpDX.Direct3D11.Device1>()) {
                using(SharpDX.DXGI.Device2 dxgidevice2 = d3d11device.QueryInterface<SharpDX.DXGI.Device2>()) {
                    using(Adapter adap = dxgidevice2.Adapter) {
                        using(DXGIFactory2 factory = adap.GetParent<DXGIFactory2>()) {


                            swapChain = new SwapChain1(factory, d3d11device, form.Handle, ref desc,
                                new SwapChainFullScreenDescription() {
                                    Windowed = !Settings.UserSettings.StartFullscreen,
                                    RefreshRate = new Rational(0, 0),
                                    Scaling = DisplayModeScaling.Unspecified,
                                    ScanlineOrdering = DisplayModeScanlineOrder.Progressive
                                });
                            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAltEnter);
                        }
                    }
                }
            }
        }
        

        static void dispose() {
            swapChain.Dispose();
            //Tileset.Clear();
            D2DContext.Dispose();
#if DEBUG
            debugger.Dispose();
#endif
            device.Dispose();
        }

    }
}
