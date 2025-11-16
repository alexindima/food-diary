using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealSatietyLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PostMealSatietyLevel",
                table: "Meals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreMealSatietyLevel",
                table: "Meals",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostMealSatietyLevel",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "PreMealSatietyLevel",
                table: "Meals");
        }
    }
}
