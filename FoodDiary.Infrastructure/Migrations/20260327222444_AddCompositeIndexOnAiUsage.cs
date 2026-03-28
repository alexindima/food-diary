using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexOnAiUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiUsages_CreatedOnUtc",
                table: "AiUsages");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_UserId_CreatedOnUtc",
                table: "AiUsages",
                columns: new[] { "UserId", "CreatedOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiUsages_UserId_CreatedOnUtc",
                table: "AiUsages");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_CreatedOnUtc",
                table: "AiUsages",
                column: "CreatedOnUtc");
        }
    }
}
