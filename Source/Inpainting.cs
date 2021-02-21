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
        // const
        private const byte BLUE = 0;
        private const byte GREEN = 1;
        private const byte RED = 2;
        private const byte PIXEL_STRIDE = 4;
        // image and its pixels
        private WriteableBitmap writeableBitmap;
        public byte[] pixels;
        // mask
        public int?[] mask;
        public byte sensitivity = 2;

        public Inpainting()
        {
            writeableBitmap = new WriteableBitmap(100, 100);
            mask = new int?[2] { null, null };
        }

        public WriteableBitmap WriteableBitmap { get => writeableBitmap; }

        /// <summary>
        /// Access pixels by indexing with i for horizontal position, j for vertical position
        /// and color for the specific color of the pixel.
        /// </summary>
        /// <param name="i">Horizontal position of pixel</param>
        /// <param name="j">Vertical position of pixel</param>
        /// <param name="color">Color position in the pixel</param>
        /// <returns></returns>
        public byte this[int i, int j = 0, byte color = 0] {
            get => pixels[i * PIXEL_STRIDE + color + (j * writeableBitmap.PixelWidth)];
            set => pixels[i * PIXEL_STRIDE + color + (j * writeableBitmap.PixelWidth)] = value;
        }

        /// <summary>
        /// To be called after any processing so image is rewrote to the screen.
        /// </summary>
        public async void Reload()
        {
            // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer
            using (Stream stream = writeableBitmap.PixelBuffer.AsStream()) {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }
            // Redraw the WriteableBitmap
            writeableBitmap.Invalidate();
        }

        /// <summary>
        /// Get the neighbors of a pixel of same color.
        /// </summary>
        /// <param name="i">Horizontal position of pixel</param>
        /// <param name="j">Vertical position of pixel</param>
        /// <param name="color">Color position in the pixel</param>
        /// <returns></returns>
        public byte?[] Neighbors(int i, int j, byte color = 0)
        {
            return new byte?[] {
                i-PIXEL_STRIDE >=0 ? this[i-PIXEL_STRIDE+color, j-PIXEL_STRIDE] : null,
                this[i+color, j-PIXEL_STRIDE],
                this[i+PIXEL_STRIDE+color, j-PIXEL_STRIDE],
                this[i-PIXEL_STRIDE+color, j],
                this[i+PIXEL_STRIDE+color, j],
                this[i-PIXEL_STRIDE+color, j+PIXEL_STRIDE],
                this[i+color, j+PIXEL_STRIDE],
                this[i+PIXEL_STRIDE+color, j+PIXEL_STRIDE],
            };
        }

        public void Erode()
        {
            byte[] copy = pixels;
            for (int j = 0; j < writeableBitmap.PixelHeight; j++) {
                for (int i = 0; i < writeableBitmap.PixelWidth; i++) {
                    byte[] blueNeighbors = Neighbors(i, j, BLUE);
                    byte[] greenNeighbors = Neighbors(i, j, GREEN);
                    byte[] redNeighbors = Neighbors(i, j, RED);
                    if (blueNeighbors.Max() < this[i, j, BLUE]) {
                        //this[i, j, BLUE] =
                    }
                }
            }
        }
    }

}
