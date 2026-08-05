using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddWeightGoalLifecycle : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "WeightGoals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetWeight = table.Column<double>(type: "double precision", nullable: false),
                    StartWeight = table.Column<double>(type: "double precision", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndWeight = table.Column<double>(type: "double precision", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WeightGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                    name: "IX_WeightGoals_UserId",
                    table: "WeightGoals",
                    column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.Sql("""
            INSERT INTO "WeightGoals" (
                "Id", "UserId", "TargetWeight", "StartWeight", "StartedAtUtc", "Status", "CreatedOnUtc")
            SELECT
                gen_random_uuid(),
                u."Id",
                u."DesiredWeight",
                COALESCE(latest_entry."Weight", u."Weight", u."DesiredWeight"),
                CURRENT_TIMESTAMP,
                'Active',
                CURRENT_TIMESTAMP
            FROM "Users" u
            LEFT JOIN LATERAL (
                SELECT w."Weight"
                FROM "WeightEntries" w
                WHERE w."UserId" = u."Id"
                ORDER BY w."Date" DESC
                LIMIT 1
            ) latest_entry ON TRUE
            WHERE u."DesiredWeight" IS NOT NULL;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "WeightGoals");
        }
    }
}
