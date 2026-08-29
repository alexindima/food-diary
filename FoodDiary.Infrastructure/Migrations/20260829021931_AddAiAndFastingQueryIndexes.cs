using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddAiAndFastingQueryIndexes : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_StartedAtUtc_Active",
                table: "FastingOccurrences",
                column: "StartedAtUtc",
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_FastingOccurrences_UserId_StartedAtUtc",
                table: "FastingOccurrences",
                columns: ["UserId", "StartedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_AiUsages_CreatedOnUtc",
                table: "AiUsages",
                column: "CreatedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_FastingOccurrences_StartedAtUtc_Active",
                table: "FastingOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_FastingOccurrences_UserId_StartedAtUtc",
                table: "FastingOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_AiUsages_CreatedOnUtc",
                table: "AiUsages");
        }
    }
}
