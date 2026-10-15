using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InboxIntelligence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalMessageId",
                table: "Emails",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ExternalMessageId",
                table: "Emails",
                column: "ExternalMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Emails_ExternalMessageId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "ExternalMessageId",
                table: "Emails");
        }
    }
}
