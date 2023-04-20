namespace GenerativeArt
{
    internal interface IGenerator
    {
        void Generate();
        void Initialize(MainWindow ourWindow);
        void Kill();
    }
}
