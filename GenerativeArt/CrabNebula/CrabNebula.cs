
using System;
using System.Diagnostics;
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
        /// <summary>   Width of the art. </summary>
        private int _artWidth;

        /// <summary>   Height of the art. </summary>
        private int _artHeight;

        /// <summary>   The main window. </summary>
        private readonly MainWindow _ourWindow;

        /// <summary>   Amassing tasks. </summary>
        private Task<(int, ushort[,], int[,], int[,], int[,])>? _taskAmass;

        /// <summary>   The draw task. </summary>
        private Task<PixelColor[]>? _taskDraw;

        /// <summary>   Object which keeps the parameters from our tab page. </summary>
        private Parameters _parameters = new();

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
            // Initialize our parameters
            _parameters = new();
            _artWidth = _ourWindow.ArtWidth;
            _artHeight = _ourWindow.ArtHeight;

            // Place the initialized values into the controls on our tab page
            // We're required to place this down here because the call relies on
            // stuff we've set up earlier in Initialize().
            DistributeParameters();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Kills any running threads. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/19/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Kill()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
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
            // Set Parameters correctly
            GatherParameters();
            Thread.SetParameters(_parameters);
            var wbmp = BitmapFactory.New(_artWidth, _artHeight);
            wbmp.Clear(Colors.Black);
            _ourWindow.Art.Source = wbmp;
            Debug.Assert(wbmp.Format == PixelFormats.Pbgra32);

            // Amass our data...
            _cts = new();
            _taskAmass = Thread.AmassAcrossThreads(_artWidth, _artHeight, _cts);
            int maxHits;
            ushort[,] hits;
            int[,] R, G, B;
            try
            {
                (maxHits, hits, R, G, B) = await _taskAmass;
            }
            catch (Exception e)
            {
                return;
            }
            _taskAmass = null;
            _cts = null;

            // Use the data to create a pixel array
            _cts = new();
            _taskDraw = new(() => Draw(maxHits, hits, R, G, B, _cts.Token));
            _taskDraw.Start();
            PixelColor[] pixels;
            try
            {
                pixels = await _taskDraw;
            }
            catch (OperationCanceledException e)
            {
                return;
            }
            _taskDraw = null;
            _cts = null;

            // Write the pixel array to the art image
            var sizePixel = Marshal.SizeOf(typeof(PixelColor));
            var stride = _artWidth * sizePixel;

            // The only I/O done in the generation process
            wbmp.WritePixels(new(0, 0, _artWidth, _artHeight), pixels, stride, 0);
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
            var pixelData = new PixelColor[_artWidth * _artHeight];

            // Do conversion to double once here.
            double maxHitsDbl = maxHits;

            // Step through all pixels in the image
            for (var iX = 0; iX < _artWidth; iX++)
            {
                for (var iY = 0; iY < _artHeight; iY++)
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
                        pixelData[iY * _artWidth + iX] = new(0, 0, 0);
                    }

                    // Gamma correction
                    var noiseVal = Math.Pow(hitCount / maxHitsDbl, 1.0 / 5.0);
                    var mult = noiseVal / hitCount;

                    // Draw it
                    pixelData[iY * _artWidth + iX] = new(
                        (byte)(R[iX, iY] * mult),
                        (byte)(G[iX, iY] * mult),
                        (byte)(B[iX, iY] * mult));
                }
            }
            return pixelData;
        }
#endregion

        #region Parameter Handling
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gather parameters from the tab page. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void GatherParameters()
        {
            _parameters.Octaves = (int)_ourWindow.sldrCnOctaves.Value;
            _parameters.Persistence = _ourWindow.sldrCnPersistence.Value;
            _parameters.NoiseScale = _ourWindow.sldrNoiseScale.Value;
            _parameters.Frequency = _ourWindow.sldrCnFrequency.Value;
            _parameters.CPoints = (int)_ourWindow.sldrCnCPoints.Value;
            _parameters.CBands = (int)_ourWindow.sldrCBands.Value;
            var btn = _ourWindow.btnBlend1;
            var brush = (SolidColorBrush)btn.Background;
            _parameters.Blend1 = brush.Color;
            btn = _ourWindow.btnBlend2;
            brush = (SolidColorBrush)btn.Background;
            _parameters.Blend2 = brush.Color;
            _parameters.FHardEdged = _ourWindow.cbxHardEdges.IsChecked ?? false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Distribute parameters from tab page to our local variables. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void DistributeParameters()
        {
            _ourWindow.sldrCnOctaves.Value = _parameters.Octaves;
            _ourWindow.sldrCnPersistence.Value = _parameters.Persistence;
            _ourWindow.sldrNoiseScale.Value = _parameters.NoiseScale;
            _ourWindow.sldrCnFrequency.Value = _parameters.Frequency;
            _ourWindow.sldrCnCPoints.Value = _parameters.CPoints;
            _ourWindow.sldrCBands.Value = _parameters.CBands;
            _ourWindow.btnBlend1.Background = new SolidColorBrush(_parameters.Blend1);
            _ourWindow.btnBlend2.Background = new SolidColorBrush(_parameters.Blend2);
            _ourWindow.cbxHardEdges.IsChecked = _parameters.FHardEdged;
        }

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
        #endregion
    }
}
