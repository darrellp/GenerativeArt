using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using GenerativeArt.Noises;
using System.Windows;
using System.Runtime.InteropServices;

namespace GenerativeArt.NoiseGenerator
{
    internal class NoiseGenerator : IGenerator
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        MainWindow _ourWindow;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        int _artWidth;
        int _artHeight;

        #region IGenerator interface
        public void Generate()
        {
            var wbmp = BitmapFactory.New(_artWidth, _artHeight);
            wbmp.Clear(Colors.Black);
            _ourWindow.Art.Source = wbmp;
            Debug.Assert(wbmp.Format == PixelFormats.Pbgra32);
            var pixels = DrawPerlin();
            var sizePixel = Marshal.SizeOf(typeof(PixelColor));
            var stride = _artWidth * sizePixel;
            wbmp.WritePixels(new Int32Rect(0, 0, _artWidth, _artHeight), pixels, stride, 0);
        }

        public void Initialize(MainWindow ourWindow)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_ourWindow == null)
            {
                _ourWindow = ourWindow;
            }
            _artWidth = _ourWindow.ArtWidth;
            _artHeight = _ourWindow.ArtHeight;
        }

        public void Kill()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Drawing
        PixelColor[] DrawPerlin()
        {
            var noise = new Perlin();

            var pixelData = new PixelColor[_artWidth * _artHeight];
            for (var iX = 0; iX < _artWidth; iX++)
            {
                var ixNorm = iX/(double)_artWidth;
                for (var iY = 0; iY < _artHeight; iY++)
                {
                    var iyNorm = iY / (double)_artHeight;
                    var val = (Byte)(noise.Value(ixNorm, iyNorm) * 255);
                    pixelData[iY * _artWidth + iX] = new PixelColor(val, val, val);
                }
            }
            return pixelData;
        }
        #endregion
    }
}
