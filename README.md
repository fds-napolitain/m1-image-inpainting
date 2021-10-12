# Inpainting project, 4th year IMAGINA MASTER

### Naïve method

After studying morphological operations on images, we were to find a naive method at least to inpaint an image (remove objects in it, aka photoshop).
My friend and I chose to make an UWP app for Windows 10, so we get an easy interface for both UI and data manipulation. The app features drag & drop, click and keyboards input to easily work on the image, without distractions.
To select an area to inpaint, you must click on a pixel, use mouse wheel to change sensitivity of one or more colors, which then will be used to select the area with a magic wand (Flood fill algorithm).

You can see our result here :

https://user-images.githubusercontent.com/18146363/137022973-8fcb5589-d886-4b9c-9d25-7ebb19f86176.mov

### Fast marching method

Even though we didn't finish it yet (we may or may not finish it), we also started to think of implementing the FMM to inpaint the image with less artifacts.
