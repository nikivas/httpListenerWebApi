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
            var byteArray = getByteArray(img, startWidth, startHeight, newWidth, newHeight, filter);
            var resultBitmap = getBitmap(byteArray);
            return resultBitmap;
        }

        private unsafe static byte[,,] getByteArray(Bitmap img, int startWidth, int startHeight, 
                int newWidth, int newHeight, IFilter filter)
        {
            int width = newWidth;
            int height = newHeight;
            byte[,,] result = new byte[4, height, width];

            point pixelColorCounter = new point();
            

            BitmapData bd = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                byte* curpos;
                fixed (byte* resultCounter = result)
                {
                    byte* alphaCounter = resultCounter;
                    byte* redCounter = resultCounter + width * height;
                    byte* greenCounter = resultCounter + 2 * width * height;
                    byte* blueCounter = resultCounter + 3 * width * height;

                    for (int h = startHeight; h < newHeight; h++)
                    {
                        
                        curpos = ((byte*)bd.Scan0) + h * bd.Stride + startWidth;
                        for (int w = startWidth; w < newWidth; w++)
                        {
                            pixelColorCounter.blue= *(curpos++); 
                            pixelColorCounter.green = *(curpos++); 
                            pixelColorCounter.red = *(curpos++);
                            pixelColorCounter.alpha = *(curpos++); 

                            if(filter != null)
                                filter.draw(ref pixelColorCounter);
                            
                            *blueCounter = pixelColorCounter.blue; ++blueCounter; // not alpha
                            *greenCounter = pixelColorCounter.green; ++greenCounter; // not alpha
                            *redCounter = pixelColorCounter.red; ++redCounter; // not alpha
                            *alphaCounter = pixelColorCounter.alpha; ++alphaCounter; // alpha


                        }
                    }
                }
            }
            finally
            {
                img.UnlockBits(bd);
            }
            return result;
        }

        private unsafe static Bitmap getBitmap(byte[,,] array)
        {
            int width = array.GetLength(2),
                height = array.GetLength(1);

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData bd = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                byte* curpos;
                for (int h = 0; h < height; h++)
                {
                    curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                    for (int w = 0; w < width; w++)
                    {
                        *(curpos++) = array[3, h, w]; // alpha
                        *(curpos++) = array[2, h, w]; // red
                        *(curpos++) = array[1, h, w]; // green
                        *(curpos++) = array[0, h, w]; // blue
                    }
                }
            }
            finally
            {
                result.UnlockBits(bd);
            }

            return result;
        }

    }
}
