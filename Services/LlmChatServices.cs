
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Chat.Models;
using static Chat.Services.UserDocumentService;

namespace Chat.Services
{
    public class LlmChatServices
    {
        private readonly HttpClient _httpClient;

        private readonly DocumentService _documentService;

        private readonly EmbeddingService _embeddingService;

        private readonly VectorSearchService _vectorSearchService;

        private readonly UserDocumentService _userDocumentService;

        private const string LmStudioUrl = "http://172.23.20.107:1234/v1/chat/completions";

        public LlmChatServices(HttpClient httpClient, DocumentService documentService, EmbeddingService embeddingService, VectorSearchService vectorSearchService, UserDocumentService userDocumentService)
        {
            _httpClient = httpClient;
            _documentService = documentService;
            _embeddingService = embeddingService;
            _vectorSearchService = vectorSearchService; 
            _userDocumentService = userDocumentService;
        }

        /// <summary>
        /// Process of loading and vectorizing documents, when the app is launched.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public async Task InitializeKnowledgeBase(string folderPath)
        {
            var chunks = await _documentService.LoadAndChunkDocuments(folderPath);
            foreach (var text in chunks)
            {
                var vector = await _embeddingService.GetEmbeddingAsync(text);
                _vectorSearchService.AddToStore(text, vector);
            }
        }

        // here final implementation of RAG -> Prompt injection (to inject search results into the prompt)
        // GetChatResponseWithRagStreamAsync (ストリーミング版)  **streaming -> displays one character at a time, such that it does not take much time to respond.
        public async Task GetChatResponseStreamAsync(List<ChatMessage> history, int topK, Action<string> onChunkReceived)
        {
            // 1. ユーザーの最新の質問を取得　Get the latest questions from users.
            var lastUserQuestion = history.LastOrDefault(m => m.Role == "user")?.Content ?? "";

            // 2. 質問をベクトル化し、関連ドキュメントを検索 Vectorize your questions and search for related documents.
            var questionVector = await _embeddingService.GetEmbeddingAsync(lastUserQuestion);
            var relevantDocs = _vectorSearchService.Search(questionVector, topK);   //changing here from 3 to 2, to increase the speed.

            // 3. コンテキスト (参考資料）の構築　 Building a context(reference materials)
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("以下の資料を参考に簡潔に答えてください。");
            contextBuilder.AppendLine("以下の「参考資料」に基づいて、ユーザーの質問に答えてください。");
            contextBuilder.AppendLine("資料に答えがない場合は「分かりません」と答えてください。");
            contextBuilder.AppendLine("\n 「参考資料」:");

            foreach (var doc in relevantDocs)
            {
                contextBuilder.AppendLine($"- {doc.Substring(0, 100)}...");
            }

            // 4. システムプロンプトの作成
            //var systemPrompt = new ChatMessage
            //{
            //    Role = "system",
            //    Content = contextBuilder.ToString(),
            //};

            // 5. 送信用メッセージリスト　（システムプロンプト + 履歴）
            var messagesToSend = new List<object>
            {
                new { role = "system", content = contextBuilder.ToString() }
            };
            messagesToSend.AddRange(history.Select(m => new { role = m.Role, content = m.Content }));

            //  Above this is the search process.   below is prompt injection (to inject search results to prompt)
            // 6. LM Studioへリクエスト送信
            var requestBody = new
            {
                model = "meta-llama-3-8b-instruct", // LM Studio でロードしているモデル名
                messages = messagesToSend,　　　　　　// 
                // temperature = 0.3 // RAGの場合は低め　（正確性重視）がおすすめ
                stream = true     // ストリーミングを有効化
            };

            var request = new HttpRequestMessage(HttpMethod.Post, LmStudioUrl)
            {
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(data);
                        if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) &&
                            choices[0].TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var content))
                        {
                            onChunkReceived(content.GetString() ?? "");
                        }
                    }
                    catch (Exception)  // パースエラーは無視 
                    {
                    }
                }
            }

        }
    }
}
