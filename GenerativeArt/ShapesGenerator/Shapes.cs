using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenerativeArt.ShapesGenerator
{
    public class Shapes : IGenerator, INotifyPropertyChanged
    {
        #region Private Variables
        private MainWindow _ourWindow { get; }

        private double _gridCount;
        public double GridCount
        {
            get => _gridCount;
            set
            {
                _gridCount = value;
                NotifyPropertyChanged(nameof(GridCount));
            }
        }

        private double _baseScale;
        public double BaseScale
        {
            get => _baseScale;
            set
            {
                _baseScale = value;
                NotifyPropertyChanged(nameof(BaseScale));
            }
        }
        private int ArtWidth => _ourWindow.ArtWidth;
        private int ArtHeight => _ourWindow.ArtHeight;
        #endregion

        #region Constructor
        internal Shapes(MainWindow ourWindow)
        {
            _ourWindow = ourWindow;
            HookParameterControls();
        }
        #endregion

        #region Property changes
        public Shapes() {}

        public event PropertyChangedEventHandler? PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IGenerator interface
        public void Generate()
        {
            var wbmp = BitmapFactory.New(ArtWidth, ArtHeight);
            using (wbmp.GetBitmapContext())
            {
                wbmp.Clear(Colors.Black);
                var cellSize = (double)ArtWidth / GridCount;
                var baseRadius = cellSize * BaseScale / 2;
                for (var ix = 0; ix < GridCount; ix++)
                {
                    for (var iy = 0; iy < GridCount; iy++)
                    {
                        var xc = (ix + 0.5) * cellSize;
                        var yc = (iy + 0.5) * cellSize;
                        wbmp.FillEllipseCentered((int)xc, (int)yc, (int)baseRadius, (int)baseRadius, Colors.Red);
                    }
                }
                _ourWindow.Art.Source = wbmp;
            }
        }

        public void Initialize()
        {
            GridCount = 20;
            BaseScale = 0.5;
        }

        public void Kill()
        {
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
            _ourWindow.sldrShGridCount.ValueChanged += sldrShGridCount_ValueChanged;
            _ourWindow.sldrShBaseScale.ValueChanged += SldrsldrShBaseScale_ValueChanged;
        }

        private void SldrsldrShBaseScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShBaseScale.Content = $"Base Scale: {e.NewValue:#0.00}";
        }

        private void sldrShGridCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShGridCount.Content = $"Grid Count: {e.NewValue}";
        }
        #endregion
    }
}
