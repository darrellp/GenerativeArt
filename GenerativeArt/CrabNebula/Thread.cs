using MathNet.Numerics.Distributions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static GenerativeArt.Utilities;

namespace GenerativeArt.CrabNebula
{
    internal class Thread
    {
        private const double SqrtTwo = 1.41421356237;
        private static double _noiseScale;
        private const double StdDev = 0.15;
        private const double Mean = 0.5;

        private static int _cBands;
        private static double _frequency;
        private static double _persistence;
        private static int _octaves;
        private static int _cPoints;

        private readonly int _cPointsThread;
        private readonly int _width;
        private readonly int _height;
        private readonly Perlin _noise;

        private readonly Normal _distNormal;
        private readonly Color _clrInner = Colors.Red;
        private readonly Color _clrOuter = Colors.Yellow;

        private int _maxHits;
        private readonly ushort[,] _hits;
        // ReSharper disable InconsistentNaming
        private readonly int[,] _r;
        private readonly int[,] _g;
        private readonly int[,] _b;
        // ReSharper restore InconsistentNaming

        internal static void SetParameters(Parameters parameters)
        {
            _octaves = parameters.Octaves;
            _persistence = parameters.Persistence;
            _noiseScale = parameters.NoiseScale;
            _frequency = parameters.Frequency;
            _cPoints = parameters.CPoints;
            _cBands = parameters.CBands;
        }

        internal static async Task<(int maxhits, ushort[,] hits, int[,] r, int[,] g, int[,] b)> 
            AmassAcrossThreads(int width, int height)
        {
            Perlin noise = new()
            {
                Frequency = _frequency,
                Persistence = _persistence,
                Octaves = _octaves,
            };

            var cCore = Environment.ProcessorCount;
            var threads = Enumerable.
                Range(0, cCore).
                Select(_ => new Thread(_cPoints / cCore, width, height, noise)).
                ToArray();
            var tasks = threads.Select(t => new Task(t.Amass)).ToList();
            tasks.ForEach(t => t.Start());
            await Task.WhenAll(tasks);
            return ConsolidateThreads(threads);
        }

        static (int maxhits, ushort[,] hits, int[,] r, int[,] g, int[,] b) ConsolidateThreads(Thread[] threads)
        {
            var width = threads[0]._width;
            var height = threads[0]._height;
            var hits = new ushort[width, height];
            var r = new int[width, height];
            var g = new int[width, height];
            var b = new int[width, height];
            var maxHits = 0;

            for (var ix = 0; ix < width; ix++)
            {
                for (var iy = 0; iy < height; iy++)
                {
                    var hitsHere = (ushort)threads.Sum(t => t._hits[ix, iy]);
                    hits[ix, iy] = hitsHere;
                    if (hitsHere > maxHits)
                    {
                        maxHits = hitsHere;
                    }
                    r[ix, iy] = threads.Sum(t => t._r[ix, iy]);
                    g[ix, iy] = threads.Sum(t => t._g[ix, iy]);
                    b[ix, iy] = threads.Sum(t => t._b[ix, iy]);
                }
            }

            return (maxHits, hits, r, g, b);
        }
        
        private Thread(int cPointsThread, int width, int height, Perlin noise)
        {
            _cPointsThread = cPointsThread;
            _width = width;
            _height = height;
            _noise = noise;
            _distNormal = new Normal(Mean, StdDev);
            _hits = new ushort[_width, _height];
            _r = new int[_width, _height];
            _g = new int[_width, _height];
            _b = new int[_width, _height];
        }

        internal void Amass()
        {
            // Generate a new random point each time through this loop
            for (var ipt = 0; ipt < _cPointsThread; ipt++)
            {
                // Calculate the point and it's color
                var (pt, clr) = CalcNebulaPoint(_noise);

                // Round off to integers
                var xPix = (int)(pt.X + 0.5);
                var yPix = (int)(pt.Y + 0.5);

                if (xPix >= 0 && xPix < _width && yPix >= 0 && yPix < _height)
                {
                    // Increment our color buffers appropriately
                    _r[xPix, yPix] += clr.R;
                    _g[xPix, yPix] += clr.G;
                    _b[xPix, yPix] += clr.B;

                    // increment the number of hits at this point
                    var hitsCur = ++_hits[xPix, yPix];
                    if (hitsCur > _maxHits)
                    {
                        _maxHits = hitsCur;
                    }
                }
            }
        }

        internal (Point, Color) CalcNebulaPoint(Perlin noise)
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

            var nx = _noiseScale * (noise.Value(xNorm, yNorm, 0.75) - 0.5);
            var ny = _noiseScale * (noise.Value(xNorm, yNorm, 0.25) - 0.5);

            return (new Point(x + nx, y + ny), NebulaColor(_cBands, tColor));
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
