using InboxIntelligence.Models;
using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Repositories
{
    public interface IEmailRepository
    {
        Task<HashSet<string>> GetExistingMessageIdsAsync(IEnumerable<string> messageIds);
        Task AddRangeAsync(IEnumerable<Email> emails, IEnumerable<ContactEmail> contactEmails);
        Task<List<Email>> GetEmailsPendingEmbeddingAsync(int batchSize);
        Task SaveEmbeddingAsync(Email email, float[] embedding);
        Task IncrementEmbeddingFailureCountAsync(Email email);
        Task<List<Email>> GetSimilarEmailsAsync(
                            float[] queryEmbedding, 
                            string? contact,
                            DateTime? startDate, 
                            DateTime? endDate, 
                            int topK);
        Task<List<Email>> GetEmailsByKeywordAsync(
                            string query,
                            string? contact,
                            DateTime? startDate,
                            DateTime? endDate, 
                            int topK);
        Task<List<Email>> GetConversationEmailsAsync(
                            string conversationId, 
                            DateTime? startDate, 
                            DateTime? endDate, 
                            int limit);
        Task<List<Email>> GetEmailsByContactAsync(
                            string contact,
                            DateTime? startDate,
                            DateTime? endDate,
                            int limit = 50);
    }
}
