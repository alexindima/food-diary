using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class AddAiPromptTemplates : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "AiPromptTemplates",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    PromptText = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptTemplates_Key_IsActive",
                table: "AiPromptTemplates",
                columns: new[] { "Key", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptTemplates_Key_Locale",
                table: "AiPromptTemplates",
                columns: new[] { "Key", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "AiPromptTemplates");
        }
    }
}
