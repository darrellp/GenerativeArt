using GenerativeArt.Noises;
using MathNet.Numerics.Distributions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static GenerativeArt.Utilities;

namespace GenerativeArt.CrabNebula
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   This is the guts of the crab nebula art.  It figures out the detailed
    ///             hit info by using Perlin noise.  It's designed to be run on a separate
    ///             thread and only determines hit and color info without actually doing 
    ///             any I/O. 
    ///             </summary>
    ///
    /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class Thread
    {
        #region Private Variables
        private const double SqrtTwo = 1.41421356237;
        private static double _noiseScale;
        private const double StdDev = 0.15;
        private const double Mean = 0.5;

        private static int _cBands;
        private static double _frequency;
        private static double _persistence;
        private static int _octaves;
        private static int _cPoints;
        private static Color _blend1;
        private static Color _blend2;
        private static bool _fHardEdged;

        private readonly int _cPointsThread;
        private readonly int _width;
        private readonly int _height;
        private readonly Perlin _noise;

        private readonly Normal _distNormal;

        private int _maxHits;
        private readonly ushort[,] _hits;
        // ReSharper disable InconsistentNaming
        private readonly int[,] _r;
        private readonly int[,] _g;
        private readonly int[,] _b;
        // ReSharper restore InconsistentNaming
        #endregion

        #region Parameter Setting
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sets the parameters on our tab page from the Parameters object. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="parameters">   Parameters for our tab page. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal static void SetParameters(Parameters parameters)
        {
            _octaves = parameters.Octaves;
            _persistence = parameters.Persistence;
            _noiseScale = parameters.NoiseScale;
            _frequency = parameters.Frequency;
            _cPoints = parameters.CPoints;
            _cBands = parameters.CBands;
            _blend1 = parameters.Blend1;
            _blend2 = parameters.Blend2;
            _fHardEdged = parameters.FHardEdged;
        }
        #endregion

        #region Constructor
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
        #endregion

        #region Threading
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sets up a number of threads to do the hit/color calculations.  Each
        ///             thread has it's own buffers for all this info which will be combined
        ///             into one set at the end in ConsolidateThreads(). </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="width">    The width. </param>
        /// <param name="height">   The height. </param>
        ///
        /// <returns>   All the hits and color information </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal static async Task<(int maxhits, ushort[,] hits, int[,] r, int[,] g, int[,] b)> 
            AmassAcrossThreads(int width, int height, CancellationTokenSource cts)
        {
            Perlin noise = new()
            {
                Frequency = _frequency,
                Persistence = _persistence,
                Octaves = _octaves,
            };

            var cThreads = Environment.ProcessorCount;
            var threads = Enumerable.
                Range(0, cThreads).
                Select(_ => new Thread(_cPoints / cThreads, width, height, noise)).
                ToArray();
            var tasks = threads.Select(t => new Task(() =>t.Amass(cts.Token))).ToList();
            tasks.ForEach(t => t.Start());
            await Task.WhenAll(tasks);
            return ConsolidateThreads(threads);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Consolidate the hit/color information calculated by the individual threads. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="threads">  The threads. </param>
        ///
        /// <returns>   All the hits and color information. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static (int maxhits, ushort[,] hits, int[,] r, int[,] g, int[,] b) 
            ConsolidateThreads(Thread[] threads)
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
        #endregion

        #region Calculating hit/color information
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Amass all the color/hit information for this thread. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal void Amass(CancellationToken token)
        {
            // Generate a new random point each time through this loop
            for (var ipt = 0; ipt < _cPointsThread; ipt++)
            {
#if KILLABLE
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }
#endif
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Calculates a nebula point and its color. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="noise">    The noise. </param>
        ///
        /// <returns>   The calculated nebula point. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Determines a Nebula color. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="cBands">   The count of bands. </param>
        /// <param name="t">        The 0-1 value with 0 at the center and 1 at the edges. </param>
        ///
        /// <returns>   A Color. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static Color NebulaColor(int cBands, double t)
        {
            var band = cBands * t;
            var iBand = (int)Math.Floor(band);
            if (_fHardEdged)
            {
                return (iBand & 1) == 0 ? _blend1 : _blend2;
            }
            var tBand = band - iBand;
            var color1 = _blend1;
            var color2 = _blend2;

            if ((iBand & 1) == 0)
            {
                (color1, color2) = (color2, color1);
            }

            return LerpColor(color1, color2, tBand);
        }
#endregion
    }
}
