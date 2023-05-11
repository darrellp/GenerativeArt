namespace GenerativeArt
{
    internal interface IGenerator
    {
        void Generate(int seed = -1);
        void Initialize();
        void Kill();

        string? Serialize(int seed)
        {
            return null;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Deserialize this object from the json string. </summary>
        ///
        /// <param name="json"> The JSON for the serialization. </param>
        ///
        /// <returns>   The seed. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        int Deserialize(string json)
        {
            return -1;
        }

        string SerialExtension => string.Empty;
        public bool DoesSerialization => SerialExtension.Length > 0;
    }
}
