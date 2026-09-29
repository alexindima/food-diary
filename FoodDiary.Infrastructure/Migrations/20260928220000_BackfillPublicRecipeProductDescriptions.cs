using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    [ExcludeFromCodeCoverage]
    public partial class BackfillPublicRecipeProductDescriptions : Migration {
        protected override void Up(MigrationBuilder migrationBuilder) {
            // Existing public recipes publish their ingredient names, including private products.
            // Preserve author-provided snapshots and leave private recipes untouched.
            migrationBuilder.Sql("""
                UPDATE "RecipeIngredients" AS ingredient
                SET "PublicName" = LEFT(BTRIM(product."Name"), 256),
                    "PublicUnit" = CASE product."BaseUnit"
                        WHEN 0 THEN 'G'
                        WHEN 1 THEN 'Ml'
                        WHEN 2 THEN 'Pcs'
                        ELSE NULL
                    END
                FROM "Products" AS product, "RecipeSteps" AS step, "Recipes" AS recipe
                WHERE ingredient."ProductId" = product."Id"
                  AND ingredient."RecipeStepId" = step."Id"
                  AND step."RecipeId" = recipe."Id"
                  AND recipe."Visibility" = 0
                  AND ingredient."PublicName" IS NULL
                  AND NULLIF(BTRIM(product."Name"), '') IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            // Publication snapshots cannot be distinguished from later author edits safely.
            // Keep them on rollback; the preceding schema migration owns column removal.
        }
    }
}
