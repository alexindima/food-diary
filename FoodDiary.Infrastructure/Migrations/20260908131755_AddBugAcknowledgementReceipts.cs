using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddBugAcknowledgementReceipts : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "BugAcknowledgementReceipts",
                columns: table => new {
                    InboxId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_BugAcknowledgementReceipts", x => x.InboxId));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "BugAcknowledgementReceipts");
        }
    }
}
