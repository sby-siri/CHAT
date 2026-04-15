namespace Chat.Models
{
    /// <summary>
    /// Model definition for embedding
    /// Save the text and vector pairs to this location
    /// </summary>
    public class DocumentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; } = "";

        public float[]? Vector { get; set; }  // ベクトルデータ

        public class UserVectorStore
        {
            public List<DocumentChunk> Chunks { get; set; }
        }
    }
}
