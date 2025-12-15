using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileImageAssetIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfileImageAssetId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfileImageAssetId",
                table: "Users",
                column: "ProfileImageAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ImageAssets_ProfileImageAssetId",
                table: "Users",
                column: "ProfileImageAssetId",
                principalTable: "ImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ImageAssets_ProfileImageAssetId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProfileImageAssetId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileImageAssetId",
                table: "Users");
        }
    }
}
