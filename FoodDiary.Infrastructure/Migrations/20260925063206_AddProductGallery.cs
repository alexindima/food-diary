using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddProductGallery : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new {
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_ProductImages", x => new { x.ProductId, x.ImageAssetId });
                    table.ForeignKey(
                        name: "FK_ProductImages_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ImageAssetId",
                table: "ProductImages",
                column: "ImageAssetId");
            migrationBuilder.Sql("""
                INSERT INTO "ProductImages" ("ProductId", "ImageAssetId", "ImageUrl", "Position")
                SELECT "Id", "ImageAssetId", "ImageUrl", 0 FROM "Products"
                WHERE "ImageAssetId" IS NOT NULL AND "ImageUrl" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "ProductImages");
        }
    }
}
