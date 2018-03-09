using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Kontur.ImageTransformer
{
    class FilterBuilder
    {
        public static IFilter urlParse(string rawUrl)
        {
            var url = rawUrl.Split('/');
            if (url[1] != @"process")
                return null;
            else if (!Regex.IsMatch(url[3], @"^ *(-?[0-9]*,){3}-?[0-9]* *$"))
                return null;

            url[2] = url[2].ToLower();

            if (url[2] == @"rotate-cw")
                return new FilterRotateCW();

            else if (url[2] == @"rotate-ccw")
                return new FilterRotateCCW();

            else if (url[2] == @"flip-h")
                return new FilterFlipH();

            else if (url[2] == @"flip-v")
                return new FilterFlipV();

            return null;
        }
    }
}
