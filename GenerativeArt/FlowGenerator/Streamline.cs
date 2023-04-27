using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace GenerativeArt.FlowGenerator
{
    internal class Streamline
    {
        private List<Point> _fwd = new();
        private List<Point> _bwd = new();
        Color shortColor = Colors.Green;
        Color longColor = Colors.Yellow;
        const int cShort = 20;
        const int cLong = 100;
        private double maxThickness = 3;
        private double getThick = 0.2;

        internal int FwdCount => _fwd.Count;
        internal int BwdCount => _bwd.Count;
        internal int Count => _fwd.Count + _bwd.Count;

        internal int Add(Point pt, bool fForward)
        {
            var list = (fForward ? _fwd : _bwd);

            list.Add(pt);
            return fForward ? list.Count - 1 : -list.Count;
        }

        internal void Draw(DrawingContext dc)
        {
            var t = Count switch
            {
                < cShort => 0.0,
                > cLong => 1.0,
                _ => (double)(Count - cShort)/(cLong - cShort),
            };
            var thickThresh = Count * getThick;

            for (var ivtx = -BwdCount; ivtx < FwdCount - 1; ivtx++)
            {
                var distFromBack = BwdCount + ivtx;
                var distFromFront = FwdCount - ivtx;
                var dist = Math.Min(distFromBack, distFromFront);
                var thickness = maxThickness;
                var alpha = 255.0;

                if (dist < thickThresh)
                {
                    var ratio = (double)dist / thickThresh;
                    thickness *= ratio;
                    alpha *= ratio;
                }

                var color = Utilities.LerpColor(shortColor, longColor, t);
                color.A = (byte)alpha;
                var brush = new SolidColorBrush(color);
                var flow = new Pen(brush, thickness);
                dc.DrawLine(flow, this[ivtx], this[ivtx + 1]);
            }
        }

        public Point this[int i] => i < 0 ? _bwd[-1 - i] : _fwd[i];
    }
}
