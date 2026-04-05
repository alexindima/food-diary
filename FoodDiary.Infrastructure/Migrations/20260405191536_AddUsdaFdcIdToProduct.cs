using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddUsdaFdcIdToProduct : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<int>(
                name: "UsdaFdcId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_UsdaFdcId",
                table: "Products",
                column: "UsdaFdcId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UsdaFoods_UsdaFdcId",
                table: "Products",
                column: "UsdaFdcId",
                principalTable: "UsdaFoods",
                principalColumn: "FdcId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_UsdaFoods_UsdaFdcId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UsdaFdcId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UsdaFdcId",
                table: "Products");
        }
    }
}
