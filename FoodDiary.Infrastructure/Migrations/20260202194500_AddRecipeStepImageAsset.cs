using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

public partial class AddRecipeStepImageAsset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ImageAssetId",
            table: "RecipeSteps",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_RecipeSteps_ImageAssetId",
            table: "RecipeSteps",
            column: "ImageAssetId");

        migrationBuilder.AddForeignKey(
            name: "FK_RecipeSteps_ImageAssets_ImageAssetId",
            table: "RecipeSteps",
            column: "ImageAssetId",
            principalTable: "ImageAssets",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_RecipeSteps_ImageAssets_ImageAssetId",
            table: "RecipeSteps");

        migrationBuilder.DropIndex(
            name: "IX_RecipeSteps_ImageAssetId",
            table: "RecipeSteps");

        migrationBuilder.DropColumn(
            name: "ImageAssetId",
            table: "RecipeSteps");
    }
}
