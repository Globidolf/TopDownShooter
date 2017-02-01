

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Resource = SharpDX.Direct3D11.Resource;
using Device = SharpDX.Direct3D11.Device;

namespace Game_Java_Port {
    public static class dataLoader {

        public static bool Loaded = false;

        public const string imgdir = "./data/img/";


        public static int getResID(string ResName = "default") { return Array.FindIndex(filenames, f => f == ResName); }

        private static string[] filenames;
        public static ShaderResourceView[] ShaderData;
        public static Texture2D[] D3DResources;
        

        public static void LoadAll(Device device) {
            if (Loaded)
                unLoadAll();

            

            if(!Loaded) {
                Loaded = true;
                filenames = new List<string>(Directory.EnumerateFiles(imgdir)).ConvertAll(s => s.Remove(0, imgdir.Length)).ToArray();
                D3DResources = new Texture2D[filenames.Length];
                ShaderData = new ShaderResourceView[filenames.Length];

                for(int i = 0; i < filenames.Length; i++) {
                    Load(device, filenames[i], i);
                }
            }
        }

        public static void unLoadAll() {
            if(Loaded) {
                Loaded = false;
                foreach(ShaderResourceView srv in ShaderData)
                    srv.Dispose();
                foreach(Texture2D tx in D3DResources)
                    tx.Dispose();
                D3DResources = null;
                ShaderData = null;
                filenames = null;
            }
        }

        /// <summary>
        /// Combining two snippets to load bitmaps, i thus have achieved the ability to load a texture from a bitmap including it's alpha channel.
        /// </summary>
        /// <param name="device">Direct3D 11 Device to load the bitmap into</param>
        /// <param name="file">Name of the file</param>
        /// <returns>a 2D Bitmap resource to be used by shaders and similiar creatures</returns>
        private static void Load(Device device, string file, int ResID) {
            // Loads from file using System.Drawing.Image

            if(File.Exists(imgdir + file)) {
                //snippet taken from a random forum without any description
                //will read a bitmap from specified file INCLUDING those darn alpha values


                byte[] Buffer = File.ReadAllBytes(imgdir + file);
                GCHandle GCH = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
                IntPtr Scan0 = (IntPtr)((int)(GCH.AddrOfPinnedObject()) + 54);
                int W = Marshal.ReadInt32(Scan0, -36);
                int H = Marshal.ReadInt32(Scan0, -32);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(W, H, W * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, Scan0);
                bitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);

                GCH.Free();


                //to prevent some mysterious errors caused by the above bitmap instance, we copy the data to a safely instantiated one and use that one instead.

                System.Drawing.Rectangle sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);

                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);

                g.DrawImage(bitmap, 0, 0);

                g.Dispose();

                bitmap.Dispose();
                //code from this point taken from SharpDX sample:
                //https://github.com/sharpdx/SharpDX-Samples/blob/master/Desktop/Direct2D1/BitmapApp/Program.cs
                //slightly modified.

                Size2 size = new Size2(bmp.Width, bmp.Height);

                // Transform pixels from BGRA to RGBA
                int stride = bmp.Width * sizeof(int);
                using(DataStream tempStream = new DataStream(bmp.Height * stride, true, true)) {

                    System.Drawing.Imaging.BitmapData bitmapData = bmp.LockBits(sourceArea, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                    // Convert all pixels 
                    for(int y = 0; y < bmp.Height; y++) {
                        int offset = bitmapData.Stride * y;
                        for(int x = 0; x < bmp.Width; x++) {
                            // Not optimized 
                            byte B = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte G = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte R = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte A = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            /*if(A < 255 && R > 0)
                                break;
                                */
                            int rgba = R | (G << 8) | (B << 16) | (A << 24);
                            tempStream.Write(rgba);
                        }
                    }

                    bmp.UnlockBits(bitmapData);
                    tempStream.Position = 0;

                    D3DResources[ResID] = new Texture2D(device, new Texture2DDescription() {
                        Width = bmp.Size.Width,
                        Height = bmp.Size.Height,
                        ArraySize = 1,
                        BindFlags = BindFlags.ShaderResource,
                        Usage = ResourceUsage.Default,
                        CpuAccessFlags = CpuAccessFlags.None,
                        Format = Format.R8G8B8A8_UNorm,
                        MipLevels = 1,
                        OptionFlags = ResourceOptionFlags.None,
                        SampleDescription = new SampleDescription(1, 0),
                    }, new DataRectangle(tempStream.DataPointer, stride));
                    ShaderData[ResID] = new ShaderResourceView(device, D3DResources[ResID]);
                    bmp.Dispose();
                    tempStream.Dispose();
                }
            }
        }
    }
}
