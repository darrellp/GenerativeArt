﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenerativeArt.CrabNebula
{
    // Todo: Antialiasing
    // Todo: Other types of noise
    // Todo: Killing running threads on new Generate
    // Todo: General Optimization
    // Todo: Other color schemes?
    // Todo: Other gradient types - Horizontal, Ray, etc.
    // Todo: Alpha
    // Todo: Standard Deviation control

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Current code is based on blog post here:
    ///      https://generateme.wordpress.com/2018/10/24/smooth-rendering-log-density-mapping/
    /// with a few differences - primarily that it's written in C# on a WriteableBitmap rather than
    /// in Processing. I've changed some of the parameters to suit me and though I tried some public
    /// domain noise producers, none seemed like what I wanted or I couldn't figure them out so wrote
    /// my own Perlin noise generator.  Colors are done in radial bands rather than whatever he does
    /// (which I think was a horizontal single gradation.  Probably other stuff that I didn't really
    /// understand in his code and just wrote the way that seemed right to me.
    /// </summary>
    ///
    /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class CrabNebula : IGenerator
    {
        #region Private variables
        private int _artWidth;
        private int _artHeight;
#pragma warning disable CS8618
        private MainWindow _ourWindow;
#pragma warning restore CS8618

        private Task<(int, ushort[,], int[,], int[,], int[,])>? _taskAmass;
        private Task<PixelColor[]>? _taskDraw;

        private Parameters _parameters = new();
        #endregion

        #region Initialization
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes this object. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ///
        /// <param name="ourWindow">    our window. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Initialize(MainWindow ourWindow)
        {
            // Initialize our parameters
            _parameters = new Parameters();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_ourWindow == null)
            {
                _ourWindow = ourWindow;
                
                // Grab stuff we want to react to on our tab page
                HookParameterControls();
            }
            _artWidth = _ourWindow.ArtWidth;
            _artHeight = _ourWindow.ArtHeight;

            // Place the initialized values into the controls on our tab page
            // We're required to place this down here because the call relies on
            // stuff we've set up earlier in Initialize().
            DistributeParameters();
        }
        #endregion

        #region Generating/Drawing
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Generates the Crab Nebula art on the Art image. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public async void Generate()
        {
            // Set Parameters correctly
            GatherParameters();
            Thread.SetParameters(_parameters);
            var wbmp = BitmapFactory.New(_artWidth, _artHeight);
            wbmp.Clear(Colors.Black);
            _ourWindow.Art.Source = wbmp;
            Debug.Assert(wbmp.Format == PixelFormats.Pbgra32);

            // Amass our data...
            _taskAmass = Thread.AmassAcrossThreads(_artWidth, _artHeight);
            var (maxHits, hits, R, G, B) = await _taskAmass;
            _taskAmass = null;

            // Use the data to create a pixel array
            _taskDraw = new Task<PixelColor[]>(()=>Draw( maxHits, hits, R, G, B));
            _taskDraw.Start();
            var pixels = await _taskDraw;
            _taskDraw = null;

            // Write the pixel array to the art image
            var sizePixel = Marshal.SizeOf(typeof(PixelColor));
            var stride = _artWidth * sizePixel;
            wbmp.WritePixels(new Int32Rect(0, 0, _artWidth, _artHeight), pixels, stride, 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PixelColor
        {
            public readonly byte Blue;
            public readonly byte Green;
            public readonly byte Red;
            public readonly byte Alpha;

            public PixelColor(byte R, byte G, byte B)
            {
                Red = R;
                Green = G;
                Blue = B;
                Alpha = 255;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Draws the crab nebula given hit info. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. 
        ///             This routine does no I/O and so can be run asynchronously.  it
        ///             puts all it's color into in an array of PixelColors which can be put
        ///             into the WriteableBitmap by the caller in the I/O thread very quickly.
        ///             </remarks>
        ///
        /// <param name="maxHits">  The maximum hits across the image. </param>
        /// <param name="hits">     The hits at each pixel. </param>
        /// <param name="R">        Sum of Red color hits. </param>
        /// <param name="G">        Sum of Green color hits. </param>
        /// <param name="B">        Sum of Blue color hits. </param>
        ///
        /// <returns>   A PixelColor[] with all the final color info. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private PixelColor[] Draw( int maxHits, ushort[,] hits, int[,] R, int[,] G, int[,] B)
        {
            var pixelData = new PixelColor[_artWidth * _artHeight];

            // Do conversion to double once here.
            double maxHitsDbl = maxHits;

            // Step through all pixels in the image
            for (var iX = 0; iX < _artWidth; iX++)
            {
                for (var iY = 0; iY < _artHeight; iY++)
                {
                    var hitCount = hits[iX, iY];
                    if (hitCount == 0)
                    {
                        pixelData[iY * _artWidth + iX] = new PixelColor(0, 0, 0);
                    }

                    // Gamma correction
                    var noiseVal = Math.Pow(hitCount / maxHitsDbl, 1.0 / 5.0);
                    var mult = noiseVal / hitCount;

                    // Draw it
                    pixelData[iY * _artWidth + iX] = new PixelColor(
                        (byte)(R[iX, iY] * mult),
                        (byte)(G[iX, iY] * mult),
                        (byte)(B[iX, iY] * mult));
                }
            }
            return pixelData;
        }
        #endregion

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
