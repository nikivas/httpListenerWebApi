using System.Drawing;

namespace Kontur.ImageTransformer
{
    interface IFilter
    {

        int draw(ref byte[,,] array, int );
        
    }
}