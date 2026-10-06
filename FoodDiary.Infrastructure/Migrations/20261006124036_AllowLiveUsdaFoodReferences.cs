using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AllowLiveUsdaFoodReferences : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_UsdaFoods_UsdaFdcId",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            // The old catalog FK cannot represent provider-only references. Preserve
            // products and local references, clearing only external links on rollback.
            migrationBuilder.Sql(
                """
                UPDATE "Products" AS product
                SET "UsdaFdcId" = NULL
                WHERE product."UsdaFdcId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM "UsdaFoods" AS food
                      WHERE food."FdcId" = product."UsdaFdcId");
                """);
            migrationBuilder.AddForeignKey(
                name: "FK_Products_UsdaFoods_UsdaFdcId",
                table: "Products",
                column: "UsdaFdcId",
                principalTable: "UsdaFoods",
                principalColumn: "FdcId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
