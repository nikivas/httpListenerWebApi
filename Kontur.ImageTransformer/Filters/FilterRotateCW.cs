using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kontur.ImageTransformer
{
    class FilterRotateCW : IFilter
    {
        
        public FilterRotateCW() { }
        
        public void draw(Bitmap img)
        {
            img.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }



    }
}
