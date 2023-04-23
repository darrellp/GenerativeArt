using System.Collections.Generic;
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
            set => SetField(ref _gridCount, value);
        }

        private double _baseScale;
        public double BaseScale
        {
            get => _baseScale;
            set => SetField(ref _baseScale, value);
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

        #region IGenerator interface
        public void Generate()
        {
            var wbmp = BitmapFactory.New(ArtWidth, ArtHeight);
            using (wbmp.GetBitmapContext())
            {
                wbmp.Clear(Colors.Black);
                var cellSize = ArtWidth / GridCount;
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
        /// <summary>   Hook tab page controls. So we can put values on lables when sliders change. </summary>
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
