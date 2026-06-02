using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddUserAiTokenLimits : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<long>(
                name: "AiInputTokenLimit",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 5000000L);

            migrationBuilder.AddColumn<long>(
                name: "AiOutputTokenLimit",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 1000000L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "AiInputTokenLimit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AiOutputTokenLimit",
                table: "Users");
        }
    }
}
