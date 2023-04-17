using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorPicker.Models;

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
        private MainWindow _ourWindow;

        private Parameters _parameters = new();

#pragma warning disable CS8618
        internal CrabNebula(WriteableBitmap wbmp)
#pragma warning restore CS8618
        {
            _wbmp = wbmp;
            _width = (int)_wbmp.Width;
            _height = (int)_wbmp.Height;
        }

        public void Initialize(MainWindow ourWindow)
        {
            _parameters = new Parameters();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_ourWindow == null)
            {
                _ourWindow = ourWindow;
                HookParameterControls();
            }
            DistributeParameters();
        }

        public async void Generate()
        {
            // Set Parameters correctly
            GatherParameters();
            Thread.SetParameters(_parameters);

            // Amass our data...
            Task<(int, ushort[,], int[,], int[,], int[,])> task = Thread.AmassAcrossThreads(_width, _height);
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

        #region Parameter Handling
        private void GatherParameters()
        {
            _parameters.Octaves = (int)_ourWindow.sldrOctaves.Value;
            _parameters.Persistence = _ourWindow.sldrPersistence.Value;
            _parameters.NoiseScale = _ourWindow.sldrNoiseScale.Value;
            _parameters.Frequency = _ourWindow.sldrFrequency.Value;
            _parameters.CPoints = (int)_ourWindow.sldrCPoints.Value;
            _parameters.CBands = (int)_ourWindow.sldrCBands.Value;
            var btn = _ourWindow.btnBlend1;
            var brush = (SolidColorBrush)btn.Background;
            _parameters.Blend1 = brush.Color;
            btn = _ourWindow.btnBlend2;
            brush = (SolidColorBrush)btn.Background;
            _parameters.Blend2 = brush.Color;
            _parameters.FHardEdged = _ourWindow.cbxHardEdges.IsChecked ?? false;
        }

        private void DistributeParameters()
        {
            _ourWindow.sldrOctaves.Value = _parameters.Octaves;
            _ourWindow.sldrPersistence.Value = _parameters.Persistence;
            _ourWindow.sldrNoiseScale.Value = _parameters.NoiseScale;
            _ourWindow.sldrFrequency.Value = _parameters.Frequency;
            _ourWindow.sldrCPoints.Value = _parameters.CPoints;
            _ourWindow.sldrCBands.Value = _parameters.CBands;
            _ourWindow.btnBlend1.Background = new SolidColorBrush(_parameters.Blend1);
            _ourWindow.btnBlend2.Background = new SolidColorBrush(_parameters.Blend2);
            _ourWindow.cbxHardEdges.IsChecked = _parameters.FHardEdged;
        }

        #region Hooks
        private void HookParameterControls()
        {
            _ourWindow.sldrOctaves.ValueChanged += SldrOctaves_ValueChanged;
            _ourWindow.sldrPersistence.ValueChanged +=SldrPersistence_ValueChanged;
            _ourWindow.sldrNoiseScale.ValueChanged +=SldrNoiseScale_ValueChanged;
            _ourWindow.sldrFrequency.ValueChanged +=SldrFrequency_ValueChanged;
            _ourWindow.sldrCPoints.ValueChanged +=SldrCPoints_ValueChanged;
            _ourWindow.sldrCBands.ValueChanged +=SldrCBands_ValueChanged;
            _ourWindow.btnBlend1.Click += BtnBlend1_Click;
            _ourWindow.btnBlend2.Click += BtnBlend2_Click;
        }

        private void BtnBlend1_Click(object sender, RoutedEventArgs e)
        {
            var btn = _ourWindow.btnBlend1;
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, color) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(color);
            }
        }

        private void BtnBlend2_Click(object sender, RoutedEventArgs e)
        {
            var btn = _ourWindow.btnBlend2;
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, color) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(color);
            }
        }

        private void SldrCBands_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblCBands.Content = $"Color Bands: {e.NewValue:0}";
        }

        private void SldrCPoints_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblCPoints.Content = $"# Pts: {e.NewValue/1_000_000:#}M";
        }

        private void SldrFrequency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFrequency.Content = $"Frequency: {e.NewValue:0.00}";
        }

        private void SldrNoiseScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblNoiseScale.Content = $"Noise Scale: {e.NewValue:###0}";
        }

        private void SldrPersistence_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblPersistence.Content = $"Persistence: {e.NewValue:#0.0}";
        }

        private void SldrOctaves_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblOctaves.Content = $"Octave: {(int)e.NewValue}";
        }
        #endregion
        #endregion
    }
}
