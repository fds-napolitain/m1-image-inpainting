using System.Collections.Generic;

namespace m1_image_projet.Source
{
    public sealed partial class Inpainting
    {
        /// <summary>
        /// Coordonées
        /// </summary>
        public class Coords
        {
            public int i;
            public int j;
        }
        /*
        /// <summary>
        /// So that NarrowBand SortedSet can work easily.
        /// </summary>
        public class ByTValues : IComparer<Coords>
        {
            public int Compare(Coords x, Coords y)
            {
                if (x[x.i, x.j].T == y[i, j].T)
                {
                    return 0;
                }
                else if (x.T < y.T)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }


        /// <summary>
        /// So that NarrowBand SortedSet can work easily.
        /// </summary>
        public class ByIValues : IComparer<FMMPixel>
        {
            public int Compare(FMMPixel x, FMMPixel y)
            {
                if (x.I == y.I)
                {
                    return 0;
                }
                else if (x.I < y.I)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }*/

    }

}
