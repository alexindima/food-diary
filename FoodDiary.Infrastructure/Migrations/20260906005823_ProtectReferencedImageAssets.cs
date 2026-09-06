using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class ProtectReferencedImageAssets : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropForeignKey(
                name: "FK_MealAiSessions_ImageAssets_ImageAssetId",
                table: "MealAiSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Meals_ImageAssets_ImageAssetId",
                table: "Meals");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ImageAssets_ImageAssetId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_ImageAssets_ImageAssetId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeSteps_ImageAssets_ImageAssetId",
                table: "RecipeSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ImageAssets_ProfileImageAssetId",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_MealAiSessions_ImageAssets_ImageAssetId",
                table: "MealAiSessions",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Meals_ImageAssets_ImageAssetId",
                table: "Meals",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ImageAssets_ImageAssetId",
                table: "Products",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_ImageAssets_ImageAssetId",
                table: "Recipes",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeSteps_ImageAssets_ImageAssetId",
                table: "RecipeSteps",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ImageAssets_ProfileImageAssetId",
                table: "Users",
                column: "ProfileImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropForeignKey(
                name: "FK_MealAiSessions_ImageAssets_ImageAssetId",
                table: "MealAiSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Meals_ImageAssets_ImageAssetId",
                table: "Meals");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ImageAssets_ImageAssetId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_ImageAssets_ImageAssetId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeSteps_ImageAssets_ImageAssetId",
                table: "RecipeSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ImageAssets_ProfileImageAssetId",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_MealAiSessions_ImageAssets_ImageAssetId",
                table: "MealAiSessions",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Meals_ImageAssets_ImageAssetId",
                table: "Meals",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ImageAssets_ImageAssetId",
                table: "Products",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_ImageAssets_ImageAssetId",
                table: "Recipes",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeSteps_ImageAssets_ImageAssetId",
                table: "RecipeSteps",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ImageAssets_ProfileImageAssetId",
                table: "Users",
                column: "ProfileImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
