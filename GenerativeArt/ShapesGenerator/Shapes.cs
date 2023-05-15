using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MathNet.Numerics.Distributions;

namespace GenerativeArt.ShapesGenerator
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Shapes : IGenerator, INotifyPropertyChanged
    {
        #region Private Variables
        Random _rnd;

        private MainWindow _ourWindow { get; }
        [JsonProperty] private int _seed;

        private double _gridCount;
        [JsonProperty]
        public double GridCount
        {
            get => _gridCount;
            set => SetField(ref _gridCount, value);
        }

        private double _baseScale;
        [JsonProperty]
        public double BaseScale
        {
            get => _baseScale;
            set => SetField(ref _baseScale, value);
        }

        private bool _fixedInsets;
        [JsonProperty]
        public bool FixedInsets
        {
            get => _fixedInsets;
            set => SetField(ref _fixedInsets, value);
        }

        private int _insetsMean;
        [JsonProperty]
        public int InsetsMean
        {
            get => _insetsMean;
            set => SetField(ref _insetsMean, value);
        }

        private int _alpha;
        [JsonProperty]
        public int Alpha
        {
            get => _alpha;
            set => SetField(ref _alpha, value);
        }

        private double _insetsStdDev;
        [JsonProperty]
        public double InsetsStdDev
        {
            get => _insetsStdDev;
            set => SetField(ref _insetsStdDev, value);
        }

        private double _maxScale;
        [JsonProperty]
        public double MaxScale
        {
            get => _maxScale;
            set => SetField(ref _maxScale, value);
        }

        private double _posOffset;
        [JsonProperty]
        public double PosOffset
        {
            get => _posOffset;
            set => SetField(ref _posOffset, value);
        }

        private double _borderWidth;
        [JsonProperty]
        public double BorderWidth
        {
            get => _borderWidth;
            set => SetField(ref _borderWidth, value);
        }

        private double _pctCircles;
        [JsonProperty]
        public double PctCircles
        {
            get => _pctCircles;
            set => SetField(ref _pctCircles, value);
        }

        private double _angleVariance;
        [JsonProperty]
        public double AngleVariance
        {
            get => _angleVariance;
            set => SetField(ref _angleVariance, value);
        }

        #region Colors
        private Color _borderColor;
        [JsonProperty]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                var brush = new SolidColorBrush(value);
                _ourWindow.btnShBorderColor.Background = brush;
                SetField(ref _borderColor, value);
            }
        }

        private bool _useCircleColors;
        [JsonProperty]
        private Palette _circlePalette;
        [JsonProperty]
        private Palette _squarePalette;
        #endregion

        private int ArtHeight => _ourWindow.ArtHeight;
        private int ArtWidth => _ourWindow.ArtWidth;
        #endregion

        #region Constructor
        static readonly Palette DefaultPalette = new Palette()
        {
            Color1 = Colors.Red,
            Color2 = Colors.Black,
            Color3 = Colors.Black,
            Color4 = Colors.Black,
            Color5 = Colors.Black,
            Enabled1 = true,
            Enabled2 = false,
            Enabled3 = false,
            Enabled4 = false,
            Enabled5 = false,
            VarH = 0,
            VarS = 0,
            VarB = 0,
        };

        internal Shapes(MainWindow ourWindow)
        {
            _ourWindow = ourWindow;
            _circlePalette = new Palette(DefaultPalette);
            _squarePalette = new Palette(DefaultPalette);
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
        public void Generate(int seed = -1)
        {
            _rnd = new Random(seed);
            var normal = new Normal(InsetsMean, InsetsMean / 2.0);
            // var normal = new Normal(InsetsMean, InsetsStdDev);
            normal.RandomSource = new Random(seed);
            var cellSize = ArtWidth / GridCount;
            var baseRadius = cellSize * BaseScale / 2;
            var circleBreakEven = PctCircles / 100.0;

            var rtBitmap = new RenderTargetBitmap(ArtWidth, ArtHeight, 96, 96, PixelFormats.Default);
            var vis = new DrawingVisual();
            var dc = vis.RenderOpen();
            dc.DrawRectangle(new SolidColorBrush(Colors.Black), null, new Rect(0, 0, ArtWidth, ArtHeight));
            var shuffledPoints = new List<(int x, int y)>();
            for (var ix = 0; ix < GridCount; ix++)
            {
                for (var iy = 0; iy < GridCount; iy++)
                {
                    shuffledPoints.Add((ix, iy));
                }
            }
            Utilities.Shuffle<(int, int)>(shuffledPoints, _rnd);

            for (var i = 0; i < shuffledPoints.Count; i++)
            {
                var pen = BorderWidth > 0 ? new Pen(new SolidColorBrush(BorderColor), BorderWidth) : null;
                var ix = shuffledPoints[i].x;
                var iy = shuffledPoints[i].y;
                var xc = cellSize * (ix + 0.5 + (2 * _rnd.NextDouble() - 1) * PosOffset / 100);
                var yc = cellSize * (iy + 0.5 + (2 * _rnd.NextDouble() - 1) * PosOffset / 100);
                var outerRadius = baseRadius * _rnd.Next(100, (int)MaxScale) / 100;
                var isCircle = _rnd.NextDouble() < circleBreakEven;

                // 1 shapeInset means just the plain shape drawn
                // No perfect solution (other than using a more suitable distribution which is probably what
                // I should do here) to negative values but they're either rare or this isn't a bad solution
                
                // TODO: Use a good distribution which won't give us negative values
                var shapeInsets = FixedInsets ? InsetsMean : (int)Math.Abs(normal.Sample()) + 1;
                var angle = 2 * (_rnd.NextDouble() - 0.5) * AngleVariance;
                
                for (int iInsets = 0; iInsets <= shapeInsets; iInsets++)
                {
                    var radius = outerRadius * (shapeInsets - iInsets)/ (shapeInsets);
                    if (isCircle)
                    {
                        var color = _circlePalette.SelectColor(_rnd);
                        color.A = (byte)Alpha;
                        dc.DrawEllipse(new SolidColorBrush(color), pen, new Point(xc, yc), radius, radius);
                    }
                    else
                    {
                        var color = _useCircleColors ? _circlePalette.SelectColor(_rnd) : _squarePalette.SelectColor(_rnd);
                        color.A = (byte)Alpha;
                        var rect = new Rect(xc - radius, yc - radius, 2 * radius, 2 * radius);
                        DrawRotRect(dc, color, pen, angle, rect);
                    }

                    pen = null;
                }
            }

            dc.Close();
            rtBitmap.Render(vis);
            _ourWindow.Art.Source = rtBitmap;
        }

        internal void DrawRotRect(DrawingContext dc, Color color, Pen? pen, double angle, Rect rect)
        {
            var ctr = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            var xfmRotate = new RotateTransform(angle, ctr.X, ctr.Y);
            dc.PushTransform(xfmRotate);
            dc.DrawRectangle(new SolidColorBrush(color), pen, rect);
            dc.Pop();
        }

        public void Initialize()
        {
            GridCount = 20;
            BaseScale = 0.5;
            MaxScale = 100.1;
            PosOffset = 0.1;
            PctCircles = 50;
            AngleVariance = 0.01;
            FixedInsets = true;
            InsetsMean = 2;
            InsetsMean = 1;
            InsetsStdDev = 0;
            Alpha = 255;
            BorderColor = Colors.Black;
            BorderWidth = 0;
            _useCircleColors = false;
            _circlePalette = new Palette(DefaultPalette);
            _squarePalette = new Palette(DefaultPalette);
            _squarePalette.Color1 = Colors.Blue;
            _squarePalette.PaletteRGBToHSB();
        }

        public void Kill()
        {
        }

        public string Serialize(int seed)
        {
            _seed = seed;
            return JsonConvert.SerializeObject(this);
        }

        public int Deserialize(string json)
        {
            JsonConvert.PopulateObject(json, this);
            return _seed;
        }

        public string SerialExtension => "shp";
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
            _ourWindow.sldrShAngleVariance.ValueChanged +=SldrShAngleVariance_ValueChanged;
            _ourWindow.sldrShBorderWidth.ValueChanged +=SldrShBorderWidth_ValueChanged;
            _ourWindow.sldrShInsets.ValueChanged +=SldrShInsets_ValueChanged;
            _ourWindow.sldrShAlpha.ValueChanged +=SldrShAlpha_ValueChanged; ;
            _ourWindow.btnShCircleColors.Click +=BtnShCircleColors_Click;
            _ourWindow.btnShSquareColors.Click +=BtnShSquareColors_Click;
            _ourWindow.btnShBorderColor.Click +=BtnShBorderColor_Click;
        }

        private void BtnShBorderColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = _ourWindow.btnShBorderColor;
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, color) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(color);
                BorderColor = color;
            }
        }

        private void BtnShSquareColors_Click(object sender, RoutedEventArgs e)
        {
            _squarePalette = _squarePalette.GetUserPalette();
        }

        private void BtnShCircleColors_Click(object sender, RoutedEventArgs e)
        {
            _circlePalette = _circlePalette.GetUserPalette();
        }

        private void SldrShAlpha_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShAlpha.Content = $"Opacity: {e.NewValue: ##0}";
        }

        private void SldrShBorderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShBorderWidth.Content = $"Border Width: {e.NewValue:0.00}";
        }

        private void SldrShInsets_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShInsets.Content = $"Insets: {e.NewValue:#0}";
        }

        private void SldrShAngleVariance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShAngleVariance.Content = $"Angle Variance: {e.NewValue:##0}°";
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
