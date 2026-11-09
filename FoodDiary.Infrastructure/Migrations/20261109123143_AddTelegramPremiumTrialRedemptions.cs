using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddTelegramPremiumTrialRedemptions : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "PremiumTrialRestrictedByTelegram",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TelegramPremiumTrialRedemptions",
                columns: table => new { TelegramUserId = table.Column<long>(type: "bigint", nullable: false) },
                constraints: table => table.PrimaryKey("PK_TelegramPremiumTrialRedemptions", x => x.TelegramUserId));

            // Only a Telegram-only account proves its current verified identity was used for the historical trial.
            migrationBuilder.Sql("""
                INSERT INTO "TelegramPremiumTrialRedemptions" ("TelegramUserId")
                SELECT "TelegramUserId" FROM "Users"
                WHERE "TelegramUserId" > 0 AND "TelegramLinkVerifiedForRecovery" AND "Email" IS NULL AND NOT "HasPassword"
                    AND "GoogleSubject" IS NULL AND ("PremiumTrialStartedAtUtc" IS NOT NULL OR "PremiumTrialEndsAtUtc" IS NOT NULL)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "TelegramPremiumTrialRedemptions");

            migrationBuilder.DropColumn(
                name: "PremiumTrialRestrictedByTelegram",
                table: "Users");
        }
    }
}
