using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddTelegramAssertionReplayProtection : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "ConsumedTelegramAssertions",
            columns: table => new {
                Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumedTelegramAssertions", x => x.Fingerprint);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ConsumedTelegramAssertions_ExpiresAtUtc",
            table: "ConsumedTelegramAssertions",
            column: "ExpiresAtUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "ConsumedTelegramAssertions");
    }
}
