using InboxIntelligence.Data;
using InboxIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace InboxIntelligence.Repositories
{
    public class EmailRepository : IEmailRepository
    {
        private readonly InboxIntelligenceDbContext _dbContext; ILogger<EmailRepository> _logger;
        public EmailRepository(InboxIntelligenceDbContext dbContext, ILogger<EmailRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
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
        public Task<List<Email>> GetEmailsPendingEmbeddingAsync(int batchSize)
        {
            return _dbContext.Emails.Where(e => e.Embedding == null && e.EmbeddingFailureCount < 3)
                                    .OrderBy(e => e.SentAt)
                                    .Take(batchSize)
                                    .ToListAsync();
        }

        public async Task SaveEmbeddingAsync(Email email, float[] embedding)
        {
            email.Embedding = new Pgvector.Vector(embedding);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<Email>> GetSimilarEmailsAsync(float[] queryEmbedding, int topK)
        {
            var vector = new Vector(queryEmbedding);
            var result = await _dbContext.Emails.AsNoTracking()
                                          .Where(e => e.Embedding != null)
                                          .OrderBy(e => e.Embedding!.CosineDistance(vector))
                                          .Take(topK)
                                          .ToListAsync();

            return result;
        }

        public async Task<List<Email>> GetEmailsByKeywordAsync(
            string query,
            int topK)
        {
            var queryText = string.Join(
                " | ",
                query.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            var keywordQuery = _dbContext.Emails
                .AsNoTracking()
                .Where(e => EF.Functions
                    .ToTsVector("english", e.Subject + " " + e.Body)
                    .Matches(EF.Functions.ToTsQuery("english", queryText)))
                .OrderByDescending(e => EF.Functions
                    .ToTsVector("english", e.Subject + " " + e.Body)
                    .Rank(EF.Functions.ToTsQuery("english", queryText)))
                .Take(topK);

            _logger.LogInformation("Keyword query text: {QueryText}", queryText);
            _logger.LogInformation("Keyword SQL: {Sql}", keywordQuery.ToQueryString());

            var results = await keywordQuery.ToListAsync();

            _logger.LogInformation("Keyword result count: {Count}", results.Count);

            return results;
        }

        public async Task IncrementEmbeddingFailureCountAsync(Email email)
        {
            email.EmbeddingFailureCount++;
            await _dbContext.SaveChangesAsync();
        }

    }
}
