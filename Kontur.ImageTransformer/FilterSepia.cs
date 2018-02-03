using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    class FilterSepia : IFilter
    {
        public Bitmap image { get; set; }
        public FilterSepia() { }
        public FilterSepia(Bitmap image)
        {
            this.image = image;
        }

        public void draw(ref point pnt)
        {
            if (pnt == null)
                return;
            int afterRed =   (int) ((pnt.red * .393f) + (pnt.green * .769f) + (pnt.blue * .189f));
            int afterGreen = (int) ((pnt.red * .349f) + (pnt.green * .686f) + (pnt.blue * .168f));
            int afterBlue =  (int) ((pnt.red * .272f) + (pnt.green * .534f) + (pnt.blue * .131f));

            pnt.red = afterRed > 255 ? (byte) 255 : (byte) afterRed;
            pnt.green = afterGreen > 255 ? (byte)255 : (byte)afterGreen;
            pnt.blue = afterBlue > 255 ? (byte)255 : (byte)afterBlue;
        }
    }
}
