using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImageAssetId",
                table: "Recipes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageAssetId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageAssetId",
                table: "Meals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImageAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageAssets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ImageAssetId",
                table: "Recipes",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ImageAssetId",
                table: "Products",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_ImageAssetId",
                table: "Meals",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAssets_UserId",
                table: "ImageAssets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meals_ImageAssets_ImageAssetId",
                table: "Meals",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ImageAssets_ImageAssetId",
                table: "Products",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_ImageAssets_ImageAssetId",
                table: "Recipes",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meals_ImageAssets_ImageAssetId",
                table: "Meals");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ImageAssets_ImageAssetId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_ImageAssets_ImageAssetId",
                table: "Recipes");

            migrationBuilder.DropTable(
                name: "ImageAssets");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_ImageAssetId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Products_ImageAssetId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Meals_ImageAssetId",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "ImageAssetId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ImageAssetId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageAssetId",
                table: "Meals");
        }
    }
}
