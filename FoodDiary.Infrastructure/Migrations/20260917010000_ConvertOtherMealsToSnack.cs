using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    [ExcludeFromCodeCoverage]
    public partial class ConvertOtherMealsToSnack : Migration {
        protected override void Up(MigrationBuilder migrationBuilder) {
            // Persisted enum values: Other = 4, Snack = 3. Leave missing types unchanged.
            migrationBuilder.Sql("""
                UPDATE "Meals"
                SET "MealType" = 3
                WHERE "MealType" = 4;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            throw new NotSupportedException(
                "Cannot restore Other meal types: converted meals cannot be distinguished from original snacks.");
        }
    }
}
