using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddImageObjectDeletionOutbox : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "ImageObjectDeletionOutbox",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
            },
            constraints: table => {
                table.PrimaryKey("PK_ImageObjectDeletionOutbox", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ImageObjectDeletionOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "ImageObjectDeletionOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "ImageObjectDeletionOutbox");
    }
}
