using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddRecipeLanguage : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            name: "Language",
            table: "Recipes",
            type: "character varying(2)",
            maxLength: 2,
            nullable: false,
            defaultValue: "en");

        migrationBuilder.AddColumn<bool>(
            name: "LanguageConfirmed",
            table: "Recipes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""
                UPDATE "Recipes" AS recipe
                SET "Language" = CASE
                    WHEN lower(trim(coalesce(author."Language", ''))) LIKE 'ru%' THEN 'ru'
                    ELSE 'en'
                END
                FROM "Users" AS author
                WHERE recipe."UserId" = author."Id";
                """);
        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Visibility_Language_CreatedOnUtc",
            table: "Recipes",
            columns: ["Visibility", "Language", "CreatedOnUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropIndex(
            name: "IX_Recipes_Visibility_Language_CreatedOnUtc",
            table: "Recipes");

        migrationBuilder.DropColumn(
            name: "Language",
            table: "Recipes");

        migrationBuilder.DropColumn(
            name: "LanguageConfirmed",
            table: "Recipes");
    }
}
