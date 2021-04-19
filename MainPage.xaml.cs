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
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        /// <summary>
        /// Start copying image for the drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            Debug.WriteLine("Copy image.");
        }

        /// <summary>
        /// Load image in document and updates values of pixels for Inpainting main processing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    StorageFile storageFile = items[0] as StorageFile;
                    IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read);
                    inpainting.WriteableBitmap.SetSource(fileStream);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

                    // Scale image to appropriate size
                    BitmapTransform transform = new BitmapTransform() {
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
                    inpainting.SetPixels(pixelData.DetachPixelData());
                    inpainting.mask = new System.Collections.BitArray(inpainting.WriteableBitmap.PixelWidth * inpainting.WriteableBitmap.PixelHeight);
                    //inpainting.fmmpixels = new byte[inpainting.mask.Count];
                    Image.Source = inpainting.WriteableBitmap;
                }
            }
            Debug.WriteLine("Show image and creates Inpainting object.");
        }

        /// <summary>
        /// Update initial position of pixel for Inpainting mask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            inpainting.mask_position[0] = (int)(e.GetPosition(Image).X / Image.ActualWidth * inpainting.WriteableBitmap.PixelWidth);
            inpainting.mask_position[1] = (int)(e.GetPosition(Image).Y / Image.ActualHeight * inpainting.WriteableBitmap.PixelHeight);
            inpainting.SetMask();
            Debug.WriteLine("Set mask position.");
        }

        /// <summary>
        /// Update sensitivity of mask (wheel up bigger, wheel down smaller)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint((Image)sender).Properties.MouseWheelDelta >= 0)
            {
                if (inpainting.sensitivityColor < 3)
                {
                    inpainting.sensitivity[inpainting.sensitivityColor] += 2;
                }
                else
                {
                    inpainting.sensitivity[0] += 2;
                    inpainting.sensitivity[1] += 2;
                    inpainting.sensitivity[2] += 2;
                }
            }
            else
            {
                if (inpainting.sensitivityColor < 3)
                {
                    inpainting.sensitivity[inpainting.sensitivityColor] -= 2;
                }
                else
                {
                    inpainting.sensitivity[0] -= 2;
                    inpainting.sensitivity[1] -= 2;
                    inpainting.sensitivity[2] -= 2;
                }
            }
            inpainting.SetMask();
            Debug.WriteLine("Change sensitivity to " + inpainting.sensitivity[0] + " " + inpainting.sensitivity[1] + " " + inpainting.sensitivity[2] + ".");
        }

        /// <summary>
        /// Hotkeys association with action.
        /// b => replace by blue pixels only (debugging purpose)
        /// enter => apply inpainting using naive method (erode mean)
        /// delete => apply inpainting using fast marching method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.B)
            {
                inpainting.ShowMask();
                Debug.WriteLine("Show mask.");
            }
            else if (e.VirtualKey == VirtualKey.Delete)
            {
                //inpainting.Inpaint();
                Debug.WriteLine("Replace mask by neighbors (FMM).");
            }
            else if (e.VirtualKey == VirtualKey.Enter)
            {
                inpainting.ErosionMean();
                Debug.WriteLine("Replace mask by neighbors (naive).");
            }
            else if (e.VirtualKey == VirtualKey.S)
            {
                SaveImage();
                Debug.WriteLine("Save image for latter usages.");
            } 
            else if (e.VirtualKey == VirtualKey.V)
            {
                inpainting.sensitivityColor++;
                inpainting.sensitivityColor %= 4;
                Debug.WriteLine("Sensitivity cursor set to " + inpainting.sensitivityColor);
            }
            inpainting.Reload();
        }

        /// <summary>
        /// Save image as PNG
        /// </summary>
        private async System.Threading.Tasks.Task SaveImage()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("image.png", CreationCollisionOption.ReplaceExisting);
            using (var ras = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                Stream stream = inpainting.WriteableBitmap.PixelBuffer.AsStream();
                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, buffer.Length);
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ras);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)inpainting.WriteableBitmap.PixelWidth, (uint)inpainting.WriteableBitmap.PixelHeight, 96.0, 96.0, buffer);
                await encoder.FlushAsync();
            }
            Debug.WriteLine("Image written to " + localFolder.Path);
        }
    }
}
