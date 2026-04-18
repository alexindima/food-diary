using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddThemeToUsers : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Users",
                type: "text",
                nullable: true,
                defaultValue: "ocean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Users");
        }
    }
}
