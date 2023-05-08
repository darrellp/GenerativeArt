namespace GenerativeArt
{
    internal interface IGenerator
    {
        void Generate(int seed = -1);
        void Initialize();
        void Kill();
    }
}
