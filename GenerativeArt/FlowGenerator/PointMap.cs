using System.Windows;
using System.Windows.Shapes;

namespace GenerativeArt.FlowGenerator
{

    internal class PointMap
    {
        private readonly FlowGenerator _flow;
        private readonly Cell[,] _cells;
        private readonly int _mapWidth;
        private readonly int _mapHeight;

        internal PointMap(FlowGenerator flow)
        {
            _flow = flow;
            _mapWidth = (int)(_flow.ArtWidth / _flow.InterlineDistance) + 1;
            _mapHeight = (int)(_flow.ArtHeight / _flow.InterlineDistance) + 1;
            _cells = new Cell[_mapWidth, _mapHeight];

            for (var ix = 0; ix < _mapWidth; ix++)
            {
                for (var iy = 0; iy < _mapHeight; iy++)
                {
                    _cells[ix, iy] = new Cell(_flow.InterlineDistance);
                }
            }
        }

        internal bool Onboard(Point pt) => pt.X >= 0 && pt.Y >= 0 && pt.X < _flow.ArtWidth && pt.Y < _flow.ArtHeight;

        internal bool Register(Streamline line, Point pt, bool fForward)
        {
            if (pt.X < 0 || pt.X >= _flow.ArtWidth || pt.Y < 0 || pt.Y >= _flow.ArtHeight)
            {
                line.Add(pt, fForward);
                return true;
            }

            if (!IsValid(line, pt))
            {
                return false;
            }
            var xIndex = (int)(pt.X / _flow.InterlineDistance);
            var yIndex = (int)(pt.Y / _flow.InterlineDistance);

            var index = line.Add(pt, fForward);
            _cells[xIndex, yIndex].Add(line, index);
            return true;
        }

        internal bool IsValid(Streamline line, Point pt)
        {
            if (pt.X < 0 || pt.X >= _flow.ArtWidth || pt.Y < 0 || pt.Y >= _flow.ArtHeight)
            {
                return true;
            }

            var xIndex = (int)(pt.X / _flow.InterlineDistance);
            var yIndex = (int)(pt.Y / _flow.InterlineDistance);

            for (var ix = xIndex - 1; ix <= xIndex + 1; ix++)
            {
                if (ix < 0 || ix >= _mapWidth)
                {
                    continue;
                }

                for (var iy = yIndex - 1; iy <= yIndex + 1; iy++)
                {
                    if (iy < 0 || iy >= _mapHeight)
                    {
                        continue;
                    }

                    if (!_cells[ix, iy].IsValid(line, pt))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
