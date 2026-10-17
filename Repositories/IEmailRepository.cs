using InboxIntelligence.Models;

namespace InboxIntelligence.Repositories
{
    public interface IEmailRepository
    {
        Task<HashSet<string>> GetExistingMessageIdsAsync(IEnumerable<string> messageIds);
        Task AddRangeAsync(IEnumerable<Email> emails);
        Task<List<Email>> GetEmailsPendingEmbeddingAsync(int batchSize);
        Task SaveEmbeddingAsync(Email email, float[] embedding);
    }
}
