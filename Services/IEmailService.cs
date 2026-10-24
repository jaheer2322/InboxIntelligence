using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Services
{
    public interface IEmailService
    {
        Task<int> ImportEmailsAsync(IEnumerable<BulkEmailImportDTO> emails);
        Task<List<EmailResponseDTO>> GetSimilarEmailsAsync(
                                        string query,
                                        string? contact,
                                        DateTime? startDate,
                                        DateTime? endDate);
        Task<List<EmailResponseDTO>> GetConversationEmailsAsync(
                                        string conversationId, 
                                        DateTime? startDate,
                                        DateTime? endDate,
                                        int limit);
        Task<string> AskAsync(
                        string query,
                        string? contact,
                        DateTime? startDate,
                        DateTime? endDate,
                        CancellationToken cancellationToken = default);
        Task<List<EmailResponseDTO>> GetEmailsByContactAsync(
            string contact,
            DateTime? startDate,
            DateTime? endDate,
            int limit = 50);
    }
}
