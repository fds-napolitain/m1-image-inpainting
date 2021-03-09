using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using m1_image_projet.Source;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Diagnostics;
using Windows.System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace m1_image_projet
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Inpainting inpainting;

        public MainPage()
        {
            inpainting = new Inpainting();
            this.InitializeComponent();
        }

        /// <summary>
        /// Start copying image for the drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        /// <summary>
        /// Load image in document and updates values of pixels for Inpainting main processing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0) {
                    StorageFile storageFile = items[0] as StorageFile;
                    IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read);
                    inpainting.WriteableBitmap.SetSource(fileStream);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

                    // Scale image to appropriate size
                    BitmapTransform transform = new BitmapTransform()
                    {
                        ScaledWidth = Convert.ToUInt32(inpainting.WriteableBitmap.PixelWidth),
                        ScaledHeight = Convert.ToUInt32(inpainting.WriteableBitmap.PixelHeight)
                    };

                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,    // WriteableBitmap uses BGRA format
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.IgnoreExifOrientation, // This sample ignores Exif orientation
                        ColorManagementMode.DoNotColorManage);

                    // An array containing the decoded image data, which could be modified before being displayed
                    inpainting.pixels = pixelData.DetachPixelData();
                    inpainting.mask = new System.Collections.BitArray(inpainting.WriteableBitmap.PixelWidth * inpainting.WriteableBitmap.PixelHeight);
                    Image.Source = inpainting.WriteableBitmap;
                }
            }
        }

        /// <summary>
        /// Update initial position of pixel for Inpainting mask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            inpainting.mask_position[0] = (int) (e.GetPosition(Image).X / Image.ActualWidth * inpainting.WriteableBitmap.PixelWidth);
            inpainting.mask_position[1] = (int) (e.GetPosition(Image).Y / Image.ActualHeight * inpainting.WriteableBitmap.PixelHeight);
            inpainting.SetMask();
        }

        /// <summary>
        /// Update sensitivity of mask (wheel up bigger, wheel down smaller)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint((Image)sender).Properties.MouseWheelDelta >= 0) {
                inpainting.sensitivity += 2;
            } else {
                inpainting.sensitivity -= 2;
            }
            inpainting.SetMask();
        }

        /// <summary>
        /// Replace mask with background with repeated erosions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if ((e.Key == VirtualKey.Enter || e.Key == VirtualKey.Back) && inpainting.mask_position[0] != -1) {
                inpainting.ReplaceMask();
            }
        }
    }
}
