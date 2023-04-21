
using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using GenerativeArt.Noises;
using System.Windows;
using System.Runtime.InteropServices;

namespace GenerativeArt.NoiseGenerator
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Generates the noise image. </summary>
    ///
    /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class NoiseGenerator : IGenerator
    {
        #region Private Variables
        /// <summary>   Main window. </summary>
        private readonly MainWindow _ourWindow;

        /// <summary>   Width of the art control. </summary>
        private int _artWidth;

        /// <summary>   Height of the art control. </summary>
        private int _artHeight;

        /// <summary>   Values from our tab page. </summary>
        private Parameters _params = new();
        #endregion

        #region Constructor
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="ourWindow">    our window. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal NoiseGenerator(MainWindow ourWindow)
        {
            _ourWindow = ourWindow;
            HookParameterControls();
        }
        #endregion  

        #region IGenerator interface
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Generates noise as the "art". </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Generate()
        {
            GatherParameters();
            var wbmp = BitmapFactory.New(_artWidth, _artHeight);
            wbmp.Clear(Colors.Black);
            _ourWindow.Art.Source = wbmp;
            Debug.Assert(wbmp.Format == PixelFormats.Pbgra32);
            var pixels = DrawPerlin();
            var sizePixel = Marshal.SizeOf(typeof(PixelColor));
            var stride = _artWidth * sizePixel;
            wbmp.WritePixels(new Int32Rect(0, 0, _artWidth, _artHeight), pixels, stride, 0);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes this object.  Resets all the parameters on the tabs page. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Initialize()
        {
            _params = new();
            _artWidth = _ourWindow.ArtWidth;
            _artHeight = _ourWindow.ArtHeight;
            DistributeParameters();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Kills any processes running for this generator. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Kill()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Drawing
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Draw the perlin noise to a pixel buffer.  Does no actual I/O 
        ///             so suitable for threading. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <returns>   Buffer of all our pixesl to be drawn on the image. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private PixelColor[] DrawPerlin()
        {
            var noise = new Perlin()
            {
                Frequency = _params.Frequency,
                Octaves = _params.Octaves,
                Persistence = _params.Persistence,
            };

            var pixelData = new PixelColor[_artWidth * _artHeight];
            for (var iX = 0; iX < _artWidth; iX++)
            {
                var ixNorm = iX/(double)_artWidth;
                for (var iY = 0; iY < _artHeight; iY++)
                {
                    var iyNorm = iY / (double)_artHeight;
                    var val = (Byte)(noise.Value(ixNorm, iyNorm) * 255);
                    pixelData[iY * _artWidth + iX] = new(val, val, val);
                }
            }
            return pixelData;
        }
        #endregion

        #region Parameter handling
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Distribute parameters to the controls on the tabs page. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void DistributeParameters()
        {
            _ourWindow.sldrNsOctaves.Value = _params.Octaves;
            _ourWindow.sldrNsPersistence.Value = _params.Persistence;
            _ourWindow.sldrNsFrequency.Value = _params.Frequency;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gather parameters from the tabs page. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void GatherParameters()
        {
            _params.Octaves = (int)_ourWindow.sldrNsOctaves.Value;
            _params.Persistence = _ourWindow.sldrNsPersistence.Value;
            _params.Frequency = _ourWindow.sldrNsFrequency.Value;

        }

        #region Hooks
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Hook tab page controls. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void HookParameterControls()
        {
            _ourWindow.sldrNsOctaves.ValueChanged += SldrNsOctaves_ValueChanged;
            _ourWindow.sldrNsPersistence.ValueChanged += SldrNsPersistence_ValueChanged;
            _ourWindow.sldrNsFrequency.ValueChanged += SldrNsFrequency_ValueChanged;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by SldrNsFrequency for value changed events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrNsFrequency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblNsFrequency.Content = $"Frequency: {e.NewValue:0.00}";
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by SldrNsPersistence for value changed events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrNsPersistence_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblNsPersistence.Content = $"Persistence: {e.NewValue:#0.0}";
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by SldrNsOctaves for value changed events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        A RoutedPropertyChangedEventArgs&lt;double&gt; to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SldrNsOctaves_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblNsOctaves.Content = $"Octave: {(int)e.NewValue}";
        }
        #endregion
        #endregion
    }
}
