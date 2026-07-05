using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddEmailOutbox : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "EmailOutbox",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                FromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ToAddressesJson = table.Column<string>(type: "text", nullable: false),
                Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                HtmlBody = table.Column<string>(type: "text", nullable: false),
                TextBody = table.Column<string>(type: "text", nullable: true),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NextAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
            },
            constraints: table => {
                table.PrimaryKey("PK_EmailOutbox", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmailOutbox_ProcessedOnUtc_NextAttemptOnUtc",
            table: "EmailOutbox",
            columns: ["ProcessedOnUtc", "NextAttemptOnUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "EmailOutbox");
    }
}
