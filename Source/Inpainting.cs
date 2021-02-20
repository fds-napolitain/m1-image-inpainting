using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using System.IO;

namespace m1_image_projet.Source
{
    public sealed class Inpainting
    {
        private const short PIXEL_STRIDE = 4;
        private WriteableBitmap writeableBitmap;
        public byte[] pixels;

        public Inpainting()
        {
            writeableBitmap = new WriteableBitmap(100, 100);
        }

        public Inpainting(Inpainting inpainting)
        {
            writeableBitmap = inpainting.writeableBitmap;
            pixels = inpainting.pixels;
        }

        public WriteableBitmap WriteableBitmap { get => writeableBitmap; }

        public byte this[int i, int j = 0] {
            get => pixels[i + (j * writeableBitmap.PixelWidth)];
            set => pixels[i + (j * writeableBitmap.PixelWidth)] = value;
        }

        public async void Reload()
        {
            // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer
            using (Stream stream = writeableBitmap.PixelBuffer.AsStream()) {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }
            // Redraw the WriteableBitmap
            writeableBitmap.Invalidate();
        }

        public byte[] Neighbors(int i, int j)
        {
            return new byte[] {
                this[i-PIXEL_STRIDE, j-PIXEL_STRIDE],
                this[i, j-PIXEL_STRIDE],
                this[i+PIXEL_STRIDE, j-PIXEL_STRIDE],
                this[i-PIXEL_STRIDE, j],
                this[i+PIXEL_STRIDE, j],
                this[i-PIXEL_STRIDE, j+PIXEL_STRIDE],
                this[i, j+PIXEL_STRIDE],
                this[i+PIXEL_STRIDE, j+PIXEL_STRIDE],
            };
        }


    }

}
