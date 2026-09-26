using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddRecipeGallery : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "RecipeImages",
                columns: table => new {
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeImages", x => new { x.RecipeId, x.ImageAssetId });
                    table.ForeignKey(
                        name: "FK_RecipeImages_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipeImages_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeImages_ImageAssetId",
                table: "RecipeImages",
                column: "ImageAssetId");
            migrationBuilder.Sql("""
                INSERT INTO "RecipeImages" ("RecipeId", "ImageAssetId", "ImageUrl", "Position")
                SELECT "Id", "ImageAssetId", "ImageUrl", 0 FROM "Recipes"
                WHERE "ImageAssetId" IS NOT NULL AND "ImageUrl" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "RecipeImages");
        }
    }
}
