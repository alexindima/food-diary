using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddDailyReferenceValues : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "DailyReferenceValues",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NutrientId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AgeGroup = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DailyReferenceValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReferenceValues_UsdaNutrients_NutrientId",
                        column: x => x.NutrientId,
                        principalTable: "UsdaNutrients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyReferenceValues_NutrientId_AgeGroup_Gender",
                table: "DailyReferenceValues",
                columns: new[] { "NutrientId", "AgeGroup", "Gender" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "DailyReferenceValues");
        }
    }
}
