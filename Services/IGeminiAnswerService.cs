namespace InboxIntelligence.Services
{
    public interface IGeminiAnswerService
    {
        Task<string> GenerateAnswerAsync(
            string question,
            string emailContext,
            CancellationToken cancellationToken = default);
    }
}