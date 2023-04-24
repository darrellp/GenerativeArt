using System;
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
        Random _rnd = new Random();

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

        private double _maxScale;
        public double MaxScale
        {
            get => _maxScale;
            set => SetField(ref _maxScale, value);
        }

        private double _posOffset;
        public double PosOffset
        {
            get => _posOffset;
            set => SetField(ref _posOffset, value);
        }

        private double _pctCircles;
        public double PctCircles
        {
            get => _pctCircles;
            set => SetField(ref _pctCircles, value);
        }

        private int ArtHeight => _ourWindow.ArtHeight;
        private int ArtWidth => _ourWindow.ArtWidth;
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
            var cellSize = ArtWidth / GridCount;
            var baseRadius = cellSize * BaseScale / 2;
            var wbmp = BitmapFactory.New(ArtWidth, ArtHeight);
            var circleBreakEven = PctCircles / 100.0;
            using (wbmp.GetBitmapContext())
            {
                wbmp.Clear(Colors.Black);
                for (var ix = 0; ix < GridCount; ix++)
                {
                    for (var iy = 0; iy < GridCount; iy++)
                    {
                        var xc = cellSize * (ix + 0.5 + (2 * _rnd.NextDouble() - 1) * PosOffset / 100);
                        var yc = cellSize * (iy + 0.5 + (2 * _rnd.NextDouble() - 1) * PosOffset / 100);
                        var radius = baseRadius * _rnd.Next(100, (int)MaxScale) / 100;
                        var isCircle = _rnd.NextDouble() < circleBreakEven;
                        if (isCircle)
                        {
                            wbmp.FillEllipseCentered((int)xc, (int)yc, (int)radius, (int)radius, Colors.Red);
                        }
                        else
                        {
                            wbmp.FillRectangle((int)(xc - radius), (int)(yc - radius), (int)(xc + radius), (int)(yc + radius), Colors.Blue);
                        }
                    }
                }
                _ourWindow.Art.Source = wbmp;
            }
        }

        public void Initialize()
        {
            GridCount = 20;
            BaseScale = 0.5;
            MaxScale = 100.1;
            PosOffset = 0.1;
            PctCircles = 50;
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
            _ourWindow.sldrShMaxScale.ValueChanged += sldrShMaxScale_ValueChanged;
            _ourWindow.sldrShPosOffset.ValueChanged +=SldrShPosOffset_ValueChanged;
            _ourWindow.sldrShPctCircles.ValueChanged +=SldrShPctCircles_ValueChanged;
        }

        private void SldrShPctCircles_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShPctCircles.Content = $"Pct Circles: {e.NewValue:##0}%";
        }

        private void SldrShPosOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShPosOffset.Content = $"Pos Offset: {e.NewValue:##0}%";
        }

        private void SldrsldrShBaseScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShBaseScale.Content = $"Base Scale: {e.NewValue:#0.00}";
        }

        private void sldrShGridCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShGridCount.Content = $"Grid Count: {e.NewValue}";
        }

        private void sldrShMaxScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShMaxScale.Content = $"Max Scale: {e.NewValue:##0}%";
        }
        #endregion
    }
}
