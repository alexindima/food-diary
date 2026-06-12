using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddMealItemSnapshots : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "MealItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<double>(
                name: "SnapshotAlcoholPerBase",
                table: "MealItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotBaseAmount",
                table: "MealItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotCaloriesPerBase",
                table: "MealItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotCarbsPerBase",
                table: "MealItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotFatsPerBase",
                table: "MealItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotFiberPerBase",
                table: "MealItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotImageUrl",
                table: "MealItems",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotName",
                table: "MealItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotProteinsPerBase",
                table: "MealItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotUnit",
                table: "MealItems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceAiItemId",
                table: "MealItems",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotAlcoholPerBase",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotBaseAmount",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotCaloriesPerBase",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotCarbsPerBase",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotFatsPerBase",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotFiberPerBase",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotImageUrl",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotName",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotProteinsPerBase",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SnapshotUnit",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "SourceAiItemId",
                table: "MealItems");
        }
    }
}
