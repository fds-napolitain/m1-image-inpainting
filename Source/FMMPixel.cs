using System.Collections.Generic;

namespace m1_image_projet.Source
{
    public sealed partial class Inpainting
    {
        public class FMMPixel
        {
            public float T; // value
            public float I; // gray value
            public flag f; // flag
            public int i,j;

            public enum flag
            {
                BAND,
                KNOWN,
                INSIDE,
            }

        }



        public class ByTValues : IComparer<FMMPixel>
        {
            public int Compare(FMMPixel x, FMMPixel y)
            {
                if (x.T == y.T) {
                    return 0;
                } else if (x.T < y.T) {
                    return -1;
                } else {
                    return 1;
                }
            }
        }

        public class ByIValues : IComparer<FMMPixel>
        {
            public int Compare(FMMPixel x, FMMPixel y)
            {
                if (x.I == y.I) {
                    return 0;
                } else if (x.I < y.I) {
                    return -1;
                } else {
                    return 1;
                }
            }
        }

    }

}
