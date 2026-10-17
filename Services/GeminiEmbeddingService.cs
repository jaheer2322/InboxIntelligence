
using System.Text.Json.Serialization;

namespace InboxIntelligence.Services
{
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiEmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            var apiKey = _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured.");
            }

            var url =
                "https://generativelanguage.googleapis.com/v1beta/" +
                "models/gemini-embedding-2:embedContent";

            using var request = new HttpRequestMessage(
                HttpMethod.Post, url);

            request.Headers.Add("x-goog-api-key", apiKey);

            request.Content = JsonContent.Create(new
            {
                content = new
                {
                    parts = new[] { new { text } }
                },
                output_dimensionality = 768
            });

            using var response = await _httpClient.SendAsync(
                request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
                    cancellationToken: cancellationToken);

            var values = result?.Embedding?.Values;

            if (values is null || values.Length != 768)
            {
                throw new InvalidOperationException(
                    "Gemini returned an invalid embedding.");
            }

            return values;
        }
    }

    sealed class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingData? Embedding { get; set; }
    }

    sealed class EmbeddingData
    {
        [JsonPropertyName("values")]
        public float[]? Values { get; set; }
    }
}
