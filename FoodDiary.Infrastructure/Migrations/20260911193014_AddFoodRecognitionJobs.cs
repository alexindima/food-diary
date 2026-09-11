using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddFoodRecognitionJobs : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "FoodRecognitionJobs",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VisionJson = table.Column<string>(type: "text", nullable: true),
                    NutritionJson = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NutritionErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_FoodRecognitionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodRecognitionJobs_ImageAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FoodRecognitionJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_ImageAssetId",
                table: "FoodRecognitionJobs",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_Status_CreatedOnUtc",
                table: "FoodRecognitionJobs",
                columns: ["Status", "CreatedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_UpdatedOnUtc",
                table: "FoodRecognitionJobs",
                column: "UpdatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FoodRecognitionJobs_UserId_CreatedOnUtc",
                table: "FoodRecognitionJobs",
                columns: ["UserId", "CreatedOnUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "FoodRecognitionJobs");
        }
    }
}
