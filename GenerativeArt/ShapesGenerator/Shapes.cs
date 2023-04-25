using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        #region Colors
        private Color[] _circleColors = new Color[4];
        private bool[] _circleColorEnabled = new bool[4];
        private Color[] _squareColors = new Color[4];
        private bool[] _squareColorEnabled = new bool[4];
        private bool _useCircleColors;
        private double _varCircleH;
        private double _varCircleS;
        private double _varCircleB;
        private double _varSquareH;
        private double _varSquareS;
        private double _varSquareB;
        #endregion

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
            var circleBreakEven = PctCircles / 100.0;
            List<Color> circleColors = new();
            List<Color> squareColors = new();
            for (int iColor = 0; iColor < 4; iColor++)
            {
                if (_circleColorEnabled[iColor])
                {
                    circleColors.Add(_circleColors[iColor]);
                }
                if (_squareColorEnabled[iColor])
                {
                    squareColors.Add(_squareColors[iColor]);
                }
            }

            if (circleColors.Count == 0)
            {
                circleColors.Add(Colors.Red);
            }

            if (squareColors.Count == 0)
            {
                squareColors.Add(Colors.Blue);
            }

            var hsbCircles = circleColors.Select(c => new HSB(c)).ToArray();
            var hsbSquares = squareColors.Select(c => new HSB(c)).ToArray();


            var rtBitmap = new RenderTargetBitmap(ArtWidth, ArtHeight, 96, 96, PixelFormats.Default);
            DrawingVisual vis = new DrawingVisual();
            DrawingContext dc = vis.RenderOpen();
            dc.DrawRectangle(new SolidColorBrush(Colors.Black), null, new Rect(0, 0, ArtWidth, ArtHeight));
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
                        var color = SelectColor(hsbCircles, _varCircleH, _varCircleS, _varCircleB);
                        dc.DrawEllipse(new SolidColorBrush(color), null, new Point(xc, yc), radius, radius);
                    }
                    else
                    {
                        Color color;
                        if (_useCircleColors)
                        {
                            color = SelectColor(hsbCircles, _varCircleH, _varCircleS, _varCircleB);
                        }
                        else
                        {
                            color = SelectColor(hsbSquares, _varSquareH, _varSquareS, _varSquareB);
                        }
                        dc.DrawRectangle(new SolidColorBrush(color), null, new Rect(xc - radius, yc - radius, 2 * radius, 2 * radius));
                    }
                }
            }
            dc.Close();
            rtBitmap.Render(vis);
            _ourWindow.Art.Source = rtBitmap;
        }

        Color SelectColor(HSB[] palette, double varH, double varS, double varB)
        {
            var hsbSelected = palette[_rnd.Next(palette.Length)];
            var h = hsbSelected.H + 2*(_rnd.NextDouble() - 0.5) * varH;
            if (h < 0) h += 360;
            else if (h > 360) h -= 360;
            var s = hsbSelected.S  + 2*(_rnd.NextDouble() - 0.5) * varS;
            if (s < 0) s = 0;
            if (s > 1) s = 1;
            var b = hsbSelected.B  + 2*(_rnd.NextDouble() - 0.5) * varB;
            if (b < 0) b = 0;
            if (b > 1) b = 1;
            return HSB.ColorFromHSB(h, s, b);
        }

        public void Initialize()
        {
            GridCount = 20;
            BaseScale = 0.5;
            MaxScale = 100.1;
            PosOffset = 0.1;
            PctCircles = 50;
            _useCircleColors = false;
            _circleColors = new Color[4] {Colors.Red, Colors.Black, Colors.Black, Colors.Black};
            _squareColors = new Color[4] { Colors.Blue, Colors.Black, Colors.Black, Colors.Black };
            _circleColorEnabled = new bool[4] { true, false, false, false };
            _squareColorEnabled = new bool[4] { true, false, false, false };
            _varCircleH = _varCircleS = _varCircleB 
                = _varSquareH = _varSquareS = _varSquareB = 0;
    }

    public void Kill()
        {
        }
        #endregion

        #region Hooks
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Hook tab page controls. So we can put values on labels when sliders change. </summary>
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
            _ourWindow.btnShColors.Click += BtnShColors_Click;
        }

        private void BtnShColors_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ShapeColors();
            XferToDlg(dlg);
            if (dlg.ShowDialog() == true)
            {
                XferFromDlg(dlg);
            }
        }

        private void XferToDlg(ShapeColors dlg)
        {
            dlg.chkUseCircles.IsChecked = _useCircleColors;
            for (var i = 1; i <= 4; i++)
            {
                XferColorToDlg(dlg, i, _circleColors, "Circle");
                XferColorToDlg(dlg, i, _squareColors, "Square");
                XferEnablesToDlg(dlg, i, _circleColorEnabled, "Circle");
                XferEnablesToDlg(dlg, i, _squareColorEnabled, "Square");
            }

            dlg.sldrSquareHVar.Value = _varSquareH;
            dlg.sldrSquareSVar.Value = _varSquareS;
            dlg.sldrSquareBVar.Value = _varSquareB;
            dlg.sldrCircleHVar.Value = _varCircleH;
            dlg.sldrCircleSVar.Value = _varCircleS;
            dlg.sldrCircleBVar.Value = _varCircleB;
        }

        private void XferFromDlg(ShapeColors dlg)
        {
            _useCircleColors = dlg.chkUseCircles.IsChecked??false;
            for (var i = 1; i <= 4; i++)
            {
                XferColorFromDlg(dlg, i, _circleColors, "Circle");
                XferColorFromDlg(dlg, i, _squareColors, "Square");
                XferEnablesFromDlg(dlg, i, _circleColorEnabled, "Circle");
                XferEnablesFromDlg(dlg, i, _squareColorEnabled, "Square");
            }

            _varSquareH = dlg.sldrSquareHVar.Value;
            _varSquareS = dlg.sldrSquareSVar.Value;
            _varSquareB = dlg.sldrSquareBVar.Value;
            _varCircleH = dlg.sldrCircleHVar.Value;
            _varCircleS = dlg.sldrCircleSVar.Value;
            _varCircleB = dlg.sldrCircleBVar.Value;

        }

        private void XferColorFromDlg(ShapeColors dlg, int index, Color[] array, string shapeName)
        {
            var nameColor = $"btn{shapeName}Color{index:0}";
            var btnColor = dlg.FindName(nameColor) as Button;
            Debug.Assert(btnColor != null);
            var brush = btnColor.Background as SolidColorBrush;
            Debug.Assert(brush != null);
           array[index - 1] = brush.Color;
        }

        private void XferEnablesFromDlg(ShapeColors dlg, int index, bool[] array, string shapeName)
        {
            var nameColor = $"chk{shapeName}Color{index:0}";
            var checkbox = dlg.FindName(nameColor) as CheckBox;
            Debug.Assert(checkbox != null);
            array[index - 1] = checkbox.IsChecked == true;
        }

        private void XferColorToDlg(ShapeColors dlg, int index, Color[] array, string shapeName)
        {
            var nameColor = $"btn{shapeName}Color{index:0}";
            var btnColor = dlg.FindName(nameColor) as Button;
            Debug.Assert(btnColor != null);
            var brush = new SolidColorBrush(array[index - 1]);
            Debug.Assert(brush != null);
            btnColor.Background = brush;
        }

        private void XferEnablesToDlg(ShapeColors dlg, int index, bool[] array, string shapeName)
        {
            var nameColor = $"chk{shapeName}Color{index:0}";
            var checkbox = dlg.FindName(nameColor) as CheckBox;
            Debug.Assert(checkbox != null);
            checkbox.IsChecked = array[index - 1];
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
