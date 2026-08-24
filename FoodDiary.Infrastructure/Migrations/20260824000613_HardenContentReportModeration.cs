using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class HardenContentReportModeration : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_ContentReports_UserId_TargetType_TargetId",
            table: "ContentReports");

        migrationBuilder.AddColumn<Guid>(
            name: "ReviewedByUserId",
            table: "ContentReports",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
                DELETE FROM "ContentReports" AS duplicate
                USING "ContentReports" AS keeper
                WHERE duplicate."UserId" = keeper."UserId"
                  AND duplicate."TargetType" = keeper."TargetType"
                  AND duplicate."TargetId" = keeper."TargetId"
                  AND (duplicate."CreatedOnUtc", duplicate."Id") > (keeper."CreatedOnUtc", keeper."Id");
                """);

        migrationBuilder.CreateIndex(
            name: "IX_ContentReports_UserId_TargetType_TargetId",
            table: "ContentReports",
            columns: ["UserId", "TargetType", "TargetId"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_ContentReports_UserId_TargetType_TargetId",
            table: "ContentReports");

        migrationBuilder.DropColumn(
            name: "ReviewedByUserId",
            table: "ContentReports");

        migrationBuilder.CreateIndex(
            name: "IX_ContentReports_UserId_TargetType_TargetId",
            table: "ContentReports",
            columns: ["UserId", "TargetType", "TargetId"]);
    }
}
