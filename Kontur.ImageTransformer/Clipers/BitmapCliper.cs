using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kontur.ImageTransformer
{
    public class point
    {

        public point()
        {
            blue = 0;
            alpha = 0;
            green = 0;
            red = 0;
         }

        public byte red { get; set; }

        public byte green { get; set; }

        public byte blue { get; set; }

        public byte alpha { get; set; }

    }

    class BitmapCliper
    {
        
        public unsafe static Bitmap Clip(Bitmap img, int startWidth, int startHeight, int newWidth, int newHeight, IFilter filter)
        {
            if (startWidth < 0 || startHeight < 0 || startWidth > img.Width || startHeight > img.Height)
                return img;
            if (startWidth + newWidth < 0 || startHeight + newHeight < 0 || startWidth + newWidth > img.Width || startHeight + newHeight > img.Height)
                return img;

            int width = newWidth;
            int height = newHeight;
            byte[,,] result = new byte[4, height, width];
            var a = DateTime.Now;
            point pixelColorCounter = new point();

            Bitmap resultBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData resultBitmapbytes = resultBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            BitmapData bd = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                byte* curpos;
                byte* resultBitmapPos;
                fixed (byte* resultCounter = result)
                {
                    byte* alphaCounter = resultCounter;
                    byte* redCounter = resultCounter + width * height;
                    byte* greenCounter = resultCounter + 2 * width * height;
                    byte* blueCounter = resultCounter + 3 * width * height;

                    for (int h = startHeight; h < newHeight; h++)
                    {
                        curpos = ((byte*)bd.Scan0) + h * bd.Stride + startWidth;
                        resultBitmapPos = ((byte*)resultBitmapbytes.Scan0) + (h - startHeight)*resultBitmapbytes.Stride;
                        for (int w = startWidth; w < newWidth; w++)
                        {

                            pixelColorCounter.blue = *(curpos++);
                            pixelColorCounter.green = *(curpos++);
                            pixelColorCounter.red = *(curpos++);
                            pixelColorCounter.alpha = *(curpos++);

                            if (filter != null)
                                filter.draw(ref pixelColorCounter);

                            *(resultBitmapPos++) = pixelColorCounter.blue; ++blueCounter; 
                            *(resultBitmapPos++) = pixelColorCounter.green; ++greenCounter;
                            *(resultBitmapPos++) = pixelColorCounter.red; ++redCounter; 
                            *(resultBitmapPos++) = pixelColorCounter.alpha; ++alphaCounter;
                        }
                    }
                }
            }
            finally
            {
                img.UnlockBits(bd);
                resultBitmap.UnlockBits(resultBitmapbytes);
            }
            var b = DateTime.Now;
            Console.WriteLine((b - a).Milliseconds);
            return resultBitmap;
        }
        
        public static void getCorrectCoords(ref Bitmap img, ref long coordX0, ref long coordY0, ref long coordX1, ref long coordY1)
        {
            coordY0 = Math.Max(0, coordY0);
            coordY0 = Math.Min(coordY0, img.Height);

            coordX0 = Math.Max(0, coordX0);
            coordX0 = Math.Min(coordX0, img.Width);

            coordY1 = Math.Max(0, coordY1);
            coordY1 = Math.Min(coordY1, img.Height);

            coordX1 = Math.Max(0, coordX1);
            coordX1 = Math.Min(coordX1, img.Width);
        }
    }
}
