using System;
using System.Collections.Generic;
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
        private double maxThickness = 7;
        private double getThick = 0.5;
        private bool fSearching = false;
        private int searchPos = 0;
        private int sampleInterval = 7;
        private bool fFirstSide = true;

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
                //color.A = (byte)alpha;
                var brush = new SolidColorBrush(color);
                var flow = new Pen(brush, thickness);
                flow.EndLineCap = PenLineCap.Round;
                ;
                dc.DrawLine(flow, this[ivtx], this[ivtx + 1]);
            }
        }

        internal (bool fFound, Point pt) SearchStart(PointMap map)
        {
            if (!fSearching)
            {
                fSearching = true;
                // We start one before the end so that we're guaranteed points on each
                // side of us which is what we'll use to generate our perpindicular for
                // our test point.
                searchPos = -BwdCount + 1;
            }

            var ptRet = new Point();

            while (searchPos < FwdCount - 1)
            {
                (var fFound, ptRet) = CheckSample(map);
                if (fFound)
                {
                    return (true, ptRet);
                }
            }

            // Couldn't find anything
            return (false, ptRet);
        }

        private (bool, Point) CheckSample(PointMap map)
        {
            var ptAhead = this[searchPos + 1];
            var ptBehind = this[searchPos - 1];
            var diffX = ptAhead.X - ptBehind.X;
            var diffY = ptAhead.Y - ptBehind.Y;
            var vecPerp = new Point(diffY, -diffX);
            var norm = 1.5 * map.Sep / Math.Sqrt(Utilities.Norm(vecPerp));
            vecPerp = new Point(vecPerp.X * norm, vecPerp.Y * norm);
            var midPt = new Point((ptAhead.X + ptBehind.X)/2, (ptBehind.Y + ptAhead.Y)/2);
            Point ptTest;
            if (fFirstSide)
            {
                ptTest = new Point(midPt.X + vecPerp.X, midPt.Y + vecPerp.Y);
            }
            else
            {
                ptTest = new Point(midPt.X - vecPerp.X, midPt.Y - vecPerp.Y);
                searchPos += sampleInterval;
            }

            fFirstSide = !fFirstSide;
            return  (map.Onboard(ptTest) && map.IsValid(this, ptTest), ptTest);
        }

        public Point this[int i] => i < 0 ? _bwd[-1 - i] : _fwd[i];
    }
}
