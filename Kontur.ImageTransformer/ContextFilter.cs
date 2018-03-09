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
        public const int BITS_PER_PIXEL = 32;
        public const int MAX_PICTURE_SIZE = 100 * 1024;
        public static bool IsContextCorrect(Bitmap listenerContext)
        {

            if (listenerContext.Size.Height > 1000 || listenerContext.Size.Width > 1000)
                return false;
            if (listenerContext.Size.Height * listenerContext.Size.Width * BITS_PER_PIXEL > MAX_PICTURE_SIZE)
                return false;
            return true;
        }
    }
}
