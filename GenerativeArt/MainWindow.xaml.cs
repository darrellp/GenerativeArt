using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MathNet.Numerics.Distributions;

namespace GenerativeArt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const double NoiseScale = 1000.0;
        private const double StdDev = 0.15;
        private const double Mean = 0.5;
        private int cPoints = 6000000;
        private readonly Normal _distNormal;
        private int width, height;

        private WriteableBitmap? wbmp;
        public MainWindow()
        {
            InitializeComponent();

            _distNormal = new(Mean, StdDev);
        }

        // Current code is based on blog post here:
        //      https://generateme.wordpress.com/2018/10/24/smooth-rendering-log-density-mapping/
        // with a few differences - primarily that it's written in C# on a WriteableBitmap rather than in Processing.
        // I've changed some of the parameters to suit me and though I tried some public domain noise producers, none
        // seemed like what I wanted or I couldn't figure them out so wrote my own Perlin noise generator.  Colors are
        // done in radial bands rather than whatever he does (which I think was a horizontal single gradation.  Probably
        // other stuff that I didn't really understand in his code and just wrote the way that seemed right to me.
        // Eventually, I will probably do other generative stuff and OnGenerate will generate one of s set of different
        // algorithms here but for now this is the only one.  This stuff should definitely be put into a class of it's
        // own but pressing forward with this until I start on a second algorithm.

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            wbmp.Clear(Colors.Black);
            var noise = new Perlin() { Frequency = 1.5, Persistence = 5, Octaves = 2 };

            var maxHits = 0;
            var hits = new int[width, height];
            var R = new int[width, height];
            var G = new int[width, height];
            var B = new int[width, height];
            
            // Amass our data into proper buffers

            // Generate a new random point each time through this loop
            for (var ipt = 0; ipt < cPoints; ipt++)
            {
                // Calculate the point and it's color
                var (pt, clr) = CalcNebulaPoint(noise);

                // Round off to integers
                var xPix = (int)(pt.X + 0.5);
                var yPix = (int)(pt.Y + 0.5);

                if (xPix >= 0 && xPix < width && yPix >= 0 && yPix < height)
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
            for (var iX = 0; iX < width; iX++)
            {
                for (var iY = 0; iY < height; iY++)
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
                    wbmp.SetPixel(iX, iY, color);
                }
            }
        }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            // There's got to be an easier way than this but I'm using this to determine when the
            // size of the WritableBitmap is actually known.  I'm not sure what the proper way to
            // do this is, but it can't be this!

            width = (int)RctSize.ActualWidth;
            height = (int)RctSize.ActualHeight;
            wbmp = BitmapFactory.New(width, height);
            Art.Source = wbmp;
            OnGenerate(this, new RoutedEventArgs());
        }

        private readonly Color _clrInner = Colors.Red;
        private readonly Color _clrOuter = Colors.Yellow;
        private const double SqrtTwo = 1.41421356237;

        // Determine a random point in the nebula and what color it should be
        private (Point, Color) CalcNebulaPoint(Perlin noise)
        {
            // Pick a random normally distributed point around (0.5, 0.5)
            var xNorm = (float)_distNormal.Sample();
            var yNorm = (float)_distNormal.Sample();

            // Pixel coordinates
            var x = xNorm * width;
            var y = yNorm * height;

            // Randomly distributed around (0, 0)
            var xc = xNorm - 0.5;
            var yc = yNorm - 0.5;

            // Distance from the center
            var dist = Math.Sqrt(xc * xc + yc * yc);

            // Normalize so 1 at the corners of (-0.5, -0.5) - (0.5, 0.5)
            var tColor = dist / SqrtTwo;

            // calculate noise scaling factor from distance
            var r = NoiseScale * 4 * dist; // Math.Sqrt(xNorm * xNorm + yNorm * yNorm); 

            // Pick two uncorrelated points using the Z axis
            var nx = r * (noise.Value(xNorm, yNorm, 0.75) - 0.5);
            var ny = r * (noise.Value(xNorm, yNorm, 0.25) - 0.5);

            return (new Point(x + nx, y + ny), NebulaColor(8, tColor));
            // return (new Point(x + nx, y + ny), LerpColor(_clrInner, _clrOuter, tColor));
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

        private static Color LerpColor(Color color1, Color color2, double t)
        {
            var r = color1.R * (1 - t) + color2.R * t;
            var g = color1.G * (1 - t) + color2.G * t;
            var b = color1.B * (1 - t) + color2.B * t;
            return new Color() { R = (byte)r, G = (byte)g, B = (byte)b };
        }
    }
}
