using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class CoalesceAchievementEvaluationOutbox : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_AchievementEvaluationOutbox_UserId",
            table: "AchievementEvaluationOutbox");

        migrationBuilder.AddColumn<long>(
            name: "Revision",
            table: "AchievementEvaluationOutbox",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.Sql(
            """
                WITH ranked AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "UserId"
                               ORDER BY
                                   CASE WHEN "ProcessedOnUtc" IS NULL AND "DeadLetteredOnUtc" IS NULL THEN 0 ELSE 1 END,
                                   "CreatedOnUtc" DESC,
                                   "Id" DESC) AS row_number
                    FROM "AchievementEvaluationOutbox"
                )
                DELETE FROM "AchievementEvaluationOutbox" AS message
                USING ranked
                WHERE message."Id" = ranked."Id" AND ranked.row_number > 1;

                UPDATE "AchievementEvaluationOutbox"
                SET "Revision" = 1;
                """);

        migrationBuilder.CreateIndex(
            name: "IX_AchievementEvaluationOutbox_UserId",
            table: "AchievementEvaluationOutbox",
            column: "UserId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_AchievementEvaluationOutbox_UserId",
            table: "AchievementEvaluationOutbox");

        migrationBuilder.DropColumn(
            name: "Revision",
            table: "AchievementEvaluationOutbox");

        migrationBuilder.CreateIndex(
            name: "IX_AchievementEvaluationOutbox_UserId",
            table: "AchievementEvaluationOutbox",
            column: "UserId");
    }
}
