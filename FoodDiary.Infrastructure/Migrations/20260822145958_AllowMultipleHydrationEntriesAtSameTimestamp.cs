using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AllowMultipleHydrationEntriesAtSameTimestamp : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries");

            migrationBuilder.CreateIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries",
                columns: ["UserId", "Timestamp"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries");

            migrationBuilder.CreateIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries",
                columns: ["UserId", "Timestamp"],
                unique: true);
        }
    }
}
