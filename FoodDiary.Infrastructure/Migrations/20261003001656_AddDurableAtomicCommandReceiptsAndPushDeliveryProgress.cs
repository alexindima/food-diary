using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddDurableAtomicCommandReceiptsAndPushDeliveryProgress : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "CompletedSubscriptionIdsJson",
                table: "NotificationWebPushOutbox",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "AtomicCommandReceipts",
                columns: table => new {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpiresOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_AtomicCommandReceipts", x => x.Key));

            migrationBuilder.CreateIndex(
                name: "IX_AtomicCommandReceipts_ExpiresOnUtc",
                table: "AtomicCommandReceipts",
                column: "ExpiresOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AtomicCommandReceipts_UserId",
                table: "AtomicCommandReceipts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "AtomicCommandReceipts");

            migrationBuilder.DropColumn(
                name: "CompletedSubscriptionIdsJson",
                table: "NotificationWebPushOutbox");
        }
    }
}
