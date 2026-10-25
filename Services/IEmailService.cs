using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Services
{
    public interface IEmailService
    {
        Task<int> ImportEmailsAsync(IEnumerable<BulkEmailImportDTO> emails, CancellationToken cancellationToken = default);
        Task<List<EmailResponseDTO>> GetSimilarEmailsAsync(
                                        string query,
                                        string? contact,
                                        DateTime? startDate,
                                        DateTime? endDate,
                                        CancellationToken cancellationToken = default);
        Task<List<EmailResponseDTO>> GetConversationEmailsAsync(
                                        string conversationId, 
                                        DateTime? startDate,
                                        DateTime? endDate,
                                        int limit,
                                        CancellationToken cancellationToken = default);
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
            int limit = 50,
            CancellationToken cancellationToken = default);
    }
}
