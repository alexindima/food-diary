using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNutritionGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CarbTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DailyCalorieTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FatTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProteinTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepGoal",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WaterGoal",
                table: "Users",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarbTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DailyCalorieTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FatTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProteinTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StepGoal",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WaterGoal",
                table: "Users");
        }
    }
}
