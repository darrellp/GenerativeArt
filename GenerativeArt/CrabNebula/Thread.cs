
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
    /// <summary>
    /// This is the guts of the crab nebula art.  It figures out the detailed hit info by using
    /// Perlin noise.  It's designed to be run on a separate thread and only determines hit and color
    /// info without actually doing any I/O.
    /// </summary>
    ///
    /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class Thread
    {
        #region Private Variables
        private readonly CrabNebula _nebula;

        /// <summary>   (Immutable) the sqrt two. </summary>
        private const double SqrtTwo = 1.41421356237;

        /// <summary>   The noise scale. </summary>
        private double NoiseScale => _nebula.NoiseScale;

        /// <summary>   (Immutable) the standard development. </summary>
        private double StdDev => _nebula.StdDev;

        /// <summary>   (Immutable) the mean. </summary>
        private const double Mean = 0.5;

        /// <summary>   The bands. </summary>
        private int CBands => _nebula.CBands;

        /// <summary>   The first blend. </summary>
        private Color Blend1 => _nebula.Blend1;

        /// <summary>   The second blend. </summary>
        private Color Blend2 => _nebula.Blend2;

        /// <summary>   True if hard edged. </summary>
        private bool FHardEdged => _nebula.FHardEdged;

        /// <summary>   (Immutable) the points thread. </summary>
        private readonly int _cPointsThread;


        /// <summary>   (Immutable) the width. </summary>
        private int Width => _nebula.ArtWidth;

        /// <summary>   (Immutable) the height. </summary>
        private int Height => _nebula.ArtHeight;

        /// <summary>   (Immutable) the noise. </summary>
        private readonly Perlin _noise;

        /// <summary>   (Immutable) the distance normal. </summary>
        private readonly Normal _distNormal;

        /// <summary>   The maximum hits. </summary>
        private int _maxHits;

        /// <summary>   Hits from the nebula process. </summary>
        private readonly ushort[,] _hits;

        // ReSharper disable InconsistentNaming
        /// <summary>   (Immutable) the r. </summary>
        private readonly int[,] _r;

        /// <summary>   (Immutable) the g. </summary>
        private readonly int[,] _g;

        /// <summary>   (Immutable) the b. </summary>
        private readonly int[,] _b;
        // ReSharper restore InconsistentNaming
        #endregion

        #region Constructor
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="cPointsThread">    Count of points each thread handles. </param>
        /// <param name="width">            The width of the bitmap. </param>
        /// <param name="height">           The height of the bitmap. </param>
        /// <param name="noise">            The Perlin noise generator. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private Thread(CrabNebula nebula, int cPointsThread, int seed, Perlin noise)
        {
            _nebula = nebula;
            _cPointsThread = cPointsThread;
            _noise = noise;
            _distNormal = new Normal(Mean, StdDev);
            _distNormal.RandomSource = new Random(seed);
            _hits = new ushort[Width, Height];
            _r = new int[Width, Height];
            _g = new int[Width, Height];
            _b = new int[Width, Height];
        }
        #endregion

        #region Threading
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Sets up a number of threads to do the hit/color calculations.  Each thread has it's own
        /// buffers for all this info which will be combined into one set at the end in
        /// ConsolidateThreads().
        /// </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="width">    The width of the bitmap. </param>
        /// <param name="height">   The height of the bitmap. </param>
        /// <param name="cts">      Cancellation Token Source. </param>
        ///
        /// <returns>   All the hits and color information. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal static async Task<(int maxhits, ushort[,] hits, int[,] r, int[,] g, int[,] b)> 
            AmassAcrossThreads(CrabNebula nebula, int width, int height, int seed, CancellationTokenSource cts)
        {
            Random rnd = new Random(seed);
            Perlin noise = new(seed + 1)
            {
                Frequency = nebula.Frequency,
                Persistence = nebula.Persistence,
                Octaves = nebula.Octaves,
            };

            var cThreads = Environment.ProcessorCount;
            var threads = Enumerable.
                Range(0, cThreads).
                Select(_ => new Thread(nebula, nebula.CPoints / cThreads, rnd.Next(), noise)).
                ToArray();
            var tasks = threads.Select(t => new Task(() =>t.Amass(cts.Token))).ToList();
            tasks.ForEach(t => t.Start());
            await Task.WhenAll(tasks);
            return ConsolidateThreads(threads);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Consolidate the hit/color information calculated by the individual threads.
        /// </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="threads">  The threads and their calculations. </param>
        ///
        /// <returns>   All the hits and color information. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static (int maxhits, ushort[,] hits, int[,] r, int[,] g, int[,] b) 
            ConsolidateThreads(Thread[] threads)
        {
            var width = threads[0].Width;
            var height = threads[0].Height;
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
        ///
        /// <param name="token">    A token that allows processing to be cancelled. </param>
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
                var (pt, clr) = CalcRandomNebulaPoint(_noise);

                // Round off to integers
                var xPix = (int)(pt.X + 0.5);
                var yPix = (int)(pt.Y + 0.5);

                if (xPix >= 0 && xPix < Width && yPix >= 0 && yPix < Height)
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
        /// <param name="noise">    The Perlin noise generator. </param>
        ///
        /// <returns>   The calculated nebula point. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal (Point, Color) CalcRandomNebulaPoint(Perlin noise)
        {
            // Pick a random normally distributed point around (0.5, 0.5)
            var xNorm = (float)_distNormal.Sample();
            var yNorm = (float)_distNormal.Sample();
            return CalcNebulaPoint(xNorm, yNorm, noise);
        }

        internal (Point, Color) CalcNebulaPoint(double xNorm, double yNorm, Perlin noise)
        {
            // Pixel coordinates
            var x = xNorm * Width;
            var y = yNorm * Height;

            // Randomly distributed around (0, 0)
            var xc = xNorm - 0.5;
            var yc = yNorm - 0.5;

            // Distance from the center
            var dist = Math.Sqrt(xc * xc + yc * yc);

            // Normalize so 1 at the corners of (-0.5, -0.5) - (0.5, 0.5)
            var tColor = dist / SqrtTwo;

            var nx = NoiseScale * (noise.Value(xNorm, yNorm, 0.75) - 0.5);
            var ny = NoiseScale * (noise.Value(xNorm, yNorm, 0.25) - 0.5);

            return (new Point(x + nx, y + ny), NebulaColor(CBands, tColor));
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

        private Color NebulaColor(int cBands, double t)
        {
            var band = cBands * t;
            var iBand = (int)Math.Floor(band);
            if (FHardEdged)
            {
                return (iBand & 1) == 0 ? Blend1 : Blend2;
            }
            var tBand = band - iBand;
            var color1 = Blend1;
            var color2 = Blend2;

            if ((iBand & 1) == 0)
            {
                (color1, color2) = (color2, color1);
            }

            return Lerp(color1, color2, tBand);
        }
#endregion
    }
}
