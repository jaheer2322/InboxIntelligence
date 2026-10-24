using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InboxIntelligence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactEmails",
                columns: table => new
                {
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    EmailId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactEmails", x => new { x.EmailAddress, x.EmailId });
                    table.ForeignKey(
                        name: "FK_ContactEmails_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmails_EmailId",
                table: "ContactEmails",
                column: "EmailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactEmails");
        }
    }
}
