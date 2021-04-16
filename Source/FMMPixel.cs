using System.Collections.Generic;

namespace m1_image_projet.Source
{
    public sealed partial class Inpainting
    {
        /// <summary>
        /// Type de pixel utilisé pour le masque pour FMM
        /// </summary>
        public class FMMPixel
        {
            public float T; // value
            public float I; // gray value
            public Flag f; // flag

            public enum Flag
            {
                BAND,
                KNOWN,
                INSIDE,
            }

        }

        /// <summary>
        /// Type de pixel utilisé pour le masque pour FMM dans la narrowband
        /// </summary>
        public class FMMPixelWithCoords : FMMPixel
        {
            public int i;
            public int j;
        }

        /// <summary>
        /// So that NarrowBand SortedSet can work easily.
        /// </summary>
        public class ByTValues : IComparer<FMMPixel>
        {
            public int Compare(FMMPixel x, FMMPixel y)
            {
                if (x.T == y.T)
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
        }

    }

}