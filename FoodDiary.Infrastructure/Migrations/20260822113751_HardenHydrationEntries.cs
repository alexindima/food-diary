using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class HardenHydrationEntries : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries");

            migrationBuilder.Sql(
                """
                DELETE FROM "HydrationEntries" duplicate
                USING "HydrationEntries" retained
                WHERE duplicate."UserId" = retained."UserId"
                  AND duplicate."Timestamp" = retained."Timestamp"
                  AND (duplicate."CreatedOnUtc", duplicate."Id") > (retained."CreatedOnUtc", retained."Id");
                """);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "HydrationEntries",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries",
                columns: ["UserId", "Timestamp"],
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_HydrationEntries_AmountMl",
                table: "HydrationEntries",
                sql: "\"AmountMl\" > 0 AND \"AmountMl\" <= 10000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HydrationEntries_AmountMl",
                table: "HydrationEntries");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "HydrationEntries");

            migrationBuilder.CreateIndex(
                name: "IX_HydrationEntries_User_Timestamp",
                table: "HydrationEntries",
                columns: ["UserId", "Timestamp"]);
        }
    }
}
