using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    public class ContextFilter
    {
        public const int MAX_PICTURE_SIZE = 100 * 1024;
        public static bool IsContextCorrect(Bitmap img, HttpListenerContext listenerContext)
        {
            if (img.Size.Height > 1000 || img.Size.Width > 1000)
                return false;
            else if (listenerContext.Request.ContentLength64 > MAX_PICTURE_SIZE)
                return false;
            else if (img.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                return false;
            return true;
        }
    }
}
