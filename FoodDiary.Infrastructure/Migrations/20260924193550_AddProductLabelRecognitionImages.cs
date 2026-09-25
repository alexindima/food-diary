using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AddProductLabelRecognitionImages : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "IsProductLabel",
                table: "FoodRecognitionJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FoodRecognitionJobImages",
                columns: table => new {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_FoodRecognitionJobImages", x => new { x.JobId, x.ImageAssetId });
                    table.ForeignKey(
                        name: "FK_FoodRecognitionJobImages_FoodRecognitionJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "FoodRecognitionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoodRecognitionJobImages_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobImages_ImageAssetId",
                table: "FoodRecognitionJobImages",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobImages_JobId_Position",
                table: "FoodRecognitionJobImages",
                columns: ["JobId", "Position"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "FoodRecognitionJobImages");

            migrationBuilder.DropColumn(
                name: "IsProductLabel",
                table: "FoodRecognitionJobs");
        }
    }
}
