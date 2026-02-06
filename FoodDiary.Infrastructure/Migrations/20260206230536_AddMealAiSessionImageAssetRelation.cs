using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealAiSessionImageAssetRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MealAiSessions_ImageAssetId",
                table: "MealAiSessions",
                column: "ImageAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealAiSessions_ImageAssets_ImageAssetId",
                table: "MealAiSessions",
                column: "ImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealAiSessions_ImageAssets_ImageAssetId",
                table: "MealAiSessions");

            migrationBuilder.DropIndex(
                name: "IX_MealAiSessions_ImageAssetId",
                table: "MealAiSessions");
        }
    }
}
