using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GenerativeArt.FlowGenerator
{
    internal class Cell
    {
        private readonly Dictionary<Streamline, List<int>> _ptDict = new();
        private readonly double _sepSq;

        internal Cell(double sep)
        {
            _sepSq = sep * sep;
        }

        internal bool IsValid(Streamline line, Point pt)
        {
            foreach (var (thisLine, indices) in _ptDict)
            {
                // We don't compare against our own points
                if (thisLine == line)
                {
                    continue;
                }

                if (indices.Select(index => thisLine[index]).Any(ptTest => Utilities.Dist2(pt, ptTest) < _sepSq))
                {
                    return false;
                }
            }
            return true;
        }

        internal void Add(Streamline line, int index)
        {
            if (!_ptDict.ContainsKey(line))
            {
                _ptDict[line] = new List<int>();
            }

            _ptDict[line].Add(index);
        }
    }
}
