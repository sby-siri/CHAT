using Microsoft.AspNetCore.Components.Forms;

namespace Chat.Services
{
    public class UserDocumentService
    {
        private readonly DocumentService _documentService;
        private readonly EmbeddingService _embeddingService;
        private readonly VectorSearchService _vectorSearchService;

        public int SelectedTopK { get; set; } = 2;

        public List<string> UploadedDocs { get; private set; } = new();

        //when clear doc is pressed to update the ui
        public event Action? OnChange;

        public UserDocumentService(
            DocumentService documentService,
            EmbeddingService embeddingService,
            VectorSearchService vectorSearchService)
        {
            _documentService = documentService;
            _embeddingService = embeddingService;
            _vectorSearchService = vectorSearchService;
        }

        public async Task ProcessFiles(InputFileChangeEventArgs e)
        {
            foreach (var file in e.GetMultipleFiles())
            {
                UploadedDocs.Add(file.Name);

                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                var text = await reader.ReadToEndAsync();

                var chunks = _documentService.SplitIntoChunks(text, 600);

                foreach (var chunk in chunks)
                {
                    var vector = await _embeddingService.GetEmbeddingAsync(chunk);
                    _vectorSearchService.AddToStore(chunk, vector);
                }
            }

            OnChange?.Invoke();
        }

        public void Clear()
        {
            UploadedDocs.Clear();
            _vectorSearchService.Clear();

            OnChange?.Invoke();
        }
    }
}