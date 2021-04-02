namespace m1_image_projet.Source
{
    public sealed partial class Inpainting
    {
        public class FMMPixel
        {
            private float T; // value
            private float I; // gray value
            private flag f; // flag

            private enum flag
            {
                BAND,
                KNOWN,
                INSIDE,
            }

        }
    }

}
