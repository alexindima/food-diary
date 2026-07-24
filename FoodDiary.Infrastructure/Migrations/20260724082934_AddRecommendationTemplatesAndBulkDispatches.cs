using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddRecommendationTemplatesAndBulkDispatches : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "RecommendationBulkDispatches",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DietologistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecommendationBulkDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationBulkDispatches_Recommendations_Recommendation~",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecommendationBulkDispatches_Users_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecommendationBulkDispatches_Users_DietologistUserId",
                        column: x => x.DietologistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationTemplates",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DietologistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_RecommendationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationTemplates_Users_DietologistUserId",
                        column: x => x.DietologistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationBulkDispatches_ClientUserId",
                table: "RecommendationBulkDispatches",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationBulkDispatches_DietologistUserId_IdempotencyK~",
                table: "RecommendationBulkDispatches",
                columns: ["DietologistUserId", "IdempotencyKey", "ClientUserId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationBulkDispatches_RecommendationId",
                table: "RecommendationBulkDispatches",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationTemplates_DietologistUserId_IsArchived_Name",
                table: "RecommendationTemplates",
                columns: ["DietologistUserId", "IsArchived", "Name"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "RecommendationBulkDispatches");

            migrationBuilder.DropTable(
                name: "RecommendationTemplates");
        }
    }
}
