using InboxIntelligence.Models;
using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Repositories
{
    public interface IEmailRepository
    {
        Task<HashSet<string>> GetExistingMessageIdsAsync(
            IEnumerable<string> messageIds,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IEnumerable<Email> emails,
            IEnumerable<ContactEmail> contactEmails,
            CancellationToken cancellationToken = default);

        Task<List<Email>> GetEmailsPendingEmbeddingAsync(
            int batchSize,
            CancellationToken cancellationToken = default);

        Task SaveEmbeddingAsync(
            Email email,
            float[] embedding,
            CancellationToken cancellationToken = default);

        Task IncrementEmbeddingFailureCountAsync(
            Email email,
            CancellationToken cancellationToken = default);

        Task<List<Email>> GetSimilarEmailsAsync(
            float[] queryEmbedding,
            string? contact,
            DateTime? startDate,
            DateTime? endDate,
            int topK,
            CancellationToken cancellationToken = default);

        Task<List<Email>> GetEmailsByKeywordAsync(
            string query,
            string? contact,
            DateTime? startDate,
            DateTime? endDate,
            int topK,
            CancellationToken cancellationToken = default);

        Task<List<Email>> GetConversationEmailsAsync(
            string conversationId,
            DateTime? startDate,
            DateTime? endDate,
            int limit,
            CancellationToken cancellationToken = default);

        Task<List<Email>> GetEmailsByContactAsync(
            string contact,
            DateTime? startDate,
            DateTime? endDate,
            int limit = 50,
            CancellationToken cancellationToken = default);
    }
}
