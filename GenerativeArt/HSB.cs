using System;
using System.Windows.Media;

namespace GenerativeArt
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Hue Saturation Brightness. </summary>
    ///
    /// <remarks>   Hue is 0-360, Sat and Brightness are 0.0-1.0
    ///             Darrell Plank, 4/25/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal struct HSB
    {
        public double H { get; init; }
        public double S { get; init; }
        public double B { get; init; }

        public HSB(double h, double s, double b)
        {
            H = h;
            S = s;
            B = b;
        }

        public HSB(Color color)
        {
            (H, S, B) = ColorToHSB(color);
        }

        public Color ToColor()
        {
            return ColorFromHSB(H, S, B);
        }

        public static (double hue, double saturation, double brightness) ColorToHSB(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            var hue = GetHue(color);
            var saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            var brightness = max / 255d;

            return (hue, saturation, brightness);
        }

        public static double GetHue(Color color)
        {

            double min = Math.Min(Math.Min(color.R, color.G), color.B);
            double max = Math.Max(Math.Max(color.R, color.G), color.B);

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (min == max)
            {
                return 0;
            }

            double hue = 0;
            if (max == color.R)
            {
                hue = (color.G - color.B) / (max - min);

            }
            else if (max == color.G)
            {
                hue = 2f + (color.B - color.R) / (max - min);

            }
            else
            {
                hue = 4f + (color.R - color.G) / (max - min);
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator

            hue = hue * 60;
            if (hue < 0) hue = hue + 360;

            return hue;
        }

        public static Color ColorFromHSB(double hue, double saturation, double brightness)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            brightness = brightness * 255;
            var v = Convert.ToByte(brightness);
            var p = Convert.ToByte(brightness * (1 - saturation));
            var q = Convert.ToByte(brightness * (1 - f * saturation));
            var t = Convert.ToByte(brightness * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q)
            };
        }

    }
}
