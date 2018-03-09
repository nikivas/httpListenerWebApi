using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    class FilterFlipH : IFilter
    {
        
        public FilterFlipH() { }

        public void draw(Bitmap img )
        {
            img.RotateFlip(RotateFlipType.RotateNoneFlipX);
        }
    }
}
