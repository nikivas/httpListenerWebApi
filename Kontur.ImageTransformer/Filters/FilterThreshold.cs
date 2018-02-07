using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    class FilterThreshold : IFilter
    {

        public int value { get; set; }
        
        public FilterThreshold()
        {
            this.value = 0;
        }

        public FilterThreshold(int value)
        {
            this.value = value;
        }

        public void draw(ref point pnt )
        {
            if (pnt == null)
                return;
            int average = (pnt.blue + pnt.green + pnt.red) / 3;

            if (average>= 255 * value / 100)
            {
                pnt.blue = 255;
                pnt.green = 255;
                pnt.red = 255;
            }
            else
            {
                pnt.blue = 0;
                pnt.green = 0;
                pnt.red = 0;
            }
            
        }
    }
}
