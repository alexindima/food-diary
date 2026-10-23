using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class EnforceRecipeLikeRecipeReference : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                DELETE FROM "RecipeLikes" AS like_row
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Recipes" AS recipe
                    WHERE recipe."Id" = like_row."RecipeId"
                );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeLikes_Recipes_RecipeId",
                table: "RecipeLikes",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeLikes_Recipes_RecipeId",
                table: "RecipeLikes");
        }
    }
}
