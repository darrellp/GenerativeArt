namespace GenerativeArt.NoiseGenerator
{
    internal struct Parameters
    {
        internal double Frequency { get; set; } = 7;
        internal double Persistence { get; set; } = 0.5;
        internal int Octaves { get; set; } = 6;

        public Parameters() { }
    }
}
