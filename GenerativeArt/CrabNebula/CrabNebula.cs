using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenerativeArt.CrabNebula
{
    // Current code is based on blog post here:
    //      https://generateme.wordpress.com/2018/10/24/smooth-rendering-log-density-mapping/
    // with a few differences - primarily that it's written in C# on a WriteableBitmap rather than in Processing.
    // I've changed some of the parameters to suit me and though I tried some public domain noise producers, none
    // seemed like what I wanted or I couldn't figure them out so wrote my own Perlin noise generator.  Colors are
    // done in radial bands rather than whatever he does (which I think was a horizontal single gradation.  Probably
    // other stuff that I didn't really understand in his code and just wrote the way that seemed right to me.

    internal class CrabNebula : IGenerator
    {
        private readonly WriteableBitmap _wbmp;
        private readonly int _width;
        private readonly int _height;


        internal CrabNebula(WriteableBitmap wbmp)
        {
            _wbmp = wbmp;
            _width = (int)_wbmp.Width;
            _height = (int)_wbmp.Height;
        }

        public async void Generate()
        {
            // Amass our data...
            Task<(int, ushort[,], int[,], int[,], int[,])> task = Thread.CreateThreads(_width, _height);
            var (maxHits, hits, R, G, B) = await task;

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
    }
}
