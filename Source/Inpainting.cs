using System;
using System.Collections;
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
        private const int BLUE = 0;
        private const int GREEN = 1;
        private const int RED = 2;
        private const int PIXEL_STRIDE = 4;
        // image and its pixels
        private WriteableBitmap writeableBitmap;
        public byte[] pixels;
        // mask
        public int?[] mask_position;
        public BitArray mask;
        public int sensitivity = 2;

        public Inpainting()
        {
            writeableBitmap = new WriteableBitmap(100, 100);
            mask = new BitArray(10000);
            mask_position = new int?[2] { null, null };
        }

        public Inpainting(byte[] pixels)
        {
            this.pixels = pixels;
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
        public byte this[int i, int j = 0, int color = 0] {
            get => pixels[i * PIXEL_STRIDE + color + (j * writeableBitmap.PixelWidth * PIXEL_STRIDE)];
            set => pixels[i * PIXEL_STRIDE + color + (j * writeableBitmap.PixelWidth * PIXEL_STRIDE)] = value;
        }

        public bool getMask(int i, int j = 0) {
            return mask.Get(i * PIXEL_STRIDE + (j * writeableBitmap.PixelWidth * PIXEL_STRIDE));
        }

        /// <summary>
        /// To be called after any processing so image is rewrote to the screen
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
        public int[][] NeighborsCoordinates(int i, int j, int color = 0)
        {
            return new int[][] {
                new int[] { i - 1, j - 1, color }, // top
                new int[] { i, j - 1, color },
                new int[] { i + 1, j - 1, color },
                new int[] { i - 1, j, color }, // middle
                new int[] { i + 1, j, color },
                new int[] { i - 1, j + 1, color }, // bottom
                new int[] { i, j + 1, color },
                new int[] { i + 1, j + 1, color },
            };
        }

        public byte[] Neighbors(int i, int j, int color = 0) {
            List<byte> neighbors = new List<byte>();
            int[][] c = NeighborsCoordinates(i, j, color);
            for (int k = 0; k < 8; k++) {
                if (c[k][0] < 0 || c[k][0] >= writeableBitmap.PixelWidth || c[k][1] < -1 || c[k][1] >= writeableBitmap.PixelHeight) {
                    neighbors.Add(this[c[k][0], c[k][1], color]);
                }
            }
            return neighbors.ToArray();
        }

        public void Erosion()
        {
            Inpainting copy = new Inpainting(pixels);
            for (int j = 0; j < writeableBitmap.PixelHeight; j++) {
                for (int i = 0; i < writeableBitmap.PixelWidth; i++) {
                    byte[] blueNeighbors = Neighbors(i, j, BLUE);
                    byte[] greenNeighbors = Neighbors(i, j, GREEN);
                    byte[] redNeighbors = Neighbors(i, j, RED);
                    byte blueMax = blueNeighbors.Max();
                    byte greenMax = greenNeighbors.Max();
                    byte redMax = redNeighbors.Max();
                    if (blueMax < copy[i, j, BLUE]) {
                        this[i, j, BLUE] = blueMax;
                    }
                    if (greenMax < copy[i, j, GREEN]) {
                        this[i, j, GREEN] = greenMax;
                    }
                    if (redMax < copy[i, j, RED]) {
                        this[i, j, RED] = redMax;
                    }
                }
            }
        }

        public void Dilation()
        {
            Inpainting copy = new Inpainting(pixels);
            for (int j = 0; j < writeableBitmap.PixelHeight; j++) {
                for (int i = 0; i < writeableBitmap.PixelWidth; i++) {
                    byte[] blueNeighbors = Neighbors(i, j, BLUE);
                    byte[] greenNeighbors = Neighbors(i, j, GREEN);
                    byte[] redNeighbors = Neighbors(i, j, RED);
                    byte blueMin = blueNeighbors.Min();
                    byte greenMin = greenNeighbors.Min();
                    byte redMin = redNeighbors.Min();
                    if (blueMin > copy[i, j, BLUE]) {
                        this[i, j, BLUE] = blueMin;
                    }
                    if (greenMin > copy[i, j, GREEN]) {
                        this[i, j, GREEN] = greenMin;
                    }
                    if (redMin > copy[i, j, RED]) {
                        this[i, j, RED] = redMin;
                    }
                }
            }
        }
    }

}
