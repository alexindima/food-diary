using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddOpenFoodFactsProductCache : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "OpenFoodFactsProducts",
                columns: table => new {
                    Barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Brand = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CaloriesPer100G = table.Column<double>(type: "double precision", nullable: true),
                    ProteinsPer100G = table.Column<double>(type: "double precision", nullable: true),
                    FatsPer100G = table.Column<double>(type: "double precision", nullable: true),
                    CarbsPer100G = table.Column<double>(type: "double precision", nullable: true),
                    FiberPer100G = table.Column<double>(type: "double precision", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SearchHitCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_OpenFoodFactsProducts", x => x.Barcode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_Brand",
                table: "OpenFoodFactsProducts",
                column: "Brand")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_Category",
                table: "OpenFoodFactsProducts",
                column: "Category")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_LastSeenAtUtc",
                table: "OpenFoodFactsProducts",
                column: "LastSeenAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OpenFoodFactsProducts_Name",
                table: "OpenFoodFactsProducts",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "OpenFoodFactsProducts");
        }
    }
}
