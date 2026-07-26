using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddOutboxReplayAudit : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "OutboxReplayAudits",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RequestedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    PreviousError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_OutboxReplayAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxReplayAudits_OutboxName_MessageId_RequestedOnUtc",
                table: "OutboxReplayAudits",
                columns: ["OutboxName", "MessageId", "RequestedOnUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "OutboxReplayAudits");
        }
    }
}
