using InboxIntelligence.Models;

namespace InboxIntelligence.Repositories
{
    public interface IEmailRepository
    {
        Task<HashSet<string>> GetExistingMessageIdsAsync(IEnumerable<string> messageIds);
        Task AddRangeAsync(IEnumerable<Email> emails);
    }
}
