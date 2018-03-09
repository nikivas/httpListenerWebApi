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
        public unsafe static Bitmap Clip(string[] coordList, Bitmap img)
        {
            long polygonX0 = 0;
            long polygonWeight = 0;
            long polygonY0 = 0;
            long polygonHeight = 0;

            if (!Int64.TryParse(coordList[0], out polygonX0) || !Int64.TryParse(coordList[1], out polygonY0) ||
                !Int64.TryParse(coordList[2], out polygonWeight) || !Int64.TryParse(coordList[3], out polygonHeight))
            {
                return null;
            }

            long polygonX1 = polygonWeight + polygonX0;
            long polygonY1 = polygonHeight + polygonY0;

            BitmapCliper.getCorrectCoords(ref img, ref polygonX0, ref polygonY0, ref polygonX1, ref polygonY1);

            long resultImageHeight = Math.Abs(polygonY1 - polygonY0);
            long resultImageWidth = Math.Abs(polygonX1 - polygonX0);

            if (resultImageHeight == 0 && resultImageWidth == 0)
            {
                throw new FormatException();
            }
            Int64 resultImagePointX0 = Math.Min(polygonX0, polygonX1);
            Int64 resultImagePointY0 = Math.Min(polygonY0, polygonY1);


            var resultImage = BitmapCliper._Clip(img, (int)resultImagePointX0, (int)resultImagePointY0,
                                                (int)resultImageWidth, (int)resultImageHeight);
            return resultImage;

        }

        public unsafe static Bitmap _Clip(Bitmap img, int startWidth, int startHeight, int newWidth, int newHeight)
        {
            if (startWidth < 0 || startHeight < 0 || startWidth > img.Width || startHeight > img.Height)
                return img;
            if (startWidth + newWidth < 0 || startHeight + newHeight < 0 || startWidth + newWidth > img.Width || startHeight + newHeight > img.Height)
                return img;

            int width = newWidth;
            int height = newHeight;
            byte[,,] result = new byte[4, height, width];
            //var a = DateTime.Now;

            Bitmap resultBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData resultBitmapbytes = resultBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            BitmapData bd = img.LockBits(new Rectangle(startWidth, startHeight, newWidth, newHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

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

                    for (int h = 0; h < newHeight; h++)
                    {
                        curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                        resultBitmapPos = ((byte*)resultBitmapbytes.Scan0) + h*resultBitmapbytes.Stride;
                        for (int w = 0; w < newWidth; w++)
                        {
                            *(resultBitmapPos++) = *(curpos++);
                            *(resultBitmapPos++) = *(curpos++);
                            *(resultBitmapPos++) = *(curpos++);
                            *(resultBitmapPos++) = *(curpos++);
                        }
                    }
                }
            }
            finally
            {
                img.UnlockBits(bd);
                resultBitmap.UnlockBits(resultBitmapbytes);
            }
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
