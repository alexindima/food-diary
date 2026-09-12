using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AddMealRecognitionReceipts : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "MealRecognitionReceipts",
                columns: table => new {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecognitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealVersion = table.Column<long>(type: "bigint", nullable: false),
                    MealOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SavedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UndoUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UndoneAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_MealRecognitionReceipts", x => x.OperationId);
                    table.ForeignKey(
                        name: "FK_MealRecognitionReceipts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealRecognitionReceipts_UserId_RecognitionId",
                table: "MealRecognitionReceipts",
                columns: ["UserId", "RecognitionId"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "MealRecognitionReceipts");
        }
    }
}
