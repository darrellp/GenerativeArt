using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerativeArt.CrabNebula
{
    internal struct Parameters
    {
        internal int CPoints { get; set; } = 6_000_000;
        internal double NoiseScale { get; set; } = 800.0;
        internal double StdDev { get; set; } = 0.15;
        internal int CBands { get; set; } = 8;
        internal double Frequency { get; set; } = 1.5;
        internal double Persistence { get; set; } = 5;
        internal int Octaves { get; set; } = 3;

        public Parameters() {}
    }
}
