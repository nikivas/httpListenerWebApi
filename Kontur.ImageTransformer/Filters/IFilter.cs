using System.Drawing;

namespace Kontur.ImageTransformer
{
    interface IFilter
    {
        void draw(ref point pnt );   
    }
}