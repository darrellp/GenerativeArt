using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GenerativeArt
{
    internal static class Utilities
    {
        internal static double Lerp(double val1, double val2, double t)
        {
            return val1 + (val2 - val1) * t;
        }

        internal static Point Lerp(Point p1, Point p2, double t)
        {
            return p1 + (p2 - p1) * t;
        }

        internal static Color Lerp(Color color1, Color color2, double t)
        {
            var r = Lerp(color1.R, color2.R, t);
            var g = Lerp(color1.G, color2.G, t);
            var b = Lerp(color1.B, color2.B, t);
            return new Color() { A = 255, R = (byte)r, G = (byte)g, B = (byte)b };
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Distance between two points </summary>
        ///
        /// <remarks>   Darrell Plank, 4/26/2023. </remarks>
        ///
        /// <param name="pt1">  The first point. </param>
        /// <param name="pt2">  The second point. </param>
        ///
        /// <returns>   Distance between pt1 and pt2. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal static double Dist(Point pt1, Point pt2)
        {
            return Math.Sqrt(Dist2(pt1, pt2));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Distance squared. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/26/2023. </remarks>
        ///
        /// <param name="pt1">  The first point. </param>
        /// <param name="pt2">  The second point. </param>
        ///
        /// <returns>   Squared distance between two points. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal static double Dist2(Point pt1, Point pt2)
        {
            var dx = pt1.X - pt2.X;
            var dy = pt1.Y - pt2.Y;
            return dx * dx + dy * dy;
        }

        internal static double Norm(Point pt) => pt.X * pt.X + pt.Y * pt.Y;
    }
}
