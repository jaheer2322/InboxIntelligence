using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InboxIntelligence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailFullTextSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE INDEX "IX_Emails_FullTextSearch"
                ON "Emails"
                USING GIN (
                    to_tsvector('english', "Subject" || ' ' || "Body")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Emails_FullTextSearch";
                """);
        }
    }
}
