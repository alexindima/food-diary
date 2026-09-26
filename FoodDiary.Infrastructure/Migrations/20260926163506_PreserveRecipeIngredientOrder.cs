using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class PreserveRecipeIngredientOrder : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            migrationBuilder.Sql("""
                WITH ordered AS (
                    SELECT "Id", ROW_NUMBER() OVER (
                        PARTITION BY "RecipeStepId" ORDER BY "CreatedOnUtc", "Id"
                    ) - 1 AS position
                    FROM "RecipeIngredients"
                )
                UPDATE "RecipeIngredients" AS ingredient
                SET "Position" = ordered.position::integer
                FROM ordered WHERE ingredient."Id" = ordered."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "RecipeIngredients");
        }
    }
}
