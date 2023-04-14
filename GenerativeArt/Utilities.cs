using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GenerativeArt
{
    internal static class Utilities
    {
        internal static Color LerpColor(Color color1, Color color2, double t)
        {
            var r = color1.R * (1 - t) + color2.R * t;
            var g = color1.G * (1 - t) + color2.G * t;
            var b = color1.B * (1 - t) + color2.B * t;
            return new Color() { R = (byte)r, G = (byte)g, B = (byte)b };
        }
    }
}
