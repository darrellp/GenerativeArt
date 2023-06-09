﻿using GenerativeArt.Noises;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace GenerativeArt.FlowGenerator
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A flow generator. </summary>
    ///
    /// <remarks>   Based on:
    ///             https://web.cs.ucdavis.edu/~ma/SIGGRAPH02/course23/notes/papers/Jobard.pdf
    ///             Darrell Plank, 4/27/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [JsonObject(MemberSerialization.OptIn)]
    internal class FlowGenerator : IGenerator, INotifyPropertyChanged
    {
        #region Private Variables
        Random _rnd;
        public const int StepDistance = 4;
        [JsonProperty] private int _seed;

        private MainWindow _ourWindow;
        internal int ArtHeight => _ourWindow.ArtHeight;
        internal int ArtWidth => _ourWindow.ArtWidth;

        private bool _evenLineSelection;
        [JsonProperty]
        public bool EvenLineSelection
        {
            get => _evenLineSelection;
            set => SetField(ref _evenLineSelection, value);
        }

        private int _dropBelow;
        [JsonProperty]
        public int DropBelow
        {
            get => _dropBelow;
            set => SetField(ref _dropBelow, value);
        }

        private bool _useAlpha;
        [JsonProperty]
        public bool UseAlpha
        {
            get => _useAlpha;
            set => SetField(ref _useAlpha, value);
        }

        private bool _dotted;
        [JsonProperty]
        public bool Dotted
        {
            get => _dotted;
            set => SetField(ref _dotted, value);
        }

        private double _lineCount;
        [JsonProperty]
        public double LineCount
        {
            get => _lineCount;
            set => SetField(ref _lineCount, value);
        }

        #region Flowers
        private bool _includeFlower;
        [JsonProperty]
        public bool IncludeFlower
        {
            get => _includeFlower;
            set => SetField(ref _includeFlower, value);
        }

        private Point _flowerPt;
        // Flowers have a center radius which is forbidden to streamlines.  Then the petals start and have only the radial direction
        // to influence them for flowerPetalLength.  After that the perlin noise is lerped into the influence until influenceRadius
        // is reached at which point Perlin noise takes over entirely.
        private double InfluenceStart => FlowerCtrRadius + FlowerPetalLength;
        private double InfluenceEnd => InfluenceStart + FlowerDropoff;

        private double _flowerDropoff = 190.0;     // Includes flowerCtrRadius and flowerPetalLength:
                                                  // <--flowerCtrRadius--><--flowerPetalLength--><--FlowerDropoff---->
                                                  // <----------------------influenceEnd----------------------------->
                                                  // Assert(influenceRadius >= flowerCtrRadius + flowerPetalLength);
        [JsonProperty]
        public double FlowerDropoff
        {
            get => _flowerDropoff;
            set => SetField(ref _flowerDropoff, value);
        }


        private double _flowerCtrRadius = 30.0;
        [JsonProperty]
        public double FlowerCtrRadius
        {
            get => _flowerCtrRadius;
            set => SetField(ref _flowerCtrRadius, value);
        }

        private double _flowerPetalLength = 80.0;
        [JsonProperty]
        public double FlowerPetalLength
        {
            get => _flowerPetalLength;
            set => SetField(ref _flowerPetalLength, value);
        }
        #endregion

        private double _angleMultiplier;
        [JsonProperty]
        public double AngleMultiplier
        {
            get => _angleMultiplier;
            set => SetField(ref _angleMultiplier, value);
        }

        private double _borderWidth;
        [JsonProperty]
        public double BorderWidth
        {
            get => _borderWidth;
            set => SetField(ref _borderWidth, value);
        }

        private Color _shortColor;
        [JsonProperty]
        public Color ShortColor
        {
            get => _shortColor;
            set => SetField(ref _shortColor, value);
        }

        private Color _longColor;
        [JsonProperty]
        public Color LongColor
        {
            get => _longColor;
            set => SetField(ref _longColor, value);
        }

        private Color _borderColor;
        [JsonProperty]
        public Color BorderColor
        {
            get => _borderColor;
            set => SetField(ref _borderColor, value);
        }

        private int _shortCount;
        [JsonProperty]
        public int ShortCount
        {
            get => _shortCount;
            set
            {
                var valMod = Math.Min(value, _longCount); 
                SetField(ref _shortCount, valMod);
            }
        }

        private int _longCount;
        [JsonProperty]
        public int LongCount
        {
            get => _longCount;
            set
            {
                var valMod = Math.Max(value, _shortCount);
                SetField(ref _longCount, valMod);
            }
        }

        private int _sampleInterval;
        [JsonProperty]
        public int SampleInterval
        {
            get => _sampleInterval;
            set => SetField(ref _sampleInterval, value);
        }

        private int _octaves;
        [JsonProperty]
        public int Octaves
        {
            get => _octaves;
            set => SetField(ref _octaves, value);
        }

        private double _maxThickness;
        [JsonProperty]
        public double MaxThickness
        {
            get => _maxThickness;
            set => SetField(ref _maxThickness, value);
        }

        private double _getThick;
        [JsonProperty]
        public double GetThick
        {
            get => _getThick;
            set => SetField(ref _getThick, value);
        }

        private double _interlineDistance;
        [JsonProperty]
        public double InterlineDistance
        {
            get => _interlineDistance;
            set => SetField(ref _interlineDistance, value);
        }

        public double _startPtMult;
        [JsonProperty]
        public double StartPtMult
        {
            get => _startPtMult;
            set => SetField(ref _startPtMult, value);
        }
        #endregion

        #region Constructor
        public FlowGenerator(MainWindow main)
        {
            _ourWindow = main;
            HookParameterControls();
        }
        #endregion

        #region IGenerator Interface
        public void Generate(int seed)
        {
            var perlin = new Perlin(seed) { Octaves = Octaves, };
            _rnd = new Random(seed);
            var map = new PointMap(this);

            var xCount = (ArtWidth + InterlineDistance - 1) / InterlineDistance;
            var yCount = (ArtHeight + InterlineDistance - 1) / InterlineDistance;
            var searchLineQueue = new Queue<Streamline>();
            var lines = new List<Streamline>();
            _flowerPt = new Point(_rnd.NextDouble() * ArtWidth, _rnd.NextDouble() * ArtHeight);

            // Calculate all our flow lines
            var iLines = 0;
            if (IncludeFlower)
            {
                MakeFlowers(_flowerPt, searchLineQueue, lines, map, perlin);
            }

            while (true)
            {
                Point startPt;
                if (!EvenLineSelection || searchLineQueue.Count == 0)
                {
                    do
                    {
                        startPt = new Point(ArtWidth * _rnd.NextDouble(), ArtHeight * _rnd.NextDouble());
                    } while (Utilities.Dist2(startPt, _flowerPt) < (FlowerCtrRadius + FlowerPetalLength) * (FlowerCtrRadius + FlowerPetalLength));
                    if (iLines++ >=  LineCount)
                    {
                        break;
                    }
                }
                else
                {
                    bool fFound;
                    while (true)
                    {
                        var searchLine = searchLineQueue.Peek();
                        (fFound, startPt) = searchLine.SearchStart(map);
                        if (!fFound)
                        {
                            searchLineQueue.Dequeue();
                            if (searchLineQueue.Count == 0)
                            {
                                break;
                            }
                            continue;
                        }
                        break;
                    }
                    if (!fFound)
                    {
                        // Tried all the available lines and we couldn't find anything -
                        // that's a wrap!
                        break;
                    }
                }
                var line = ProduceLine(startPt, perlin, map);
                if (line != null)
                {
                    lines.Add(line);
                    searchLineQueue.Enqueue(line);
                }
            }

            // Preamble to drawing
            var rtBitmap = new RenderTargetBitmap(ArtWidth, ArtHeight, 96, 96, PixelFormats.Default);
            var vis = new DrawingVisual();
            var dc = vis.RenderOpen();

            // Clear the window
            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ArtWidth, ArtHeight));

            foreach (var streamline in lines.Where(streamline => streamline.Count >= DropBelow))
            {
                streamline.Draw(dc);
            }

            // Postlude to drawing
            dc.Close();
            rtBitmap.Render(vis);
            _ourWindow.Art.Source = rtBitmap;
        }

        private void MakeFlowers(Point flowerPt, Queue<Streamline> searchLineQueue, List<Streamline>  lines, PointMap map, Perlin perlin)
        {
            var innerCircumference = 2 * Math.PI * FlowerCtrRadius;
            var petalCount = (int)Math.Floor(innerCircumference / InterlineDistance);
            var randOffset = _rnd.NextDouble() * Math.PI * 2;

            for (var iPetal = 0; iPetal < petalCount; iPetal++)
            {
                var angle = 2 * Math.PI *iPetal / petalCount + randOffset;
                var ptStart = new Point(flowerPt.X + Math.Cos(angle) * FlowerCtrRadius, flowerPt.Y + Math.Sin(angle) * FlowerCtrRadius);
                var line = ProduceLine(ptStart, perlin, map, true);
                if (line != null)
                {
                    lines.Add(line);
                    searchLineQueue.Enqueue(line);
                }
            }
        }

        private Streamline? ProduceLine(Point ptStart, Perlin perlin, PointMap map, bool fForwardOnly = false)
        {
            var ptCur = ptStart;
            var line = new Streamline(this, fForwardOnly);

            if (!map.Register(line, ptStart, true))
            {
                return null;
            }

            int iLoops = 0;
            while (OnBoard(ptCur))
            {
                var oldPt = ptCur;
                ptCur = NextPoint(ptCur, StepDistance, perlin);
                // if we're too close to something else, hop out - also no infinite loops
                if (!map.Register(line, ptCur, true) || ++iLoops > 500)
                {
                    break;
                }
            }

            if (fForwardOnly)
            {
                return line;
            }

            ptCur = NextPoint(ptStart, -StepDistance, perlin);
            if (!map.Register(line, ptCur, false))
            {
                return line;
            }

            int iLoop = 0;
            while (OnBoard(ptCur) && iLoop++ < 500)
            {
                var oldPt = ptCur;
                ptCur = NextPoint(ptCur, -StepDistance, perlin);
                // if we're too close to something else, hop out
                if (!map.Register(line, ptCur, false))
                {
                    break;
                }
            }

            return line;
        }

        internal bool OnBoard(Point pt)
        {
            return pt.X >= 0 && pt.Y >= 0 && pt.X < ArtWidth && pt.Y < ArtHeight;
        }

        Point NextPoint( Point pt, double stepDistance, Perlin perlin)
        {
            var angle = 0.0;
            var flowerDistance = IncludeFlower ? Utilities.Dist(pt, _flowerPt) : Double.MaxValue;
            Debug.Assert(flowerDistance >= FlowerCtrRadius - stepDistance);
            var anglePerlin = perlin.Value(pt.X / ArtWidth, pt.Y / ArtHeight) * AngleMultiplier;
            if (!IncludeFlower || flowerDistance > InfluenceEnd)
            {
                angle = anglePerlin;
            }
            else
            {
                var angleCenter = Math.Atan2(pt.Y - _flowerPt.Y, pt.X - _flowerPt.X);
                if (flowerDistance < InfluenceStart)
                {
                    angle = angleCenter;
                }
                else if (flowerDistance < InfluenceEnd)
                {
                    angle = Utilities.LerpAngle(angleCenter, anglePerlin, 
                        (flowerDistance - InfluenceStart) / (InfluenceEnd - InfluenceStart));
                }
            }

            // Higher multipliers result in more chaos
            return new Point(Math.Cos(angle) * stepDistance + pt.X, Math.Sin(angle) * stepDistance + pt.Y);
        }

        public void Initialize()
        {
            Dotted = false;
            LineCount = 800;
            EvenLineSelection = true;
            AngleMultiplier = 35;
            ShortCount = 20;
            LongCount = 100;
            ShortColor = Colors.Green;
            LongColor = Colors.Yellow;
            BorderColor = Colors.Red;
            SampleInterval = 7;
            MaxThickness = 7;
            GetThick = 0.5;
            InterlineDistance = 5;
            Octaves = 2;
            StartPtMult = 1.5;
            UseAlpha = false;
            DropBelow = 10;
            BorderWidth = 0.0;
            IncludeFlower = false;
            FlowerCtrRadius = 30.0;
            FlowerPetalLength = 80.0;
            FlowerDropoff = 190.0;
            _ourWindow.btnFlLongColor.Background = new SolidColorBrush(LongColor);
            _ourWindow.btnFlShortColor.Background = new SolidColorBrush(ShortColor);
            _ourWindow.btnFlBorderColor.Background = new SolidColorBrush(BorderColor);
        }

        public void Kill()
        {
            throw new NotImplementedException();
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

        public string SerialExtension => "flw";
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

        #region Event Handling
        private void HookParameterControls()
        {
            _ourWindow.sldrFlOctaves.ValueChanged +=SldrFlOctaves_ValueChanged;
            _ourWindow.sldrFlInterlineDistance.ValueChanged +=SldrFlInterlineDistance_ValueChanged;
            _ourWindow.sldrFlLineThickness.ValueChanged +=SldrFlLineThickness_ValueChanged;
            _ourWindow.sldrFlAngleMultiplier.ValueChanged +=SldrFlAngleMultiplier_ValueChanged;
            _ourWindow.sldrFlShortCount.ValueChanged +=SldrFlShortCount_ValueChanged;
            _ourWindow.sldrFlLongCount.ValueChanged +=SldrFlLongCount_ValueChanged;
            _ourWindow.sldrFlSampleInterval.ValueChanged +=SldrFlSampleInterval_ValueChanged;
            _ourWindow.sldrFlThickRatio.ValueChanged +=SldrFlThickRatio_ValueChanged;
            _ourWindow.sldrFlStartPtMult.ValueChanged +=SldrFlStartPtMult_ValueChanged;
            _ourWindow.sldrFlDropBelow.ValueChanged +=SldrFlDropBelow_ValueChanged;
            _ourWindow.sldrFlBorderWidth.ValueChanged +=SldrFlBorderWidth_ValueChanged;
            _ourWindow.btnFlShortColor.Click += BtnFlShortColor_Click;
            _ourWindow.btnFlLongColor.Click +=BtnFlLongColor_Click;
            _ourWindow.btnFlBorderColor.Click +=BtnFlBorderColor_Click;
            _ourWindow.btnFlowerParms.Click +=BtnFlowerParms_Click;
        }

        private void BtnFlowerParms_Click(object sender, RoutedEventArgs e)
        {
            var flowerDlg = new FlowerParameters
            {
                DataContext = this
            };
            flowerDlg.ShowDialog();
        }

        private void BtnFlBorderColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = _ourWindow.btnFlBorderColor;
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, color) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(color);
                BorderColor = color;
            }
        }

        private void BtnFlLongColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = _ourWindow.btnFlLongColor;
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, color) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(color);
                LongColor = color;
            }
        }

        private void BtnFlShortColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = _ourWindow.btnFlShortColor;
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, color) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(color);
                ShortColor = color;
            }
        }

        private void SldrFlBorderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlBorderWidth.Content = $"Border Width: {e.NewValue:0.00}";
        }

        private void SldrFlDropBelow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlDropBelow.Content = $"Drop all below length: {e.NewValue:##0}";
        }

        private void SldrFlStartPtMult_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlStartPtMult.Content = $"Start Pt Multiplier: {e.NewValue:0.##}";
        }

        private void SldrFlThickRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlThickRatio.Content = $"Thick ratio to end: {e.NewValue:0.##}";
        }

        private void SldrFlSampleInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlSampleInterval.Content = $"Sample Interval: {e.NewValue:##}";
        }

        private void SldrFlLongCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlLongCount.Content = $"Long Color Thresh: {e.NewValue:##0}";
        }

        private void SldrFlShortCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlShortCount.Content = $"Short Color Thresh: {e.NewValue:##0}";
        }

        private void SldrFlAngleMultiplier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlAngleMultiplier.Content = $"Angle Multiplier: {e.NewValue:##0.#}";
        }

        private void SldrFlOctaves_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlOctaves.Content = $"Octaves: {e.NewValue:##0.#}";
        }

        private void SldrFlLineThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
           _ourWindow.lblFlLineThickness.Content = $"Line Thickness: {e.NewValue:#0.#} pixels";
        }

        private void SldrFlInterlineDistance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblFlInterlineDistance.Content = $"Line Separation: {e.NewValue:#0.#} pixels";
        }
        #endregion
    }
}

