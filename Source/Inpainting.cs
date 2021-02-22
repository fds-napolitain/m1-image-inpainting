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
        public int[] mask_position;
        public BitArray mask;
        public int sensitivity = 2;

        public Inpainting()
        {
            writeableBitmap = new WriteableBitmap(100, 100);
            mask = new BitArray(10000);
            mask_position = new int[2] { -1, -1 };
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

        public byte this[int[] index, int color = 0] {
            get => pixels[index[0] * PIXEL_STRIDE + color + (index[1] * writeableBitmap.PixelWidth * PIXEL_STRIDE)];
            set => pixels[index[0] * PIXEL_STRIDE + color + (index[1] * writeableBitmap.PixelWidth * PIXEL_STRIDE)] = value;
        }

        /// <summary>
        /// Access mask by index i, j
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public bool getMask(int i, int j = 0) {
            return mask.Get(i * PIXEL_STRIDE + (j * writeableBitmap.PixelWidth * PIXEL_STRIDE));
        }

        public bool getMask(int[] index)
        {
            return mask.Get(index[0] * PIXEL_STRIDE + (index[1] * writeableBitmap.PixelWidth * PIXEL_STRIDE));
        }

        /// <summary>
        /// Used when mouse scroll
        /// </summary>
        public void setMask()
        {
            if (mask_position[0] != -1) {
                int r = this[mask_position, RED];
                int g = this[mask_position, GREEN];
                int b = this[mask_position, BLUE];
                int rmax = r + sensitivity;
                int rmin = r - sensitivity;
                int gmax = g + sensitivity;
                int gmin = g - sensitivity;
                int bmax = b + sensitivity;
                int bmin = b - sensitivity;
                bool flag = true;
                while (flag) {
                    int[][] rn = NeighborsCoordinates(mask_position[0], mask_position[1], RED);
                    int[][] gn = NeighborsCoordinates(mask_position[0], mask_position[1], GREEN);
                    int[][] bn = NeighborsCoordinates(mask_position[0], mask_position[1], BLUE);
                    for (int i = 0; i < 8; i++) {
                        if (NeighborCheck(rn[i])) {
                            if (this[rn[i]] <= rmax && this[rn[i]] >= rmin &&
                                this[gn[i]] <= gmax && this[gn[i]] >= gmin &&
                                this[bn[i]] <= bmax && this[bn[i]] >= bmin) {
                                mask.Set(rn[0][0] + 1 * PIXEL_STRIDE + (rn[0][1] + 1 * writeableBitmap.PixelWidth * PIXEL_STRIDE), true);
                            }
                        }
                    }
                }
            }
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
        /// Get the neighbors of a pixel of same color (coordinates)
        /// </summary>
        /// <param name="i">Horizontal position of pixel</param>
        /// <param name="j">Vertical position of pixel</param>
        /// <param name="color">Color position in the pixel</param>
        /// <returns></returns>
        public int[][] NeighborsCoordinates(int i, int j, int color = 0)
        {
            int[][] r = new int[][] {
                new int[] { i - 1, j - 1, color }, // top
                new int[] { i, j - 1, color },
                new int[] { i + 1, j - 1, color },
                new int[] { i - 1, j, color }, // middle
                new int[] { i + 1, j, color },
                new int[] { i - 1, j + 1, color }, // bottom
                new int[] { i, j + 1, color },
                new int[] { i + 1, j + 1, color },
            };
            if (getMask(r[0])) r[0][0] = -1; // if pixel on mask
            if (getMask(r[1])) r[1][0] = -1; // count as outside
            if (getMask(r[2])) r[2][0] = -1;
            if (getMask(r[3])) r[3][0] = -1;
            if (getMask(r[4])) r[4][0] = -1;
            if (getMask(r[5])) r[5][0] = -1;
            if (getMask(r[6])) r[6][0] = -1;
            if (getMask(r[7])) r[7][0] = -1;
            return r;
        }

        private bool NeighborCheck(int i, int j, int color = 0)
        {
            return !(i < 0
                || i >= writeableBitmap.PixelWidth
                || j < -1
                || j >= writeableBitmap.PixelHeight);
        }

        private bool NeighborCheck(int[] index, int color = 0)
        {
            return !(index[0] < 0
                || index[0] >= writeableBitmap.PixelWidth
                || index[1] < -1
                || index[1] >= writeableBitmap.PixelHeight);
        }

        /// <summary>
        /// Get the neighbors of a pixel of same color (value)
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public int[] Neighbors(int i, int j, int color = 0) {
            List<int> neighbors = new List<int>();
            int[][] c = NeighborsCoordinates(i, j, color);
            for (int k = 0; k < 8; k++) {
                if (NeighborCheck(c[k])) {
                    neighbors.Add(this[c[k], color]);
                }
            }
            return neighbors.ToArray();
        }

        /// <summary>
        /// Custom erosion which set mean value of neighbors to (i, j) if getMask(i, j) == true
        /// </summary>
        public void ErosionMean()
        {
            Inpainting copy = new Inpainting(pixels);
            for (int j = 0; j < writeableBitmap.PixelHeight; j++) {
                for (int i = 0; i < writeableBitmap.PixelWidth; i++) {
                    if (getMask(i, j)) {
                        int[] blueNeighbors = Neighbors(i, j, BLUE);
                        int[] greenNeighbors = Neighbors(i, j, GREEN);
                        int[] redNeighbors = Neighbors(i, j, RED);
                        byte blueMax = (byte)Math.Round(blueNeighbors.Average());
                        byte greenMax = (byte)Math.Round(greenNeighbors.Average());
                        byte redMax = (byte)Math.Round(redNeighbors.Average());
                        this[i, j, BLUE] = blueMax;
                        this[i, j, GREEN] = greenMax;
                        this[i, j, RED] = redMax;
                    }
                }
            }
        }
    }

}
