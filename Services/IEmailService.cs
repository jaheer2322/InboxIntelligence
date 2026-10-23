using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Services
{
    public interface IEmailService
    {
        Task<int> ImportEmailsAsync(IEnumerable<BulkEmailImportDTO> emails);
        Task<List<EmailResponseDTO>> GetSimilarEmailsAsync(
            string query,
            DateTime? startDate,
            DateTime? endDate);
        Task<(string Answer, List<EmailResponseDTO> Sources)> AskAsync(
            string question,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken cancellationToken = default);
        Task<List<EmailResponseDTO>> GetConversationEmailsAsync(
            string conversationId,
            DateTime? startDate,
            DateTime? endDate,
            int limit);
    }
}
