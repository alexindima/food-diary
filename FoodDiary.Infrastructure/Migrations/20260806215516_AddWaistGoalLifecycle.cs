using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddWaistGoalLifecycle : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "WaistGoals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetWaist = table.Column<double>(type: "double precision", nullable: false),
                    StartWaist = table.Column<double>(type: "double precision", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndWaist = table.Column<double>(type: "double precision", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WaistGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaistGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaistGoals_UserId",
                table: "WaistGoals",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.Sql("""
            INSERT INTO "WaistGoals" (
                "Id", "UserId", "TargetWaist", "StartWaist", "StartedAtUtc", "Status", "CreatedOnUtc")
            SELECT
                gen_random_uuid(),
                u."Id",
                u."DesiredWaist",
                COALESCE(latest_entry."Circumference", u."DesiredWaist"),
                CURRENT_TIMESTAMP,
                'Active',
                CURRENT_TIMESTAMP
            FROM "Users" u
            LEFT JOIN LATERAL (
                SELECT w."Circumference"
                FROM "WaistEntries" w
                WHERE w."UserId" = u."Id"
                ORDER BY w."Date" DESC
                LIMIT 1
            ) latest_entry ON TRUE
            WHERE u."DesiredWaist" IS NOT NULL;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "WaistGoals");
        }
    }
}
