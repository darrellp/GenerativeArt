using GenerativeArt.Noises;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
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

    internal class FlowGenerator : IGenerator
    {
        #region Private Variables
        Random _rnd = new Random();

        private MainWindow _ourWindow;
        private int ArtHeight => _ourWindow.ArtHeight;
        private int ArtWidth => _ourWindow.ArtWidth;

        #endregion

        #region Constructor
        public FlowGenerator(MainWindow main)
        {
            _ourWindow = main;
        }
        #endregion

        #region IGenerator Interface
        public void Generate()
        {
            var perlin = new Perlin() { Octaves = 3, };
            var dist = 6;
            var interlineDistance = 7;
            var map = new PointMap(interlineDistance, ArtWidth, ArtHeight);

            var xCount = (ArtWidth + interlineDistance - 1) / interlineDistance;
            var yCount = (ArtHeight + interlineDistance - 1) / interlineDistance;
            var lines = new List<Streamline>();

            // Calculate all our flow lines
            for (var i = 0; i < 400; i++)
            {
                var startPt = new Point(ArtWidth * _rnd.NextDouble(), ArtHeight * _rnd.NextDouble());
                var line = ProduceLine(startPt, dist, perlin, map);
                if (line != null)
                {
                    lines.Add(line);
                }
            }

            // Preamble to drawing
            var rtBitmap = new RenderTargetBitmap(ArtWidth, ArtHeight, 96, 96, PixelFormats.Default);
            var vis = new DrawingVisual();
            var dc = vis.RenderOpen();

            // Clear the window
            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ArtWidth, ArtHeight));

            foreach (var streamline in lines.Where(streamline => streamline.Count >= 10))
            {
                streamline.Draw(dc);
            }

            // Postlude to drawing
            dc.Close();
            rtBitmap.Render(vis);
            _ourWindow.Art.Source = rtBitmap;
        }

        private Streamline? ProduceLine(Point ptStart, double dist, Perlin perlin, PointMap map)
        {
            var ptCur = ptStart;
            var line = new Streamline();

            if (!map.Register(line, ptStart, true))
            {
                return null;
            }

            while (OnBoard(ptCur))
            {
                var oldPt = ptCur;
                ptCur = NextPoint(ptCur, dist, perlin);
                // if we're too close to something else, hop out
                if (!map.Register(line, ptCur, true))
                {
                    break;
                }
            }

            ptCur = NextPoint(ptStart, -dist, perlin);
            if (!map.Register(line, ptCur, false))
            {
                return line;
            }

            while (OnBoard(ptCur))
            {
                var oldPt = ptCur;
                ptCur = NextPoint(ptCur, -dist, perlin);
                // if we're too close to something else, hop out
                if (!map.Register(line, ptCur, false))
                {
                    break;
                }
            }

            return line;
        }

        private bool OnBoard(Point pt)
        {
            return pt.X >= 0 && pt.Y >= 0 && pt.X < ArtWidth && pt.Y < ArtHeight;
        }

        Point NextPoint( Point pt, double dist, Perlin perlin)
        {
            // Higher multipliers result in more chaos
            var angle = perlin.Value(pt.X / ArtWidth, pt.Y / ArtHeight) * 20;
            return new Point(Math.Cos(angle) * dist + pt.X, Math.Sin(angle) * dist + pt.Y);
        }

        public void Initialize()
        {
        }

        public void Kill()
        {
            throw new NotImplementedException();
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
    }
}

