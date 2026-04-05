using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddExerciseEntries : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "ExerciseEntries",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    ExerciseType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CaloriesBurned = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_ExerciseEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEntries_UserId_Date",
                table: "ExerciseEntries",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "ExerciseEntries");
        }
    }
}
