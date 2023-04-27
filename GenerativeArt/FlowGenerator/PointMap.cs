using System.Windows;

namespace GenerativeArt.FlowGenerator
{

    internal class PointMap
    {
        private readonly Cell[,] _cells;
        private readonly int _artWidth;
        private readonly int _artHeight;
        private readonly double _sep;
        private readonly int _mapWidth;
        private readonly int _mapHeight;

        internal PointMap(double sep, int artWidth, int artHeight)
        {
            _mapWidth = (int)(artWidth / sep) + 1;
            _mapHeight = (int)(artHeight / sep) + 1;
            _cells = new Cell[_mapWidth, _mapHeight];
            _artWidth = artWidth;
            _artHeight = artHeight;
            _sep = sep;

            for (var ix = 0; ix < _mapWidth; ix++)
            {
                for (var iy = 0; iy < _mapHeight; iy++)
                {
                    _cells[ix, iy] = new Cell(_sep);
                }
            }
        }

        internal bool Register(Streamline line, Point pt, bool fForward)
        {
            if (pt.X < 0 || pt.X >= _artWidth || pt.Y < 0 || pt.Y >= _artHeight)
            {
                line.Add(pt, fForward);
                return true;
            }

            var xIndex = (int)(pt.X / _sep);
            var yIndex = (int)(pt.Y / _sep);

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

            var index = line.Add(pt, fForward);
            _cells[xIndex, yIndex].Add(line, index);
            return true;
        }
    }
}
