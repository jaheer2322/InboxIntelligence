using System.Text.Json;

namespace InboxIntelligence.Services
{
    public class GeminiAgentClient : IGeminiAgentClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiAgentClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<JsonDocument> CreateInteractionAsync(
            object requestBody, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requestBody);

            var apiKey = _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://generativelanguage.googleapis.com/v1beta/interactions");

            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                throw new HttpRequestException(
                    $"Gemini Interactions API returned " +
                    $"{(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Response: {errorBody}",
                    inner: null,
                    response.StatusCode);
            }

            await using var responseStream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            return await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);
        }
    }
}
