
using System;
using System.Diagnostics;
using System.Linq;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;

using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory2 = SharpDX.DXGI.Factory2;
using Game_Java_Port.Interface;
using System.Collections.Generic;

namespace Game_Java_Port
{
    static class Program
    {
        public static List<string> DebugLog = new List<string>();

        public static SharpDX.DirectWrite.Factory DW_Factory;
        public static RenderTarget _RenderTarget = null;

        public static RenderForm form;
        static Device device;
        static SwapChain swapChain;
        static Factory2 factory;
        static SharpDX.Direct2D1.Factory d2dFactory;
        static Texture2D backBuffer;
        static SharpDX.DirectWrite.TextFormat Font;
        static RenderTargetView renderView;
        static Surface surface;
        static SolidColorBrush brush;
        public static Stopwatch stopwatch;

        static Vector2 scale = Vector2.One;
        public static object Pause = new object();
        public static int width;
        public static int height;


        public static void formResized() {

            scale.X = (float)form.ClientSize.Width / width;
            scale.Y = (float)form.ClientSize.Height / height;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            int i = 0;
            long i2 = 0;
            int fps = 0;
            // Main loop


            init();

            _RenderTarget.StrokeWidth = 2;

            loadImages();

            GameStatus.addTickable(Game.instance);
            GameStatus.addRenderable(Game.instance);

            GameMenu mainMenu = GameMenu.MainMenu;

            mainMenu.open();

            System.Windows.Forms.Cursor.Hide();
            GameStatus.Cursor = new CustomCursor(CursorTypes.Normal, 16);

            form.MaximizeBox = false;

            form.MouseWheel += (obj, args) => GameStatus.MouseWheel(args);

            form.MouseUp += (obj, args) => GameStatus.SetMouseState(args, false);
            form.MouseDown += (obj, args) => GameStatus.SetMouseState(args);

            form.MouseMove += (obj, args) => {
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

            Background back = new Background(dataLoader.get("GameBG.bmp"), settings: Background.Settings.Fill_Screen | Background.Settings.Parallax);
            back.ExtendX = ExtendMode.Wrap;
            back.ExtendY = ExtendMode.Wrap;


            GameStatus.addRenderable(GameStatus.Cursor);
            GameStatus.addTickable(GameStatus.Cursor);

            GameStatus.Running = true;
            
            RenderLoop.Run(form, () =>
            {
                lock(Pause) {
                    _RenderTarget.BeginDraw();
                    _RenderTarget.Clear(Color.Black);

                    IRenderable[] renderables;

                    int objs = 0;
                    int bgs = 0;
                    int fgs = 0;

                    lock(GameStatus.Renderables)
                        renderables = GameStatus.Renderables.ToArray();

                    foreach(Background bg in renderables.Where((bg) => bg is Background)) {
                        if(!bg.settings.HasFlag(Background.Settings.Foreground)) {
                            bg.draw(_RenderTarget);
                            bgs++;
                        }
                    }

                    foreach(IRenderable nonbg in renderables.Where((nbg) => !(nbg is Background))) {
                        nonbg.draw(_RenderTarget);
                        objs++;
                    }

                    foreach(Background fg in renderables.Where((fg) => fg is Background)) {
                        if(fg.settings.HasFlag(Background.Settings.Foreground)) {
                            fg.draw(_RenderTarget);
                            fgs++;
                        }
                    }

                    _RenderTarget.DrawText(
                        fps.ToString("000") + " fps | " + bgs.ToString("000") + " bg | " + 
                        objs.ToString("000") + " obj | " + fgs.ToString("000") + " fg | " + GameStatus.GameSubjects.Count + " tick", Font, new RectangleF(0, 0, width, height), brush);
                    i++;
                    if(stopwatch.ElapsedMilliseconds - i2 > 1000) {
                        fps = i;
                        i = 0;
                        i2 = stopwatch.ElapsedMilliseconds;
                    }
                    _RenderTarget.EndDraw();
                }
                swapChain.Present(0, PresentFlags.None);
            });

            // Release all resources
            dispose();
        }

        static void loadImages() {
            dataLoader.LoadAll(_RenderTarget);
            /*
            dataLoader.Load(RenderTarget, "test.bmp");
            dataLoader.Load(RenderTarget, "test.bmp");
            */ 
        }

        public static void Resize() {

            lock(Pause) {

                device.ImmediateContext.ClearState();
                renderView.Dispose();
                _RenderTarget.Dispose();
                surface.Dispose();
                backBuffer.Dispose();

                swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);

                backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                renderView = new RenderTargetView(device, backBuffer);
                surface = backBuffer.QueryInterface<Surface>();
                _RenderTarget = new RenderTarget(d2dFactory, surface, new RenderTargetProperties(new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)));
            }
        }

        static void init() {



            form = new RenderForm("Shooter Game");
            form.Width = 1024;
            form.Height = 800;
            form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            
            
            
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
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, new SharpDX.Direct3D.FeatureLevel[] { Device.GetSupportedFeatureLevel() }, desc, out device, out swapChain);

            d2dFactory = new SharpDX.Direct2D1.Factory(FactoryType.SingleThreaded);

            width = form.ClientSize.Width;
            height = form.ClientSize.Height;

            // Ignore all windows events
            factory = swapChain.GetParent<Factory2>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.None);
            DW_Factory = new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Isolated);

            

            Font = new SharpDX.DirectWrite.TextFormat(DW_Factory, "arial", 12);
            
            // New RenderTargetView from the backbuffer
            backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderView = new RenderTargetView(device, backBuffer);
            
            surface = backBuffer.QueryInterface<Surface>();
            

            _RenderTarget = new RenderTarget(d2dFactory, surface,
                                                            new RenderTargetProperties(new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)));

            
            
            
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
