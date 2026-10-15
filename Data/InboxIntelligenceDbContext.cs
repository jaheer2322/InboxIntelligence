using Microsoft.EntityFrameworkCore;

namespace InboxIntelligence.Data
{
    public class InboxIntelligenceDbContext : DbContext
    {
        public InboxIntelligenceDbContext(DbContextOptions<InboxIntelligenceDbContext> options) : base (options) { }
    }
}
