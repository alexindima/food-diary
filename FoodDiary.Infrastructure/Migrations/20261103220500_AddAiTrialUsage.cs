using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    [ExcludeFromCodeCoverage]
    public partial class AddAiTrialUsage : Migration {
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "IsTrial",
                table: "AiUsages",
                type: "boolean",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "IsTrial",
                table: "AiUsages");
        }
    }
}
