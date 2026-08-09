using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddAchievementEvaluationOutbox : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "AchievementEvaluationOutbox",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                },
                constraints: table => table.PrimaryKey("PK_AchievementEvaluationOutbox", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEvaluationOutbox_DueLease",
                table: "AchievementEvaluationOutbox",
                columns: ["ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEvaluationOutbox_UserId",
                table: "AchievementEvaluationOutbox",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "AchievementEvaluationOutbox");
        }
    }
}
