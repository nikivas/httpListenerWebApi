using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kontur.ImageTransformer
{
    class FilterGrayScale : IFilter
    {
        public Bitmap image { get; set; }
        public FilterGrayScale() { }
        public FilterGrayScale(Bitmap image)
        {
            this.image = image;
        }

        public void draw(ref point pnt)
        {
            if (pnt == null)
                return;
            int average = (pnt.green + pnt.blue + pnt.red) / 3;

            pnt.red = (byte) average;
            pnt.blue = (byte)average;
            pnt.green = (byte)average;
        }



    }
}
