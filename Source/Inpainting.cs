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
using System.Diagnostics;
using System.Numerics;

namespace m1_image_projet.Source
{
    public sealed partial class Inpainting
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
        public int sensitivity = 2;
        public int[] mask_position;
        public BitArray mask;
        public FMMPixel[] fmmpixels;
        public SortedSet<FMMPixelWithCoords> narrowBand;

        public Inpainting()
        {
            writeableBitmap = new WriteableBitmap(100, 100);
            mask = new BitArray(10000);
            fmmpixels = new FMMPixel[10000];
            narrowBand = new SortedSet<FMMPixelWithCoords>(new ByTValues());
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
        public byte this[int i, int j = 0, int color = 0]
        {
            get => pixels[i * PIXEL_STRIDE + color + (j * writeableBitmap.PixelWidth * PIXEL_STRIDE)];
            set => pixels[i * PIXEL_STRIDE + color + (j * writeableBitmap.PixelWidth * PIXEL_STRIDE)] = value;
        }

        public byte this[int[] index, int color = 0]
        {
            get => pixels[index[0] * PIXEL_STRIDE + color + (index[1] * writeableBitmap.PixelWidth * PIXEL_STRIDE)];
            set => pixels[index[0] * PIXEL_STRIDE + color + (index[1] * writeableBitmap.PixelWidth * PIXEL_STRIDE)] = value;
        }

        /// <summary>
        /// Access FMMPixel by index i, j
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public FMMPixel GetFMMPixel(int i, int j)
        {
            return fmmpixels[i + (j * writeableBitmap.PixelWidth)];
        }

        /// <summary>
        /// Access mask by index i, j
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public bool GetMask(int i, int j = 0)
        {
            return mask.Get(i + (j * writeableBitmap.PixelWidth));
        }

        /// <summary>
        /// Access mask by index[i, j]
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool GetMask(int[] index)
        {
            return mask.Get(index[0] + (index[1] * writeableBitmap.PixelWidth));
        }

        /// <summary>
        /// Set mask by index[i, j]
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private void SetMask(int[] index, bool value)
        {
            mask.Set(index[0] + (index[1] * writeableBitmap.PixelWidth), value);
        }

        /// <summary>
        /// Used when mouse scroll
        /// Flood-fill (node): (wikipedia)
        /// 1. Set Q to the empty queue or stack.
        /// 2. Add node to the end of Q.
        /// 3. While Q is not empty:
        /// 4.   Set n equal to the first element of Q.
        /// 5.   Remove first element from Q.
        /// 6.   If n is Inside:
        ///        Set the n
        ///        Add the node to the west of n to the end of Q.
        ///        Add the node to the east of n to the end of Q.
        ///        Add the node to the north of n to the end of Q.
        ///        Add the node to the south of n to the end of Q.
        /// 7. Continue looping until Q is exhausted.
        /// 8. Return.
        /// </summary>
        public void SetMask()
        {
            BitArray visited = new BitArray(writeableBitmap.PixelWidth * writeableBitmap.PixelHeight);
            Queue<int[]> queue = new Queue<int[]>();
            queue.Enqueue(mask_position);
            while (queue.Count > 0)
            {
                int[] n = queue.Dequeue();
                if (this[n, RED] > this[mask_position, RED] - sensitivity &&
                    this[n, RED] < this[mask_position, RED] + sensitivity &&
                    this[n, GREEN] > this[mask_position, GREEN] - sensitivity &&
                    this[n, GREEN] < this[mask_position, GREEN] + sensitivity &&
                    this[n, BLUE] > this[mask_position, BLUE] - sensitivity &&
                    this[n, BLUE] < this[mask_position, BLUE] + sensitivity)
                {
                    SetMask(n, true);
                    int[][] neighbors = NeighborsCoordinates(n[0], n[1]);
                    int[] top = neighbors[1];
                    int[] left = neighbors[3];
                    int[] right = neighbors[4];
                    int[] bottom = neighbors[6];
                    if (top[0] != -1 && !visited.Get(n[0] + (n[1] * writeableBitmap.PixelWidth))) queue.Enqueue(top);
                    if (left[0] != -1 && !visited.Get(n[0] + (n[1] * writeableBitmap.PixelWidth))) queue.Enqueue(left);
                    if (right[0] != -1 && !visited.Get(n[0] + (n[1] * writeableBitmap.PixelWidth))) queue.Enqueue(right);
                    if (bottom[0] != -1 && !visited.Get(n[0] + (n[1] * writeableBitmap.PixelWidth))) queue.Enqueue(bottom);
                }
                else
                {
                    SetMask(n, false);
                }
                visited.Set(n[0] + (n[1] * writeableBitmap.PixelWidth), true);
            }
        }

        /// <summary>
        /// Is mask pixel a border of the mask ?
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private bool IsMaskBorder(int i, int j = 0)
        {
            int[][] r = new int[][] {
                new int[] { i - 1, j - 1 }, // top
                new int[] { i, j - 1 },
                new int[] { i + 1, j - 1 },
                new int[] { i - 1, j }, // middle
                new int[] { i + 1, j },
                new int[] { i - 1, j + 1 }, // bottom
                new int[] { i, j + 1 },
                new int[] { i + 1, j + 1 },
            };
            if (i > 0 && j > 0 && !GetMask(r[0])) return true; // if pixel on mask
            if (j > 0 && !GetMask(r[1])) return true; // count as outside
            if (i < writeableBitmap.PixelWidth && j > 0 && !GetMask(r[2])) return true;
            if (i > 0 && !GetMask(r[3])) return true;
            if (i < writeableBitmap.PixelWidth && !GetMask(r[4])) return true;
            if (i > 0 && j < writeableBitmap.PixelHeight && !GetMask(r[5])) return true;
            if (j < writeableBitmap.PixelHeight && !GetMask(r[6])) return true;
            if (i < writeableBitmap.PixelWidth && j < writeableBitmap.PixelHeight && !GetMask(r[7])) return true;
            return false;
        }

        /// <summary>
        /// To be called after any processing so image is rewrote to the screen
        /// </summary>
        public async void Reload()
        {
            // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer
            using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
            {
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
        private int[][] NeighborsCoordinates(int i, int j, int color = 0)
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
            if (i > 0 && j > 0 && GetMask(r[0])) r[0][0] = -1; // if pixel on mask
            if (j > 0 && GetMask(r[1])) r[1][0] = -1; // count as outside
            if (i < writeableBitmap.PixelWidth && j > 0 && GetMask(r[2])) r[2][0] = -1;
            if (i > 0 && GetMask(r[3])) r[3][0] = -1;
            if (i < writeableBitmap.PixelWidth && GetMask(r[4])) r[4][0] = -1;
            if (i > 0 && j < writeableBitmap.PixelHeight && GetMask(r[5])) r[5][0] = -1;
            if (j < writeableBitmap.PixelHeight && GetMask(r[6])) r[6][0] = -1;
            if (i < writeableBitmap.PixelWidth && j < writeableBitmap.PixelHeight && GetMask(r[7])) r[7][0] = -1;
            return r;
        }

        /// <summary>
        /// Is pixel valid: i, j
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private bool NeighborCheck(int i, int j, int color = 0)
        {
            return !(i < 0
                || i >= writeableBitmap.PixelWidth
                || j < -1
                || j >= writeableBitmap.PixelHeight);
        }

        /// <summary>
        /// Is pixel valid: [i, j]
        /// </summary>
        /// <param name="index"></param>
        /// <param name="color"></param>
        /// <returns></returns>
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
        private int[] Neighbors(int i, int j, int color = 0)
        {
            List<int> neighbors = new List<int>();
            int[][] c = NeighborsCoordinates(i, j, color);
            for (int k = 0; k < 8; k++)
            {
                if (NeighborCheck(c[k]))
                {
                    neighbors.Add(this[c[k], color]);
                }
            }
            return neighbors.ToArray();
        }

        /// <summary>
        /// Custom erosion which set the mean value of neighbors to (i, j) if getMask(i, j) == true
        /// TODO: make it within while (mask != null) so it works smoother
        /// TODO: make it work
        /// </summary>
        public void ErosionMean()
        {
            for (int j = 0; j < writeableBitmap.PixelHeight; j++)
            {
                for (int i = 0; i < writeableBitmap.PixelWidth; i++)
                {
                    if (GetMask(i, j))
                    {
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

        /// <summary>
        /// FMM Inpainting initialisation.
        /// TODO: everything
        /// </summary>
        private void FMMInitialization()
        {
            for (int i = 0; i < writeableBitmap.PixelWidth; i++)
            {
                for (int j = 0; j < writeableBitmap.PixelHeight; j++)
                {
                    FMMPixelWithCoords P = new FMMPixelWithCoords();
                    P.i = i;
                    P.j = j;
                    
                    if (GetMask(i, j))
                    {
                        P.T = 1000000;
                        P.f = FMMPixel.Flag.INSIDE;
                        if (IsMaskBorder(i, j))
                        {
                            P.f = FMMPixel.Flag.BAND;
                            P.T = 0;

                            narrowBand.Add(P);
                        }
                    }
                    else
                    {
                        P.f = FMMPixel.Flag.KNOWN;
                        P.T = 0;
                    }
 
                }
            }
        }

        /// <summary>
        /// FMM Inpainting propagation algorithm.
        /// TODO: finish / fix method
        /// </summary>
        private void FMMPropagation()
        {
            while (narrowBand.Count > 0)
            {
                FMMPixelWithCoords P = narrowBand.Min();
                P.f = FMMPixel.Flag.KNOWN;
                for (int j = -1; j <= 1; j += 2)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        
                        FMMPixelWithCoords neighbor = (FMMPixelWithCoords)GetFMMPixel(P.i + i, P.j + j);
                        neighbor.i = P.i;
                        neighbor.j = P.j;
                        if (neighbor.f != FMMPixel.Flag.KNOWN)
                        {
                            if (neighbor.f == FMMPixel.Flag.INSIDE)
                            {
                                neighbor.f = FMMPixel.Flag.BAND;
                                Inpaint(neighbor.i, neighbor.j);

                            }

                            

                            neighbor.T = Math.Min( 
                                                    SolveEikonal(i-1, j, i, j-1),
                                                    Math.Min(
                                                             SolveEikonal(i+1, j, i, j-1),
                                                             Math.Min(
                                                                 SolveEikonal(i-1, j, i, j+1),
                                                                 SolveEikonal(i+1, j, i, j+1)
                                                             ))
                                                 );

                            narrowBand.Add(neighbor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Solve Eikonal equation.
        /// </summary>
        /// <param name="i1"></param>
        /// <param name="j1"></param>
        /// <param name="i2"></param>
        /// <param name="j2"></param>
        /// <returns></returns>
        private float SolveEikonal(int i1, int j1, int i2, int j2)
        {
            float sol = 1000000;
            FMMPixel P1 = GetFMMPixel(i1, j1);
            FMMPixel P2 = GetFMMPixel(i2, j2);
            FMMPixel P3 = GetFMMPixel(i1, j2);
            if (P1.f == FMMPixel.Flag.KNOWN)
            {
                if(P2.f == FMMPixel.Flag.KNOWN)
                {
                    double operation = 2 * (P1.T * P2.T) * (P1.T * P2.T);
                    float r = (float)Math.Sqrt(operation);
                    float s = (float)(((P1.T + P2.T) * r) / 2.0);
                    if(s >= P1.T && s >= P2.T)
                        sol = s;
                    else
                    {
                        s += r;
                        if(s >= P1.T && s >= P2.T)
                        {
                            sol = s;
                        }
                    }
                }
                else
                {
                    sol = 1 + P1.T;
                }
            }
            else if( P2.f == FMMPixel.Flag.KNOWN)
            {
                sol = 1 + P3.T;
            }
            return sol;
        }

        /// <summary>
        /// δΩi = boundary of region to inpaint
        /// δΩ = δΩi
        /// while (δΩ not empty) {
        ///     p = pixel of δΩ closest to δΩi
        ///     inpaint p using Eqn.2
        ///     advance δΩ into Ω
        /// }
        /// </summary>
        public void Inpaint(int i , int j)
        {
            foreach(FMMPixelWithCoords P in narrowBand){
                if(P.f == FMMPixel.Flag.INSIDE){
                    FMMPixel K = GetFMMPixel(i, j);
                    int x = i - P.i;
                    int y = j - P.j;
                  
                    Vector2 r = new Vector2(x, y);
                    Vector2 dir = r * gradT(i,j)/r.Length();
                    float dst = 1/(r.Length() * r.Length());
                    float lev = 1/(1+Math.Abs(P.T*K.T));
                    Vector2 w = dir * dst * lev;

                    FMMPixel P2 = GetFMMPixel(i+1, j);
                    FMMPixel P3 = GetFMMPixel(i-1, j);
                    FMMPixel P4 = GetFMMPixel(i, j+1);
                    FMMPixel P5 = GetFMMPixel(i, j-1);

                    if( 
                        P2.f == FMMPixel.Flag.INSIDE &&
                        P3.f == FMMPixel.Flag.INSIDE &&
                        P4.f == FMMPixel.Flag.INSIDE &&
                        P5.f == FMMPixel.Flag.INSIDE
                      )
                    {
                        Vector2 gradI = new Vector2((P2.I * P3.I), (P4.I * P5.I));
                    }
                    Vector2 Ia += w * (P.I + gradI * r);
                    Vector2 s += w;
                
                }
                P.I = Ia/s;
            }
        }
    }

}
