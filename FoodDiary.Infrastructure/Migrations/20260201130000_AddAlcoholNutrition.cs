using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

public partial class AddAlcoholNutrition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "AlcoholPerBase",
            table: "Products",
            type: "double precision",
            nullable: false,
            defaultValue: 0.0);

        migrationBuilder.AddColumn<double>(
            name: "TotalAlcohol",
            table: "Meals",
            type: "double precision",
            nullable: false,
            defaultValue: 0.0);

        migrationBuilder.AddColumn<double>(
            name: "ManualAlcohol",
            table: "Meals",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "TotalAlcohol",
            table: "Recipes",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "ManualAlcohol",
            table: "Recipes",
            type: "double precision",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AlcoholPerBase",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "TotalAlcohol",
            table: "Meals");

        migrationBuilder.DropColumn(
            name: "ManualAlcohol",
            table: "Meals");

        migrationBuilder.DropColumn(
            name: "TotalAlcohol",
            table: "Recipes");

        migrationBuilder.DropColumn(
            name: "ManualAlcohol",
            table: "Recipes");
    }
}
