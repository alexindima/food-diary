using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddRecipePublicIngredientDescriptions : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "PublicName",
                table: "RecipeIngredients",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicUnit",
                table: "RecipeIngredients",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "PublicName",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "PublicUnit",
                table: "RecipeIngredients");
        }
    }
}
