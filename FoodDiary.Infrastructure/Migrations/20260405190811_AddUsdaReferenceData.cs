using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddUsdaReferenceData : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "UsdaFoods",
                columns: table => new {
                    FdcId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FoodCategoryId = table.Column<int>(type: "integer", nullable: true),
                    FoodCategory = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UsdaFoods", x => x.FdcId);
                });

            migrationBuilder.CreateTable(
                name: "UsdaNutrients",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UsdaNutrients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsdaFoodPortions",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    FdcId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    MeasureUnitName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GramWeight = table.Column<double>(type: "double precision", nullable: false),
                    PortionDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Modifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UsdaFoodPortions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsdaFoodPortions_UsdaFoods_FdcId",
                        column: x => x.FdcId,
                        principalTable: "UsdaFoods",
                        principalColumn: "FdcId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsdaFoodNutrients",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    FdcId = table.Column<int>(type: "integer", nullable: false),
                    NutrientId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UsdaFoodNutrients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsdaFoodNutrients_UsdaFoods_FdcId",
                        column: x => x.FdcId,
                        principalTable: "UsdaFoods",
                        principalColumn: "FdcId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsdaFoodNutrients_UsdaNutrients_NutrientId",
                        column: x => x.NutrientId,
                        principalTable: "UsdaNutrients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoodNutrients_FdcId_NutrientId",
                table: "UsdaFoodNutrients",
                columns: new[] { "FdcId", "NutrientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoodNutrients_NutrientId",
                table: "UsdaFoodNutrients",
                column: "NutrientId");

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoodPortions_FdcId",
                table: "UsdaFoodPortions",
                column: "FdcId");

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoods_Description",
                table: "UsdaFoods",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_UsdaFoods_FoodCategoryId",
                table: "UsdaFoods",
                column: "FoodCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "UsdaFoodNutrients");

            migrationBuilder.DropTable(
                name: "UsdaFoodPortions");

            migrationBuilder.DropTable(
                name: "UsdaNutrients");

            migrationBuilder.DropTable(
                name: "UsdaFoods");
        }
    }
}
