using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kontur.ImageTransformer
{
    class BitmapCliper
    {
        public unsafe static Bitmap bpp_32Argb_Clip(Bitmap img, int startWidth, int startHeight, int newWidth, int newHeight, IFilter filter)
        {
            
            return getBitmapFromBytes_32bppArgb(clip_32bppArgb(img, startWidth, startHeight, newWidth, newHeight),filter);
        }

        private unsafe static byte[,,] clip_32bppArgb(Bitmap img, int startWidth, int startHeight, int newWidth, int newHeight)
        {
            int width = newWidth;
            int height = newHeight;
            byte[,,] res = new byte[4, height, width];
            BitmapData bd = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                byte* curpos;
                fixed (byte* _res = res)
                {
                    byte* _r = _res;
                    byte* _g = _res + width * height;
                    byte* _b = _res + 2 * width * height;
                    byte* _a = _res + 3 * width * height;

                    for (int h = startHeight; h < newHeight; h++)
                    {
                        
                        curpos = ((byte*)bd.Scan0) + h * bd.Stride + startWidth;
                        for (int w = startWidth; w < newWidth; w++)
                        {
                            *_a = *(curpos++); ++_a;
                            *_b = *(curpos++); ++_b;
                            *_g = *(curpos++); ++_g;
                            *_r = *(curpos++); ++_r;
                            
                            
                        }
                    }
                }
            }
            finally
            {
                img.UnlockBits(bd);
            }
            return res;
        }


        private unsafe static Bitmap getBitmapFromBytes_32bppArgb(byte[,,] array, IFilter filter)
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
                        *(curpos++) = array[3, h, w];
                        *(curpos++) = array[2, h, w];
                        *(curpos++) = array[1, h, w];
                        *(curpos++) = array[0, h, w];
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
