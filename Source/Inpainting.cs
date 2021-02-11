using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace m1_image_projet.Source
{
    class Inpainting
    {
        private WriteableBitmap writeableBitmap;

        public Inpainting()
        {
            this.WriteableBitmap = new WriteableBitmap(100, 100);
        }

        public WriteableBitmap WriteableBitmap { get => writeableBitmap; set => writeableBitmap = value; }
    }
}
