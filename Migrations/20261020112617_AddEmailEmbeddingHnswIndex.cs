using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InboxIntelligence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailEmbeddingHnswIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            CREATE INDEX "IX_Emails_Embedding_HNSW"
            ON "Emails"
            USING hnsw ("Embedding" vector_cosine_ops)
            WHERE "Embedding" IS NOT NULL;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_Emails_Embedding_HNSW";
            """);
        }
    }
}
