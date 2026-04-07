using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class IncreaseNutritionLessonContentMaxLength : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "NutritionLessons",
                type: "character varying(65536)",
                maxLength: 65536,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8192)",
                oldMaxLength: 8192);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "NutritionLessons",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(65536)",
                oldMaxLength: 65536);
        }
    }
}
