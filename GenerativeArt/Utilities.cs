using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace GenerativeArt
{
    internal static class Utilities
    {
        internal static void Shuffle<T>(IList<T> vals, Random rnd)
        {
            for (var i = 0; i < vals.Count; i++)
            {
                var swapIndex = i + rnd.Next(vals.Count - i);
                (vals[i], vals[swapIndex]) = (vals[swapIndex], vals[i]);
            }
        }

        internal static double Lerp(double val1, double val2, double t)
        {
            return val1 + (val2 - val1) * t;
        }

        internal static double LerpAngle(double angle1, double angle2, double t)
        {
            var flipStartEnd = false;
            var small = MathRemainder(angle1, 2 * Math.PI);
            var large = MathRemainder(angle2, 2 * Math.PI);

            // Put them in the proper order
            if (small > large)
            {
                (small, large) = (large, small);
                flipStartEnd = !flipStartEnd;
            }

            // Determine if we need to go the "other way" around the circle
            if (large - small > Math.PI)
            {
                (small, large) = (large, small + 2 * Math.PI);
                flipStartEnd = !flipStartEnd;
            }

            return small + (large - small) * (flipStartEnd ? (1 - t) : t);
        }

        // Does a mathematical remainder, putting x in range [0, r)
        internal static double MathRemainder(double x, double r)
        {
            int mult = x < 0 ?
                (int)Math.Ceiling((-x) / r) :
                -(int)Math.Floor(x / r);
            return x + mult * r;
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
