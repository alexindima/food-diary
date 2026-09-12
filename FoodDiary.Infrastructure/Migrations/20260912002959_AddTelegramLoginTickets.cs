using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddTelegramLoginTickets : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "TelegramLoginTickets",
                columns: table => new {
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BrowserBindingHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_TelegramLoginTickets", x => x.Fingerprint));

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLoginTickets_ExpiresAtUtc",
                table: "TelegramLoginTickets",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "TelegramLoginTickets");
        }
    }
}
