using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class LocalizeUsdaProductNames : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "UsdaEnglishName",
                table: "Products",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "UsdaEnglishName" = btrim(substr("Description", char_length('USDA FoodData Central (SR Legacy) · ') + 1))
                WHERE "UserId" = '00000000-0000-4000-8000-000000000001'
                  AND "Visibility" = 0
                  AND "UsdaFdcId" > 0
                  AND "Id"::text = '67fb0bd8-09f9-88f5-80a9-' || lpad(to_hex("UsdaFdcId"), 12, '0')
                  AND starts_with("Description", 'USDA FoodData Central (SR Legacy) · ')
                  AND char_length(btrim(substr("Description", char_length('USDA FoodData Central (SR Legacy) · ') + 1))) BETWEEN 1 AND 512;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Products_UsdaEnglishName",
                table: "Products",
                column: "UsdaEnglishName",
                filter: "\"UsdaEnglishName\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_Products_UsdaEnglishName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UsdaEnglishName",
                table: "Products");
        }
    }
}
