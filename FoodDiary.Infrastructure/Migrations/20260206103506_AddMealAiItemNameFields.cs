using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealAiItemNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "MealAiItems",
                newName: "NameEn");

            migrationBuilder.AddColumn<string>(
                name: "NameLocal",
                table: "MealAiItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameLocal",
                table: "MealAiItems");

            migrationBuilder.RenameColumn(
                name: "NameEn",
                table: "MealAiItems",
                newName: "Name");
        }
    }
}
