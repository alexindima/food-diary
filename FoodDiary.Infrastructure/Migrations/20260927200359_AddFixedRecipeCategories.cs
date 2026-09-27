using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AddFixedRecipeCategories : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                UPDATE "Recipes"
                SET "Category" = CASE LOWER(BTRIM(COALESCE("Category", '')))
                    WHEN 'other' THEN 'other'
                    WHEN 'другое' THEN 'other'
                    WHEN 'breakfast' THEN 'breakfast'
                    WHEN 'завтраки' THEN 'breakfast'
                    WHEN 'завтрак' THEN 'breakfast'
                    WHEN 'soups' THEN 'soups'
                    WHEN 'супы' THEN 'soups'
                    WHEN 'суп' THEN 'soups'
                    WHEN 'soup' THEN 'soups'
                    WHEN 'salads' THEN 'salads'
                    WHEN 'салаты' THEN 'salads'
                    WHEN 'салат' THEN 'salads'
                    WHEN 'salad' THEN 'salads'
                    WHEN 'main_courses' THEN 'main_courses'
                    WHEN 'основные блюда' THEN 'main_courses'
                    WHEN 'main courses' THEN 'main_courses'
                    WHEN 'main' THEN 'main_courses'
                    WHEN 'основное блюдо' THEN 'main_courses'
                    WHEN 'side_dishes' THEN 'side_dishes'
                    WHEN 'гарниры' THEN 'side_dishes'
                    WHEN 'side dishes' THEN 'side_dishes'
                    WHEN 'гарнир' THEN 'side_dishes'
                    WHEN 'appetizers' THEN 'appetizers'
                    WHEN 'закуски' THEN 'appetizers'
                    WHEN 'закуска' THEN 'appetizers'
                    WHEN 'sandwiches' THEN 'sandwiches'
                    WHEN 'сэндвичи и бургеры' THEN 'sandwiches'
                    WHEN 'sandwiches & burgers' THEN 'sandwiches'
                    WHEN 'pasta' THEN 'pasta'
                    WHEN 'паста и лапша' THEN 'pasta'
                    WHEN 'pasta & noodles' THEN 'pasta'
                    WHEN 'baking' THEN 'baking'
                    WHEN 'выпечка' THEN 'baking'
                    WHEN 'desserts' THEN 'desserts'
                    WHEN 'десерты' THEN 'desserts'
                    WHEN 'десерт' THEN 'desserts'
                    WHEN 'dessert' THEN 'desserts'
                    WHEN 'drinks' THEN 'drinks'
                    WHEN 'напитки' THEN 'drinks'
                    WHEN 'напиток' THEN 'drinks'
                    WHEN 'drink' THEN 'drinks'
                    WHEN 'sauces' THEN 'sauces'
                    WHEN 'соусы и заправки' THEN 'sauces'
                    WHEN 'sauces & dressings' THEN 'sauces'
                    WHEN 'соус' THEN 'sauces'
                    WHEN 'sauce' THEN 'sauces'
                    WHEN 'snacks' THEN 'snacks'
                    WHEN 'перекусы' THEN 'snacks'
                    WHEN 'перекус' THEN 'snacks'
                    WHEN 'snack' THEN 'snacks'
                    WHEN 'preserves' THEN 'preserves'
                    WHEN 'заготовки' THEN 'preserves'
                    ELSE 'other'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Recipes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "other",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Recipes_Category",
                table: "Recipes",
                sql: "\"Category\" IN ('other', 'breakfast', 'soups', 'salads', 'main_courses', 'side_dishes', 'appetizers', 'sandwiches', 'pasta', 'baking', 'desserts', 'drinks', 'sauces', 'snacks', 'preserves')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            // Category normalization is intentionally irreversible; original free text cannot be reconstructed.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Recipes_Category",
                table: "Recipes");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Recipes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldDefaultValue: "other");
        }
    }
}
