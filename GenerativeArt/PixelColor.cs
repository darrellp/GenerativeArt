using System.Runtime.InteropServices;

namespace GenerativeArt
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PixelColor
    {
        public readonly byte Blue;
        public readonly byte Green;
        public readonly byte Red;
        public readonly byte Alpha;

        public PixelColor(byte R, byte G, byte B)
        {
            Red = R;
            Green = G;
            Blue = B;
            Alpha = 255;
        }
    }
}
