using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFastingSessions : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "FastingSessions",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PlannedDurationHours = table.Column<int>(type: "integer", nullable: false),
                Protocol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_FastingSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_FastingSessions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FastingSessions_UserId",
            table: "FastingSessions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_FastingSessions_UserId_IsCompleted",
            table: "FastingSessions",
            columns: new[] { "UserId", "IsCompleted" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "FastingSessions");
    }
}
