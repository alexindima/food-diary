using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddRecipeStepGallery : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "RecipeStepImages",
                columns: table => new {
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipeStepImages", x => new { x.RecipeStepId, x.ImageAssetId });
                    table.ForeignKey(
                        name: "FK_RecipeStepImages_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipeStepImages_RecipeSteps_RecipeStepId",
                        column: x => x.RecipeStepId,
                        principalTable: "RecipeSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeStepImages_ImageAssetId",
                table: "RecipeStepImages",
                column: "ImageAssetId");
            migrationBuilder.Sql("""
                INSERT INTO "RecipeStepImages" ("RecipeStepId", "ImageAssetId", "ImageUrl", "Position")
                SELECT "Id", "ImageAssetId", "ImageUrl", 0 FROM "RecipeSteps"
                WHERE "ImageAssetId" IS NOT NULL AND "ImageUrl" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "RecipeStepImages");
        }
    }
}
