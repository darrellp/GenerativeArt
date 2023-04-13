using System;
using System.Linq;

namespace GenerativeArt
{
    // Based on the article here:
    //  https://adrianb.io/2014/08/09/perlinnoise.html

    public class Perlin
    {
        private readonly int[] _p;
        public static int Repeat => 256;
        public int Octaves { get; init; } = 3;
        public double Persistence { get; init; } = 1;
        public double Frequency { get; init; } = 1;

        public Perlin(int seed = -1)
        {
            _p = Permutations(seed);
        }

        private static int[] Permutations(int seed = -1)
        {
            var rnd = seed < 0 ? new Random() : new Random(seed);

            var perm256 = Enumerable.Range(0, 256).ToArray();
            for (var i = 0; i < 255; i++)
            {
                var iSwap = rnd.NextInt64(i, 255);
                (perm256[i], perm256[iSwap]) = (perm256[iSwap], perm256[i]);
            }
            return Enumerable.Range(0, 512).Select(i => perm256[i & 255]).ToArray();
        }

        public double Value(double x, double y, double z = 0)
        {
            var total = 0.0;
            var frequency = Frequency;
            var amplitude = 1.0;
            var maxValue = 0.0;

            for (var i = 0; i < Octaves; i++)
            {
                total += BaseValue(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= Persistence;
                frequency *= 2;
            }
            return total / maxValue;
        }

        private double BaseValue(double x, double y, double z = 0)
        {
            if (Repeat > 0)
            {                                    // If we have any repeat on, change the coordinates to their "local" repetitions
                x = x % Repeat;
                y = y % Repeat;
                z = z % Repeat;
            }

            var xi = (int)x & 255;                              // Calculate the "unit cube" that the point asked will be located in
            var yi = (int)y & 255;                              // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            var zi = (int)z & 255;                              // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            var xf = x - (int)x;
            var yf = y - (int)y;
            var zf = z - (int)z;

            var u = Fade(xf);
            var v = Fade(yf);
            var w = Fade(zf);

            var aaa = _p[_p[_p[xi] + yi] + zi];
            var aba = _p[_p[_p[xi] + Inc(yi)] + zi];
            var aab = _p[_p[_p[xi] + yi] + Inc(zi)];
            var abb = _p[_p[_p[xi] + Inc(yi)] + Inc(zi)];
            var baa = _p[_p[_p[Inc(xi)] + yi] + zi];
            var bba = _p[_p[_p[Inc(xi)] + Inc(yi)] + zi];
            var bab = _p[_p[_p[Inc(xi)] + yi] + Inc(zi)];
            var bbb = _p[_p[_p[Inc(xi)] + Inc(yi)] + Inc(zi)];

            var x1 = Lerp(Grad(aaa, xf, yf, zf), Grad(baa, xf - 1, yf, zf), u);
            var x2 = Lerp(Grad(aba, xf, yf - 1, zf), Grad(bba, xf - 1, yf - 1, zf), u);
            var y1 = Lerp(x1, x2, v);

            x1 = Lerp(Grad(aab, xf, yf, zf - 1), Grad(bab, xf - 1, yf, zf - 1), u);
            x2 = Lerp(Grad(abb, xf, yf - 1, zf - 1), Grad(bbb, xf - 1, yf - 1, zf - 1), u);
            var y2 = Lerp(x1, x2, v);

            // Transform from [-1, 1] range to [0, 1] range
            return (Lerp(y1, y2, w) + 1) / 2;
        }

        private static double Grad(int hash, double x, double y, double z)
        {
            switch (hash & 0xF)
            {
                case 0x0: return x + y;
                case 0x1: return -x + y;
                case 0x2: return x - y;
                case 0x3: return -x - y;
                case 0x4: return x + z;
                case 0x5: return -x + z;
                case 0x6: return x - z;
                case 0x7: return -x - z;
                case 0x8: return y + z;
                case 0x9: return -y + z;
                case 0xA: return y - z;
                case 0xB: return -y - z;
                case 0xC: return y + x;
                case 0xD: return -y + z;
                case 0xE: return y - x;
                case 0xF: return -y - z;
                default: return 0; // never happens
            }
        }

        private static double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private int Inc(int num)
        {
            return (num + 1) % Repeat;
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + t * (b - a);
        }
    }
}
