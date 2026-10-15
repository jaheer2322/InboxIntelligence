using InboxIntelligence.Models;
using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Repositories
{
    public interface IEmailRepository
    {
        Task<HashSet<string>> GetExistingMessageIdsAsync(IEnumerable<string> messageIds);
        Task AddRangeAsync(IEnumerable<Email> emails);
    }
}
