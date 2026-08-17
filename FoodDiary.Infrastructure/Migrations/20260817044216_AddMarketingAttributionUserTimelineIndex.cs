using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddMarketingAttributionUserTimelineIndex : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateIndex(
                name: "IX_MarketingAttributionEvents_UserId_OccurredAtUtc",
                table: "MarketingAttributionEvents",
                columns: ["UserId", "OccurredAtUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_MarketingAttributionEvents_UserId_OccurredAtUtc",
                table: "MarketingAttributionEvents");
        }
    }
}
