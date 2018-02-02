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
        public Bitmap image { get; set; }
        public FilterThreshold() { }
        public FilterThreshold(Bitmap image)
        {
            this.image = image;
        }

        public void draw()
        {
            
        }
    }
}
