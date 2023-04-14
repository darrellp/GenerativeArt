using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MathNet.Numerics.Distributions;
using static GenerativeArt.Utilities;

namespace GenerativeArt.CrabNebula
{
    internal class CrabNebula
    {
        private const double NoiseScale = 800.0;
        private const double StdDev = 0.15;
        private const double Mean = 0.5;
        private const int CPoints = 6_000_000;
        private const int BandCount = 8;
        private const double SqrtTwo = 1.41421356237;
        private const double Frequency = 1.5;
        private const double Persistence = 5;
        private const int Octaves = 3;

        private readonly WriteableBitmap _wbmp;
        private readonly int _width;
        private readonly int _height;
        private readonly Perlin _noise = new Perlin()
        {
            Frequency = Frequency, 
            Persistence = Persistence,
            Octaves = Octaves,
        };
        private readonly Normal _distNormal;
        private readonly Color _clrInner = Colors.Red;
        private readonly Color _clrOuter = Colors.Yellow;


        internal CrabNebula(WriteableBitmap wbmp)
        {
            _wbmp = wbmp;
            _width = (int)_wbmp.Width;
            _height = (int)_wbmp.Height;
            _distNormal = new Normal(Mean, StdDev);
        }

        internal void Generate()
        {
            var noise = new Perlin() { Frequency = 1.5, Persistence = 5, Octaves = 3 };

            var maxHits = 0;
            var hits = new ushort[_width, _height];
            var R = new int[_width, _height];
            var G = new int[_width, _height];
            var B = new int[_width, _height];

            // Amass our data into proper buffers

            // Generate a new random point each time through this loop
            for (var ipt = 0; ipt < CPoints; ipt++)
            {
                // Calculate the point and it's color
                var (pt, clr) = CalcNebulaPoint(noise);

                // Round off to integers
                var xPix = (int)(pt.X + 0.5);
                var yPix = (int)(pt.Y + 0.5);

                if (xPix >= 0 && xPix < _width && yPix >= 0 && yPix < _height)
                {
                    // Increment our color buffers appropriately
                    R[xPix, yPix] += clr.R;
                    G[xPix, yPix] += clr.G;
                    B[xPix, yPix] += clr.B;

                    // increment the number of hits at this point
                    var hitsCur = ++hits[xPix, yPix];
                    if (hitsCur > maxHits)
                    {
                        maxHits = hitsCur;
                    }
                }
            }

            // Use the data to actually draw stuff

            // Do conversion to double once here.
            double maxHitsDbl = maxHits;

            // Step through all pixels in the image
            for (var iX = 0; iX < _width; iX++)
            {
                for (var iY = 0; iY < _height; iY++)
                {
                    var hitCount = hits[iX, iY];
                    if (hitCount == 0)
                    {
                        continue;
                    }

                    // Gamma correction
                    var noiseVal = Math.Pow(hitCount / maxHitsDbl, 1.0 / 5.0);

                    // Determine gamma corrected average color at this point
                    var r = (byte)(R[iX, iY] * noiseVal / hitCount);
                    var g = (byte)(G[iX, iY] * noiseVal / hitCount);
                    var b = (byte)(B[iX, iY] * noiseVal / hitCount);
                    var color = Color.FromRgb(r, g, b);

                    // Draw it
                    _wbmp.SetPixel(iX, iY, color);
                }
            }
        }

        private (Point, Color) CalcNebulaPoint(Perlin noise)
        {
            // Pick a random normally distributed point around (0.5, 0.5)
            var xNorm = (float)_distNormal.Sample();
            var yNorm = (float)_distNormal.Sample();

            // Pixel coordinates
            var x = xNorm * _width;
            var y = yNorm * _height;

            // Randomly distributed around (0, 0)
            var xc = xNorm - 0.5;
            var yc = yNorm - 0.5;

            // Distance from the center
            var dist = Math.Sqrt(xc * xc + yc * yc);

            // Normalize so 1 at the corners of (-0.5, -0.5) - (0.5, 0.5)
            var tColor = dist / SqrtTwo;

            var nx = NoiseScale * (noise.Value(xNorm, yNorm, 0.75) - 0.5);
            var ny = NoiseScale * (noise.Value(xNorm, yNorm, 0.25) - 0.5);

            return (new Point(x + nx, y + ny), NebulaColor(BandCount, tColor));
        }

        private Color NebulaColor(int cBands, double t, bool fHardEdge = false)
        {
            var band = cBands * t;
            var iBand = (int)Math.Floor(band);
            if (fHardEdge)
            {
                return (iBand & 1) == 0 ? _clrInner : _clrOuter;
            }
            var tBand = band - iBand;
            var color1 = _clrInner;
            var color2 = _clrOuter;

            if ((iBand & 1) == 0)
            {
                (color1, color2) = (color2, color1);
            }

            return LerpColor(color1, color2, tBand);
        }

    }
}
