using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AddTelegramOperations : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "TelegramOperations",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BotId = table.Column<long>(type: "bigint", nullable: false),
                    UpdateId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    ProtectedCheckpoint = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_TelegramOperations", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperations_BotId_Completed_NextAttemptAtUtc",
                table: "TelegramOperations",
                columns: ["BotId", "Completed", "NextAttemptAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperations_BotId_UpdateId",
                table: "TelegramOperations",
                columns: ["BotId", "UpdateId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperations_UserId",
                table: "TelegramOperations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "TelegramOperations");
        }
    }
}
