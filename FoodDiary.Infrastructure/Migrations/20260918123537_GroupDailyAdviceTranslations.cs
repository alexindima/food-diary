using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class GroupDailyAdviceTranslations : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "DailyAdvices",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAdvices_GroupId_Locale",
                table: "DailyAdvices",
                columns: ["GroupId", "Locale"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_DailyAdvices_GroupId_Locale",
                table: "DailyAdvices");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "DailyAdvices");
        }
    }
}
