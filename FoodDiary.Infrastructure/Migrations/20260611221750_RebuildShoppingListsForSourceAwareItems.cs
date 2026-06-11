using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class RebuildShoppingListsForSourceAwareItems : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "Aisle",
                table: "ShoppingListItems",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedOnUtc",
                table: "ShoppingListItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ShoppingListItems",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShoppingListItemSources",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShoppingListItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MealPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    MealPlanMealId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: true),
                    MealType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_ShoppingListItemSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListItemSources_ShoppingListItems_ShoppingListItemId",
                        column: x => x.ShoppingListItemId,
                        principalTable: "ShoppingListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemSources_MealPlanId",
                table: "ShoppingListItemSources",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemSources_RecipeId",
                table: "ShoppingListItemSources",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemSources_ShoppingListItemId",
                table: "ShoppingListItemSources",
                column: "ShoppingListItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "ShoppingListItemSources");

            migrationBuilder.DropColumn(
                name: "Aisle",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "CheckedOnUtc",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ShoppingListItems");
        }
    }
}



