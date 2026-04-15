using Chat.Models;

namespace Chat.Services
{
    /// <summary>
    /// Service to manage the search logic
    /// </summary>
    public class VectorSearchService
    {
        private readonly List<DocumentChunk> _vectorStore = new();
        
        private readonly UserVectorStore _userStore;

        public VectorSearchService(UserVectorStore userStore)
        {
            _userStore = userStore;
        }

        // ドキュメントをストヤに追加　(Add documents to store)
        public void AddToStore(string text, float[] vector)
        {
            _userStore.Chunks.Add(new DocumentChunk { Text = text, Vector = vector });
        }

        // 類似度の高いチャンクを検索 (Serach for highly similar chunks)
        public List<string> Search(float[] queryVector, int topK = 3)
        {
            return _userStore.Chunks
                .Select(chunk => new { chunk.Text, Similarity = CosineSimilarity(queryVector, chunk.Vector) })
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .Select(x => x.Text)
                .ToList();
        }

        // コサイン類似度の計算（ベクトルの向きの近さを計算） Calculation of cosine similarity (Calculates the proximity of vector directions)
        private float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) return 0;

            float dotProduct = 0, magnitudeA = 0, magnitudeB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            return dotProduct / (MathF.Sqrt(magnitudeA) * MathF.Sqrt(magnitudeB));
        }

        public void Clear()
        {
            _vectorStore.Clear();
        }
    }
}
