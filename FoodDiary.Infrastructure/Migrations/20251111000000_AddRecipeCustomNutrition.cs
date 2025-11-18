using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeCustomNutrition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNutritionAutoCalculated",
                table: "Recipes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualCalories",
                table: "Recipes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualCarbs",
                table: "Recipes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualFats",
                table: "Recipes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualFiber",
                table: "Recipes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualProteins",
                table: "Recipes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalFiber",
                table: "Recipes",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNutritionAutoCalculated",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ManualCalories",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ManualCarbs",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ManualFats",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ManualFiber",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ManualProteins",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TotalFiber",
                table: "Recipes");
        }
    }
}
