using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Services
{
    public interface IEmailService
    {
        Task<int> ImportEmailsAsync(IEnumerable<BulkEmailImportDTO> emails);
        Task<List<EmailResponseDTO>> GetSimilarEmailsASync(string query);
    }
}
