using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace MovementManager.InputProcess
{
    public static class ImageLoader
    {
        static SolidBrush whiteBrush = new SolidBrush(Color.White);
        public static void SaveImage(
            Image img,
            string path,
            ImageFormat imageFormat,
            FileMode fileMode = FileMode.Create)
        {
            img.Save(new FileStream(path, fileMode), imageFormat);
        }

        public static Bitmap LoadImage(string path, int width, int height)
        {
            Image image = LoadImage(path);
            return ResizeImage(image, width, height);
        }
        public static Image LoadImage(string path)
        {
            return Image.FromFile(path);
        }
        public static Bitmap LoadImageBitmap(string path)
        {
            return (Bitmap) Bitmap.FromFile(path);
        }
        // <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.FillRectangle(whiteBrush, BgRectangle(width, height));
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private static Rectangle BgRectangle(int width, int height)
        {
            return new Rectangle(0, 0, width, height);
        }


        public static int[][] GetBlackAndWhiteImageCode(Bitmap image)
        {
            int[][] imageCode = new int[image.Width][]; //The array containing the binaries
            for (int i = 0; i < image.Width; i++)
            {
                imageCode[i] = new int[image.Height];
                for (int j = 0; j < image.Height; j++)
                {
                    Color c = image.GetPixel(i, j);
                    // check if black
                    if ( c.GetBrightness() == 0) imageCode[i][j] = 1;
                }
            }
            return imageCode;
        }



    }
}