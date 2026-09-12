using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AddTelegramOidcIdentity : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "TelegramOidcIssuer",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramOidcSubject",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramOidcIssuer_TelegramOidcSubject",
                table: "Users",
                columns: ["TelegramOidcIssuer", "TelegramOidcSubject"],
                unique: true,
                filter: "\"TelegramOidcIssuer\" IS NOT NULL AND \"TelegramOidcSubject\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_Users_TelegramOidcIssuer_TelegramOidcSubject",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramOidcIssuer",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramOidcSubject",
                table: "Users");
        }
    }
}
