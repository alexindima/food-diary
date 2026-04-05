using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddCalorieCycling : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "CalorieCyclingEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "FridayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MondayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SaturdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SundayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ThursdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TuesdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WednesdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "CalorieCyclingEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FridayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MondayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SaturdayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SundayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ThursdayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TuesdayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WednesdayCalories",
                table: "Users");
        }
    }
}
