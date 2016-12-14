

using Microsoft.Win32.SafeHandles;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Game_Java_Port {
    public static class dataLoader {

        public const string imgdir = "./data/img/";

        private static Dictionary<string, Bitmap> images = new Dictionary<string, Bitmap>();

        public static Bitmap get(string name) {
            if(!images.ContainsKey(name))
                Load(null, name);
            return images[name];
        }


        /// <summary>
        /// Loads a Direct2D Bitmap from a file using System.Drawing.Image.FromFile(...)
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="file">The file.</param>
        /// <returns>A D2D1 Bitmap</returns>
        public static void Load(RenderTarget renderTarget, string file) {
            // Loads from file using System.Drawing.Image

            //check if image has been loaded already
            if(!images.ContainsKey(file)) {
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
                BitmapProperties bitmapProperties = new BitmapProperties(new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied));

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
                    // Lock System.Drawing.Bitmap
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

                    images.Add(file, new Bitmap(renderTarget, size, tempStream, stride, bitmapProperties));
                }
            }
        }

    }
}
