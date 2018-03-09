using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    class FilterRotateCCW : IFilter
    {
        
        public FilterRotateCCW() { }

        public void draw(Bitmap img)
        {
            img.RotateFlip(RotateFlipType.Rotate270FlipNone);
        }
    }
}
