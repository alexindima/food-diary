using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealManualNutritionOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNutritionAutoCalculated",
                table: "Meals",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualCalories",
                table: "Meals",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualCarbs",
                table: "Meals",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualFats",
                table: "Meals",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualFiber",
                table: "Meals",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ManualProteins",
                table: "Meals",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNutritionAutoCalculated",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "ManualCalories",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "ManualCarbs",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "ManualFats",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "ManualFiber",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "ManualProteins",
                table: "Meals");
        }
    }
}
