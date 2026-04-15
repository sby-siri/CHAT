
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Models;

namespace Chat.Services
{
    /// <summary>
    /// Create a file and write the logic to convert the text into a numeric array (Vector).
    /// Send each chunk to and float[] retrieve it.
    /// </summary>
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;

        private const string EmbeddingUrl = "http://172.23.20.107:1234/v1/embeddings";

        public EmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var response = await _httpClient.PostAsJsonAsync(EmbeddingUrl, new
            {
                model = "nomic-ai/nomic-embed-text-v1.5", //ロードしたモデル名
                input = text
            });

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            // LM Studioのレスポンス形式に合わせてパース　　parse according to LM Studio's response format.
            var embeddingArray = json.GetProperty("data")[0].GetProperty("embedding").EnumerateArray();
            return embeddingArray.Select(x => x.GetSingle()).ToArray();
        }
    }
}
