using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace GenerativeArt.FlowGenerator
{
    internal class Streamline
    {
        private const int MinDotSize = 2;
        private FlowGenerator _flowgen;
        private List<Point> _fwd = new();
        private List<Point> _bwd = new();
        private bool fSearching = false;
        private int searchPos = 0;
        private bool fFirstSide = true;

        internal int FwdCount => _fwd.Count;
        internal int BwdCount => _bwd.Count;
        internal int Count => _fwd.Count + _bwd.Count;

        internal Streamline(FlowGenerator flowgen)
        {
            _flowgen = flowgen;
        }

        internal int Add(Point pt, bool fForward)
        {
            var list = (fForward ? _fwd : _bwd);

            list.Add(pt);
            return fForward ? list.Count - 1 : -list.Count;
        }

        internal void Draw(DrawingContext dc)
        {
            double t;
            double length = 0;
            double lengthLastDot = Double.MinValue;

            if (Count < _flowgen.ShortCount)
            {
                t = 0.0;
            }
            else if (Count > _flowgen.LongCount)
            {
                t = 1.0;
            }
            else
            {
                t = (double)(Count - _flowgen.ShortCount) / (_flowgen.LongCount - _flowgen.ShortCount);
            }

            var thickThresh = Count * _flowgen.GetThick;

            for (var ivtx = -BwdCount; ivtx < FwdCount - 1; ivtx++)
            {
                var distFromBack = BwdCount + ivtx;
                var distFromFront = FwdCount - ivtx;
                var dist = Math.Min(distFromBack, distFromFront);
                var thickness = _flowgen.MaxThickness;
                var alpha = 255.0;

                if (dist < thickThresh)
                {
                    var ratio = (double)dist / thickThresh;
                    thickness *= ratio;
                    alpha *= ratio;
                }

                var color = Utilities.Lerp(_flowgen.ShortColor, _flowgen.LongColor, t);
                if (_flowgen.UseAlpha)
                {
                    color.A = (byte)alpha;
                }
                var brush = new SolidColorBrush(color);
                if (_flowgen.Dotted)
                {
                    // How far we will have reached after this step.
                    length += FlowGenerator.StepDistance;

                    // If we're not big enough to get started or our span doesn't cover the next
                    // dot's radius, then just move to the next iteration
                    if (thickness < MinDotSize || length < lengthLastDot + thickness)
                    {
                        continue;
                    }

                    if (lengthLastDot <= double.MinValue)
                    {
                        lengthLastDot = length - FlowGenerator.StepDistance - thickness;
                    }

                    Pen? pen = null;
                    if (_flowgen.BorderWidth != 0.0)
                    {
                        var brushOutline = new SolidColorBrush(_flowgen.BorderColor);
                        pen = new Pen(brushOutline, thickness * _flowgen.BorderWidth);
                    }

                    var lengthNextDot = lengthLastDot + thickness;
                    while (lengthNextDot < length)
                    {
                        var tNextDot = (lengthNextDot - (length - FlowGenerator.StepDistance)) / FlowGenerator.StepDistance;
                        var ctrNextDot = Utilities.Lerp(this[ivtx], this[ivtx + 1], tNextDot);
                        dc.DrawEllipse(brush, pen, ctrNextDot, thickness / 2, thickness / 2);
                        lengthLastDot = lengthNextDot;
                        lengthNextDot += thickness;
                    }
                    continue;
                }
                var flow = new Pen(brush, thickness)
                {
                    EndLineCap = PenLineCap.Round,
                    StartLineCap = PenLineCap.Round,
                };
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
            var norm = _flowgen.StartPtMult * _flowgen.InterlineDistance / Math.Sqrt(Utilities.Norm(vecPerp));
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
                searchPos += _flowgen.SampleInterval;
            }

            fFirstSide = !fFirstSide;
            return  (map.Onboard(ptTest) && map.IsValid(this, ptTest), ptTest);
        }

        public Point this[int i] => i < 0 ? _bwd[-1 - i] : _fwd[i];
    }
}
