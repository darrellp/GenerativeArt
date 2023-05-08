
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    internal class NoiseGenerator : IGenerator, INotifyPropertyChanged
    {
        #region Private Variables
        private double _octaves;
        public double Octaves
        {
            get => _octaves;
            set => SetField(ref _octaves, value);
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

        public void Generate(int seed = -1)
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
                Octaves = (int)Octaves,
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

        #region INotifyPropertyChanged
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
