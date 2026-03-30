using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_UserId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Products_UserId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Meals_UserId",
                table: "Meals");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_UserId_CreatedOnUtc",
                table: "Recipes",
                columns: new[] { "UserId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Visibility_CreatedOnUtc",
                table: "Recipes",
                columns: new[] { "Visibility", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_UserId_CreatedOnUtc",
                table: "Products",
                columns: new[] { "UserId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Visibility_CreatedOnUtc",
                table: "Products",
                columns: new[] { "Visibility", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Meals_UserId_Date_CreatedOnUtc",
                table: "Meals",
                columns: new[] { "UserId", "Date", "CreatedOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_UserId_CreatedOnUtc",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Visibility_CreatedOnUtc",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Products_UserId_CreatedOnUtc",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Visibility_CreatedOnUtc",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Meals_UserId_Date_CreatedOnUtc",
                table: "Meals");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_UserId",
                table: "Recipes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UserId",
                table: "Products",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_UserId",
                table: "Meals",
                column: "UserId");
        }
    }
}
