using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddMealAiReviewMetadata : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MealAiSessions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Reviewed");

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "MealAiItems",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "MealAiItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Accepted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "MealAiSessions");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "MealAiItems");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "MealAiItems");
        }
    }
}
