
using UglyToad.PdfPig;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Services
{
    /// <summary>
    /// Create the necessary files and implement the logic for reading and chunking the document.
    /// </summary>
    public class DocumentService
    {
        private const int ChunkSize = 600;

        public async Task<List<string>> LoadAndChunkDocuments(string folderPath)
        {
            var allChunks = new List<string>();

            // フォルダが存在しない場合は空のリストを返す
            if (!Directory.Exists(folderPath)) return allChunks;

            var files = Directory.GetFiles(folderPath, "*.*")
                .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                string text = file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                              ? ExtractTextFromPdf(file)
                              : await File.ReadAllTextAsync(file);

                string cleanedText = CleanText(text);
                allChunks.AddRange(SplitIntoChunks(cleanedText, ChunkSize));
            }
            return allChunks;
        }

        private string ExtractTextFromPdf(string path)
        {
            var textBuilder = new StringBuilder();
            using (var document = PdfDocument.Open(path))
            {
                foreach (var page in document.GetPages())
                {
                    textBuilder.Append(page.Text);
                    textBuilder.Append(" ");
                }
            }
            return textBuilder.ToString();
        }

        private string CleanText(string text)
        {
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        public List<string> SplitIntoChunks(string text, int chunkSize)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            for (int i = 0; i < words.Length; i += chunkSize)
            {
                chunks.Add(string.Join(" ", words.Skip(i).Take(chunkSize)));
            }
            return chunks;
        }
    }
}
