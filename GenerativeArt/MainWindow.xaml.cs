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

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            wbmp.Clear(Colors.Black);
            var noise = new Perlin() { Frequency = 2.5, Persistence = 5, Octaves = 2 };

            //for (int iRow = 0; iRow < height; iRow++)
            //{
            //    for (int iCol = 0; iCol < width; iCol++)
            //    {
            //        var noiseVal = noise.Value(iRow / (double)height, iCol / (double)width);
            //        var colorByte = (byte)Math.Min(255, noiseVal * 255);
            //        var color = Color.FromRgb(colorByte, colorByte, colorByte);
            //        wbmp.SetPixel(iCol, iRow, color);
            //    }
            //}
            var maxHits = 0;
            var hits = new int[width, height];
            var R = new int[width, height];
            var G = new int[width, height];
            var B = new int[width, height];

            for (var ipt = 0; ipt < cPoints; ipt++)
            {
                var (pt, clr) = CalcNebulaPoint(noise);
                var xPix = (int)(pt.X + 0.5);
                var yPix = (int)(pt.Y + 0.5);

                if (xPix >= 0 && xPix < width && yPix >= 0 && yPix < height)
                {
                    R[xPix, yPix] += clr.R;
                    G[xPix, yPix] += clr.G;
                    B[xPix, yPix] += clr.B;

                    var hitsCur = ++hits[xPix, yPix];
                    if (hitsCur > maxHits)
                    {
                        maxHits = hitsCur;
                    }
                }
            }

            double maxHitsDbl = maxHits;

            for (var iX = 0; iX < width; iX++)
            {
                for (var iY = 0; iY < height; iY++)
                {
                    var hitCount = hits[iX, iY];
                    if (hitCount == 0)
                    {
                        continue;
                    }
                    var noiseVal = Math.Pow(hitCount / maxHitsDbl, 1.0 / 5.0);
                    if (noiseVal == 0.0)
                    {
                        continue;
                    }
                    //var colorByte = (byte)Math.Min(255, noiseVal * 255);
                    //var color = Color.FromRgb(colorByte, colorByte, colorByte);
                    var r = (byte)(R[iX, iY] * noiseVal / hitCount);
                    var g = (byte)(G[iX, iY] * noiseVal / hitCount);
                    var b = (byte)(B[iX, iY] * noiseVal / hitCount);
                    var color = Color.FromRgb(r, g, b);
                    wbmp.SetPixel(iX, iY, color);
                }
            }
        }
        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            width = (int)RctSize.ActualWidth;
            height = (int)RctSize.ActualHeight;
            wbmp = BitmapFactory.New(width, height);
            Art.Source = wbmp;
            OnGenerate(this, new RoutedEventArgs());
        }

        private readonly Color _clrInner = Colors.Yellow;
        private readonly Color _clrOuter = Colors.Red;
        private const double SqrtTwo = 1.41421356237;

        private (Point, Color) CalcNebulaPoint(Perlin noise)
        {
            var xNorm = (float)_distNormal.Sample(); // take random point from gaussian distribution
            var yNorm = (float)_distNormal.Sample();
            var x = xNorm * width; // take random point from gaussian distribution
            var y = yNorm * height;
            var xc = xNorm - 0.5;
            var yc = yNorm - 0.5;
            var dist = Math.Pow(xc * xc + yc * yc, 1/6.0);
            var tColor = dist / SqrtTwo;
            var r = NoiseScale * Math.Sqrt(xNorm * xNorm + yNorm * yNorm); // calculate noise scaling factor from distance

            // Pick two uncorrelated points using the Z axis
            var nx = r * (noise.Value(xNorm, yNorm, 0.75) - 0.5);
            var ny = r * (noise.Value(xNorm, yNorm, 0.25) - 0.5);

            return (new Point(x + nx, y + ny), NebulaColor(5, tColor));
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
