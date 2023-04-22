
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
        // TODO: use XAML Binding
        #region Private Variables
        private int Octaves
        {
            get => (int)_ourWindow.sldrNsOctaves.Value;
            set => _ourWindow.sldrNsOctaves.Value = value;
        }
        private double Frequency
        {
            get => _ourWindow.sldrNsFrequency.Value;
            set => _ourWindow.sldrNsFrequency.Value = value;
        }

        private double Persistence
        {
            get => _ourWindow.sldrNsPersistence.Value;
            set => _ourWindow.sldrNsPersistence.Value = value;
        }

        /// <summary>   Main window. </summary>
        private readonly MainWindow _ourWindow;

        /// <summary>   Width of the art control. </summary>
        private int ArtWidth => _ourWindow.ArtWidth;

        /// <summary>   Height of the art control. </summary>
        private int ArtHeight => _ourWindow.ArtHeight;
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
            var wbmp = BitmapFactory.New(ArtWidth, ArtHeight);
            wbmp.Clear(Colors.Black);
            _ourWindow.Art.Source = wbmp;
            Debug.Assert(wbmp.Format == PixelFormats.Pbgra32);
            var pixels = DrawPerlin();
            var sizePixel = Marshal.SizeOf(typeof(PixelColor));
            var stride = ArtWidth * sizePixel;
            wbmp.WritePixels(new Int32Rect(0, 0, ArtWidth, ArtHeight), pixels, stride, 0);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes this object.  Resets all the parameters on the tabs page. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Initialize()
        {
            Frequency = 7;
            Persistence = 0.5;
            Octaves = 6;
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
                Frequency = Frequency,
                Octaves = Octaves,
                Persistence = Persistence,
            };

            var pixelData = new PixelColor[ArtWidth * ArtHeight];
            for (var iX = 0; iX < ArtWidth; iX++)
            {
                var ixNorm = iX/(double)ArtWidth;
                for (var iY = 0; iY < ArtHeight; iY++)
                {
                    var iyNorm = iY / (double)ArtHeight;
                    var val = (Byte)(noise.Value(ixNorm, iyNorm) * 255);
                    pixelData[iY * ArtWidth + iX] = new(val, val, val);
                }
            }
            return pixelData;
        }
        #endregion

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
    }
}
