using InboxIntelligence.Models;
using Microsoft.EntityFrameworkCore;

namespace InboxIntelligence.Data
{
    public class InboxIntelligenceDbContext : DbContext
    {
        public InboxIntelligenceDbContext(DbContextOptions<InboxIntelligenceDbContext> options) : base(options) { }
        public DbSet<Email> Emails => Set<Email>();
        public DbSet<ContactEmail> ContactEmails => Set<ContactEmail>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Email>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExternalMessageId)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.HasIndex(e => e.ExternalMessageId)
                    .IsUnique();
                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(e => e.Sender)
                    .IsRequired()
                    .HasMaxLength(320);
                entity.Property(e => e.Recipients)
                    .IsRequired();
                entity.Property(e => e.Body)
                    .IsRequired();
                entity.Property(e => e.ConversationId)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.SentAt)
                    .IsRequired();
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.SentAt);
                entity.Property(e => e.Embedding)
                    .HasColumnType("vector(768)");
            });

            modelBuilder.Entity<ContactEmail>(entity =>
            {
                entity.HasKey(ce => new { ce.EmailAddress, ce.EmailId });
                entity.Property(ce => ce.EmailAddress).IsRequired();
                entity.HasOne(ce => ce.Email)
                      .WithMany()
                      .HasForeignKey(ce =>ce.EmailId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
