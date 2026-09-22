using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class PreserveRemovedMealFavorites : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_FavoriteMeals_UserId_MealId",
                table: "FavoriteMeals");

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAtUtc",
                table: "FavoriteMeals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteMeals_UserId_MealId",
                table: "FavoriteMeals",
                columns: ["UserId", "MealId"],
                unique: true,
                filter: "\"RemovedAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_FavoriteMeals_UserId_MealId",
                table: "FavoriteMeals");

            migrationBuilder.Sql("DELETE FROM \"FavoriteMeals\" WHERE \"RemovedAtUtc\" IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "RemovedAtUtc",
                table: "FavoriteMeals");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteMeals_UserId_MealId",
                table: "FavoriteMeals",
                columns: ["UserId", "MealId"],
                unique: true);
        }
    }
}
