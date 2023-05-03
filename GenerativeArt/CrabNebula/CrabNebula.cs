
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
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
    // Todo: Data Binding
    
    // Actually, I may never do data binding.  It works okay right now so I may just use data binding moving
    // forward but leave crab nebula the way it is.

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

        /// <summary>   The main window. </summary>
        private readonly MainWindow _ourWindow;

        internal int ArtHeight => _ourWindow.ArtHeight;
        internal int ArtWidth => _ourWindow.ArtWidth;

        private int _cPoints;
        public int CPoints
        {
            get => _cPoints;
            set => SetField(ref _cPoints, value);
        }

        private int _cBands;
        public int CBands
        {
            get => _cBands;
            set => SetField(ref _cBands, value);
        }

        private int _octaves;
        public int Octaves
        {
            get => _octaves;
            set => SetField(ref _octaves, value);
        }

        private double _noiseScale;
        public double NoiseScale
        {
            get => _noiseScale;
            set => SetField(ref _noiseScale, value);
        }

        private double _frequency;
        public double Frequency
        {
            get => _frequency;
            set => SetField(ref _frequency, value);
        }

        private double _persistence;
        public double Persistence
        {
            get => _persistence;
            set => SetField(ref _persistence, value);
        }

        private double _stdDev;
        public double StdDev
        {
            get => _stdDev;
            set => SetField(ref _stdDev, value);
        }

        private Color _blend1;
        public Color Blend1
        {
            get => _blend1;
            set => SetField(ref _blend1, value);
        }

        private Color _blend2;
        public Color Blend2
        {
            get => _blend2;
            set => SetField(ref _blend2, value);
        }

        private bool _fHardEdged;
        public bool FHardEdged
        {
            get => _fHardEdged;
            set => SetField(ref _fHardEdged, value);
        }

        /// <summary>   Amassing tasks. </summary>
        private Task<(int, ushort[,], int[,], int[,], int[,])>? _taskAmass;

        /// <summary>   The draw task. </summary>
        private Task<PixelColor[]>? _taskDraw;

        /// <summary>   The Cancellation Token Source. </summary>
        private CancellationTokenSource? _cts;
        #endregion

        #region Constructor
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="ourWindow">    our window. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal CrabNebula(MainWindow ourWindow)
        {
            _ourWindow = ourWindow;
            HookParameterControls();
        }
        #endregion

        #region Initialization / Destruction
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes this object. Resets all the tab page controls to defaults. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Initialize()
        {
            CPoints = 6_000_000;
            NoiseScale = 800.0;
            StdDev = 0.15;
            CBands = 8;
            Frequency = 1.5;
            Persistence = 5;
            Octaves = 3;
            Blend1 = Colors.Yellow;
            Blend2 = Colors.Red;
            FHardEdged = false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Kills any running threads. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Kill()
        {
            _cts?.Cancel();
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
#if KILLABLE
            // Check to see if we need to kill running threads
            if (_taskAmass != null)
            {
                Debug.Assert(_cts != null, nameof(_cts) + " != null");
                _cts.Cancel();
            }
            else if (_taskDraw != null)
            {
                Debug.Assert(_cts != null, nameof(_cts) + " != null");
                _cts.Cancel();
            }
            _cts = null;
#endif
            var wbmp = BitmapFactory.New(ArtWidth, ArtHeight);
            wbmp.Clear(Colors.Black);
            _ourWindow.Art.Source = wbmp;
            Debug.Assert(wbmp.Format == PixelFormats.Pbgra32);

            // Amass our data...
            _cts = new();
            _taskAmass = Thread.AmassAcrossThreads(this, ArtWidth, ArtHeight, _cts);
            int maxHits;
            ushort[,] hits;
            int[,] R, G, B;
            (maxHits, hits, R, G, B) = await _taskAmass;
            _taskAmass = null;
            _cts = null;

            // Use the data to create a pixel array
            _cts = new();
            _taskDraw = new(() => Draw(maxHits, hits, R, G, B, _cts.Token));
            _taskDraw.Start();
            PixelColor[] pixels;
            pixels = await _taskDraw;
            _taskDraw = null;
            _cts = null;

            // Write the pixel array to the art image
            var sizePixel = Marshal.SizeOf(typeof(PixelColor));
            var stride = ArtWidth * sizePixel;

            // The only I/O done in the generation process
            wbmp.WritePixels(new(0, 0, ArtWidth, ArtHeight), pixels, stride, 0);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Draws the crab nebula given hit info. </summary>
        ///
        /// <remarks>
        /// Darrell Plank, 4/19/2023. This routine does no I/O and so can be run asynchronously.  it puts
        /// all it's color into in an array of PixelColors which can be put into the WriteableBitmap by
        /// the caller in the I/O thread very quickly.
        /// </remarks>
        ///
        /// <param name="maxHits">  The maximum hits across the image. </param>
        /// <param name="hits">     The hits at each pixel. </param>
        /// <param name="R">        Sum of Red color hits. </param>
        /// <param name="G">        Sum of Green color hits. </param>
        /// <param name="B">        Sum of Blue color hits. </param>
        /// <param name="token">    Token for cancellation. </param>
        ///
        /// <returns>   A PixelColor[] with all the final color info. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private PixelColor[] Draw( int maxHits, ushort[,] hits, int[,] R, int[,] G, int[,] B, CancellationToken token)
        {
            var pixelData = new PixelColor[ArtWidth * ArtHeight];

            // Do conversion to double once here.
            double maxHitsDbl = maxHits;

            // Step through all pixels in the image
            for (var iX = 0; iX < ArtWidth; iX++)
            {
                for (var iY = 0; iY < ArtHeight; iY++)
                {
#if KILLABLE
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }
#endif
                    var hitCount = hits[iX, iY];
                    if (hitCount == 0)
                    {
                        pixelData[iY * ArtWidth + iX] = new(0, 0, 0);
                    }

                    // Gamma correction
                    var noiseVal = Math.Pow(hitCount / maxHitsDbl, 1.0 / 5.0);
                    var mult = noiseVal / hitCount;

                    // Draw it
                    pixelData[iY * ArtWidth + iX] = new(
                        (byte)(R[iX, iY] * mult),
                        (byte)(G[iX, iY] * mult),
                        (byte)(B[iX, iY] * mult));
                }
            }
            return pixelData;
        }
        #endregion

        #region Hooks
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Hook parameter controls. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void HookParameterControls()
        {
            _ourWindow.sldrCnOctaves.ValueChanged += SldrOctaves_ValueChanged;
            _ourWindow.sldrCnPersistence.ValueChanged +=SldrCnPersistenceValueChanged;
            _ourWindow.sldrNoiseScale.ValueChanged +=SldrNoiseScale_ValueChanged;
            _ourWindow.sldrCnFrequency.ValueChanged +=SldrCnFrequencyValueChanged;
            _ourWindow.sldrCnCPoints.ValueChanged +=SldrCPoints_ValueChanged;
            _ourWindow.sldrCBands.ValueChanged +=SldrCBands_ValueChanged;
            _ourWindow.btnBlend1.Click += BtnBlend1_Click;
            _ourWindow.btnBlend2.Click += BtnBlend2_Click;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by BtnBlend1 for click events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Routed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by BtnBlend2 for click events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Routed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by SldrCBands for value changed events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrCBands_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblCBands.Content = $"Color Bands: {e.NewValue:0}";
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by SldrCPoints for value changed events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrCPoints_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblCnCPoints.Content = $"# Pts: {e.NewValue/1_000_000:#}M";
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sldr cn frequency value changed. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrCnFrequencyValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblCnFrequency.Content = $"Frequency: {e.NewValue:0.00}";
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by SldrNoiseScale for value changed events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrNoiseScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblNoiseScale.Content = $"Noise Scale: {e.NewValue:###0}";
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sldr cn persistence value changed. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrCnPersistenceValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblCnPersistence.Content = $"Persistence: {e.NewValue:#0.0}";
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by SldrOctaves for value changed events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrOctaves_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblCnOctaves.Content = $"Octave: {(int)e.NewValue}";
        }
        #endregion

        #region Property changes
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

    }
}
