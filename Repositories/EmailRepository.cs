using InboxIntelligence.Data;
using InboxIntelligence.Models;
using Microsoft.EntityFrameworkCore;

namespace InboxIntelligence.Repositories
{
    public class EmailRepository : IEmailRepository
    {
        private readonly InboxIntelligenceDbContext _dbContext;
        public EmailRepository(InboxIntelligenceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HashSet<string>> GetExistingMessageIdsAsync(IEnumerable<string> messageIds)
        {
            var ids = messageIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

            if (ids.Count == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            var existingIds = await _dbContext.Emails
                .AsNoTracking()
                .Where(e => ids.Contains(e.ExternalMessageId)) //  similar to - WHERE ExternalMessageId IN (x, y, z);  
                .Select(e => e.ExternalMessageId)
                .ToListAsync();

            return new HashSet<string>(existingIds, StringComparer.Ordinal);
        }

        public async Task AddRangeAsync(IEnumerable<Email> emails)
        {
            var emailList = emails.ToList();

            if(emailList.Count == 0) { return; }

            await _dbContext.Emails.AddRangeAsync(emailList);
            await _dbContext.SaveChangesAsync();
        }
    }
}
