using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AddHydrationOperationReceipts : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "HydrationOperationReceipts",
                columns: table => new {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AmountMl = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_HydrationOperationReceipts", x => new { x.UserId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_HydrationOperationReceipts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HydrationOperationReceipts_EntryId",
                table: "HydrationOperationReceipts",
                column: "EntryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "HydrationOperationReceipts");
        }
    }
}
