using InboxIntelligence.Models;

namespace InboxIntelligence.Repositories
{
    public interface IEmailRepository
    {
        Task<HashSet<string>> GetExistingMessageIdsAsync(IEnumerable<string> messageIds);
        Task AddRangeAsync(IEnumerable<Email> emails);
        Task<List<Email>> GetEmailsPendingEmbeddingAsync(int batchSize);
        Task SaveEmbeddingAsync(Email email, float[] embedding);
        Task IncrementEmbeddingFailureCountAsync(Email email);
        Task<List<Email>> GetSimilarEmailsAsync(
                            float[] queryEmbedding, 
                            DateTime? startDate, 
                            DateTime? endDate, 
                            int topK);
        Task<List<Email>> GetEmailsByKeywordAsync(
                            string query,
                            DateTime? startDate,
                            DateTime? endDate, 
                            int topK);
        Task<List<Email>> GetConversationEmailsAsync(
                            string conversationId, 
                            DateTime? startDate, 
                            DateTime? endDate, 
                            int limit);
    }
}
