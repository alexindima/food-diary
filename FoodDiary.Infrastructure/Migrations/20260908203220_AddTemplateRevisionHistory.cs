using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddTemplateRevisionHistory : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "AiPromptRevisions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptText = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SavedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_AiPromptRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiPromptRevisions_AiPromptTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "AiPromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplateRevisions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SavedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_EmailTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptRevisions_TemplateId_ArchivedOnUtc",
                table: "AiPromptRevisions",
                columns: ["TemplateId", "ArchivedOnUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_TemplateId_ArchivedOnUtc",
                table: "EmailTemplateRevisions",
                columns: ["TemplateId", "ArchivedOnUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "AiPromptRevisions");

            migrationBuilder.DropTable(
                name: "EmailTemplateRevisions");
        }
    }
}
