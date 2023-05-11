using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            var cellSize = ArtWidth / GridCount;
            var baseRadius = cellSize * BaseScale / 2;
            var circleBreakEven = PctCircles / 100.0;

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
                        var color = _circlePalette.SelectColor(_rnd);
                        dc.DrawEllipse(new SolidColorBrush(color), null, new Point(xc, yc), radius, radius);
                    }
                    else
                    {
                        var color = _useCircleColors ? _circlePalette.SelectColor(_rnd) : _squarePalette.SelectColor(_rnd);
                        var angle = 2 * (_rnd.NextDouble() - 0.5) * AngleVariance;
                        var rect = new Rect(xc - radius, yc - radius, 2 * radius, 2 * radius);
                        DrawRotRect(dc, color, null, angle, rect);
                    }
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
            _ourWindow.btnShCircleColors.Click +=BtnShCircleColors_Click;
            _ourWindow.btnShSquareColors.Click +=BtnShSquareColors_Click;
        }

        private void BtnShSquareColors_Click(object sender, RoutedEventArgs e)
        {
            _squarePalette = _squarePalette.GetUserPalette();
        }

        private void BtnShCircleColors_Click(object sender, RoutedEventArgs e)
        {
            _circlePalette = _circlePalette.GetUserPalette();
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
