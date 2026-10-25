using System.Text.Json;

namespace InboxIntelligence.Services
{
    public interface IGeminiAgentClient
    {
        Task<JsonDocument> CreateInteractionAsync(
            object requestBody,
            CancellationToken cancellationToken = default);
    }
}
