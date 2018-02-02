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

        public void draw()
        {
            
        }
    }
}
