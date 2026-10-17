using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class HardenTelegramAssertionRecovery : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "TelegramLinkVerifiedForRecovery",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Preserve established Telegram-first identities; old email-account links have unknown provenance.
            migrationBuilder.Sql("UPDATE \"Users\" SET \"TelegramLinkVerifiedForRecovery\" = TRUE WHERE \"TelegramUserId\" IS NOT NULL AND (\"Email\" IS NULL OR NOT \"HasPassword\");");
            // Raw fingerprints cannot be canonicalized. The new epoch invalidates every pre-cutover proof.
            migrationBuilder.Sql("DELETE FROM \"ConsumedTelegramAssertions\";");

            migrationBuilder.AlterColumn<string>(
                name: "Fingerprint",
                table: "ConsumedTelegramAssertions",
                type: "character varying(67)",
                maxLength: 67,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateTable(
                name: "TelegramAssertionPolicies",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    NotBeforeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_TelegramAssertionPolicies", x => x.Id);
                    table.CheckConstraint("CK_TelegramAssertionPolicies_Singleton", "\"Id\" = 1");
                });

            // Existing validators accepted signatures up to five minutes ahead of the server clock.
            migrationBuilder.Sql("INSERT INTO \"TelegramAssertionPolicies\" (\"Id\", \"NotBeforeUtc\") VALUES (1, date_trunc('second', clock_timestamp()) + INTERVAL '5 minutes 1 second');");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConsumedTelegramAssertions_V2",
                table: "ConsumedTelegramAssertions",
                sql: "\"Fingerprint\" LIKE 'v2:%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("DELETE FROM \"ConsumedTelegramAssertions\";");
            migrationBuilder.DropTable(
                name: "TelegramAssertionPolicies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConsumedTelegramAssertions_V2",
                table: "ConsumedTelegramAssertions");

            migrationBuilder.DropColumn(
                name: "TelegramLinkVerifiedForRecovery",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Fingerprint",
                table: "ConsumedTelegramAssertions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(67)",
                oldMaxLength: 67);
        }
    }
}
