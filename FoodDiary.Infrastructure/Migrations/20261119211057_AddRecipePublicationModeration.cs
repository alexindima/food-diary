using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddRecipePublicationModeration : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "RecipeModerationBudgets",
                columns: table => new {
                    Provider = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowSeconds = table.Column<int>(type: "integer", nullable: false),
                    Requests = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_RecipeModerationBudgets", x => new { x.Provider, x.WindowStart, x.WindowSeconds }));

            migrationBuilder.CreateTable(
                name: "RecipePublicationPolicies",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RulesJson = table.Column<string>(type: "jsonb", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_RecipePublicationPolicies", x => x.Id));

            migrationBuilder.CreateTable(
                name: "RecipePublicationRevisions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrimaryResult = table.Column<string>(type: "jsonb", nullable: true),
                    ShadowResult = table.Column<string>(type: "jsonb", nullable: true),
                    ShadowNextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ShadowAttempts = table.Column<int>(type: "integer", nullable: false),
                    ShadowLeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    ShadowLeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipePublicationRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipePublicationRevisions_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipePublicationImageReferences",
                columns: table => new {
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecipePublicationImageReferences", x => new { x.RevisionId, x.AssetId });
                    table.ForeignKey(
                        name: "FK_RecipePublicationImageReferences_RecipePublicationRevisions~",
                        column: x => x.RevisionId,
                        principalTable: "RecipePublicationRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipePublicationImageReferences_AssetId",
                table: "RecipePublicationImageReferences",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipePublicationPolicies_PublishedAt",
                table: "RecipePublicationPolicies",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecipePublicationRevisions_NextAttemptAt",
                table: "RecipePublicationRevisions",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecipePublicationRevisions_OwnerId",
                table: "RecipePublicationRevisions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipePublicationRevisions_RecipeId",
                table: "RecipePublicationRevisions",
                column: "RecipeId",
                unique: true,
                filter: "\"IsCurrent\"");

            migrationBuilder.CreateIndex(
                name: "IX_RecipePublicationRevisions_ShadowNextAttemptAt",
                table: "RecipePublicationRevisions",
                column: "ShadowNextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecipePublicationRevisions_Status_CreatedAt",
                table: "RecipePublicationRevisions",
                columns: ["Status", "CreatedAt"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "RecipeModerationBudgets");

            migrationBuilder.DropTable(
                name: "RecipePublicationImageReferences");

            migrationBuilder.DropTable(
                name: "RecipePublicationPolicies");

            migrationBuilder.DropTable(
                name: "RecipePublicationRevisions");
        }
    }
}
